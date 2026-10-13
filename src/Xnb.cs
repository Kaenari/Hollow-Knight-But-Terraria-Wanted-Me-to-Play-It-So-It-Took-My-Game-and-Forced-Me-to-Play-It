// Czytnik plikow XNB z Terrarii (tekstury, dzwieki, czcionka), zeby mod nie musial zawierac grafik Terrarii.
// Dekompresja LZX przeniesiona z MonoGame LzxDecoder (pochodzi z libmspack).

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TerrariaArsenal
{
    public static class Xnb
    {
        // Zwraca zawartosc XNB po naglowku (lista czytnikow typow + obiekt), juz zdekompresowana.
        public static byte[] Load(string path)
        {
            byte[] d = File.ReadAllBytes(path);
            if (d.Length < 10 || d[0] != 'X' || d[1] != 'N' || d[2] != 'B') throw new Exception("to nie jest XNB: " + path);
            int flags = d[5];
            int total = BitConverter.ToInt32(d, 6);
            if ((flags & 0x80) == 0)
            {
                byte[] raw = new byte[d.Length - 10];
                Buffer.BlockCopy(d, 10, raw, 0, raw.Length);
                return raw;
            }
            int dsize = BitConverter.ToInt32(d, 10);
            int pos = 14;
            Lzx lz = new Lzx(16);
            byte[] output = new byte[dsize];
            int outPos = 0;
            while (pos < total && outPos < dsize)
            {
                int hi = d[pos], lo = d[pos + 1];
                int block = (hi << 8) | lo;
                int frame = 0x8000;
                if (hi == 0xFF)
                {
                    hi = lo;
                    lo = d[pos + 2];
                    frame = (hi << 8) | lo;
                    hi = d[pos + 3];
                    lo = d[pos + 4];
                    block = (hi << 8) | lo;
                    pos += 5;
                }
                else
                {
                    pos += 2;
                }
                if (block == 0 || frame == 0) break;
                byte[] chunk = lz.Decompress(d, pos, block, frame);
                int n = Math.Min(chunk.Length, dsize - outPos);
                Buffer.BlockCopy(chunk, 0, output, outPos, n);
                outPos += n;
                pos += block;
            }
            return output;
        }

        public static int Read7(byte[] b, ref int p)
        {
            int r = 0, s = 0;
            while (true)
            {
                int c = b[p++];
                r |= (c & 0x7F) << s;
                s += 7;
                if ((c & 0x80) == 0) return r;
            }
        }

        // Pomija liste czytnikow typow i zwraca pozycje danych glownego obiektu.
        public static int SkipHeader(byte[] b)
        {
            int p = 0;
            int n = Read7(b, ref p);
            for (int i = 0; i < n; i++)
            {
                int len = Read7(b, ref p);
                p += len + 4;
            }
            Read7(b, ref p); // zasoby wspoldzielone
            Read7(b, ref p); // id typu obiektu
            return p;
        }

        // Texture2D (format Color) -> piksele w ukladzie Unity (pierwszy wiersz = dol obrazka), alfa nie-premnozona.
        public static Color32[] ReadTexture(byte[] b, ref int p, out int w, out int h)
        {
            int fmt = BitConverter.ToInt32(b, p);
            w = BitConverter.ToInt32(b, p + 4);
            h = BitConverter.ToInt32(b, p + 8);
            int mips = BitConverter.ToInt32(b, p + 12);
            p += 16;
            Color32[] px = null;
            for (int m = 0; m < mips; m++)
            {
                int size = BitConverter.ToInt32(b, p);
                p += 4;
                if (m == 0)
                {
                    if (fmt != 0) throw new Exception("nieobslugiwany format tekstury " + fmt);
                    px = new Color32[w * h];
                    for (int y = 0; y < h; y++)
                    {
                        int src = p + y * w * 4;
                        int dst = (h - 1 - y) * w;
                        for (int x = 0; x < w; x++)
                        {
                            int o = src + x * 4;
                            byte a = b[o + 3];
                            byte r = b[o], g = b[o + 1], bl = b[o + 2];
                            if (a > 0 && a < 255)
                            {
                                r = (byte)Math.Min(255, r * 255 / a);
                                g = (byte)Math.Min(255, g * 255 / a);
                                bl = (byte)Math.Min(255, bl * 255 / a);
                            }
                            px[dst + x] = new Color32(r, g, bl, a);
                        }
                    }
                }
                p += size;
            }
            return px;
        }

        public static Texture2D LoadTexture(string path)
        {
            byte[] b = Load(path);
            int p = SkipHeader(b);
            int w, h;
            Color32[] px = ReadTexture(b, ref p, out w, out h);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        // SoundEffect (PCM 16 bit) -> AudioClip.
        public static AudioClip LoadSound(string path, string clipName)
        {
            byte[] b = Load(path);
            int p = SkipHeader(b);
            int fs = BitConverter.ToInt32(b, p);
            p += 4;
            int tag = BitConverter.ToUInt16(b, p);
            int ch = BitConverter.ToUInt16(b, p + 2);
            int rate = BitConverter.ToInt32(b, p + 4);
            int bits = BitConverter.ToUInt16(b, p + 14);
            p += fs;
            int ds = BitConverter.ToInt32(b, p);
            p += 4;
            if (tag != 1 || bits != 16) throw new Exception("nieobslugiwany dzwiek " + tag + "/" + bits);
            int samples = Math.Min(ds, b.Length - p) / 2;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = BitConverter.ToInt16(b, p + i * 2) / 32768f;
            AudioClip clip = AudioClip.Create(clipName, samples / Math.Max(1, ch), ch, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // --- Czcionka ReLogic DynamicSpriteFont (strony DXT3) ---
        public class FontGlyph
        {
            public char Char;
            public Texture2D Page;
            public Rect Uv;
            public float W, H, CropX, CropY, A, B, C;
        }

        public class FontData
        {
            public float LineSpacing;
            public float Spacing;
            public readonly List<FontGlyph> Glyphs = new List<FontGlyph>();
        }

        private static char ReadUtf8Char(byte[] b, ref int p)
        {
            int c = b[p];
            int n = c < 0x80 ? 1 : c < 0xE0 ? 2 : c < 0xF0 ? 3 : 4;
            string s = System.Text.Encoding.UTF8.GetString(b, p, n);
            p += n;
            return s.Length > 0 ? s[0] : '?';
        }

        public static FontData LoadFont(string path, Func<char, bool> want)
        {
            byte[] b = Load(path);
            int p = SkipHeader(b);
            FontData font = new FontData();
            font.Spacing = BitConverter.ToSingle(b, p);
            font.LineSpacing = BitConverter.ToInt32(b, p + 4);
            p += 8;
            ReadUtf8Char(b, ref p); // znak domyslny
            int pages = BitConverter.ToInt32(b, p);
            p += 4;
            HashSet<char> have = new HashSet<char>();
            for (int pg = 0; pg < pages; pg++)
            {
                Read7(b, ref p);
                int fmt = BitConverter.ToInt32(b, p);
                int w = BitConverter.ToInt32(b, p + 4);
                int h = BitConverter.ToInt32(b, p + 8);
                int mips = BitConverter.ToInt32(b, p + 12);
                p += 16;
                int texPos = -1;
                for (int m = 0; m < mips; m++)
                {
                    int size = BitConverter.ToInt32(b, p);
                    p += 4;
                    if (m == 0) texPos = p;
                    p += size;
                }
                int[] rects = ReadRects(b, ref p);
                int[] crops = ReadRects(b, ref p);
                Read7(b, ref p);
                int cc = BitConverter.ToInt32(b, p);
                p += 4;
                char[] chars = new char[cc];
                for (int i = 0; i < cc; i++) chars[i] = ReadUtf8Char(b, ref p);
                Read7(b, ref p);
                int kc = BitConverter.ToInt32(b, p);
                p += 4;
                int kernPos = p;
                p += 12 * kc;

                List<int> need = new List<int>();
                for (int i = 0; i < cc; i++)
                {
                    if (!have.Contains(chars[i]) && want(chars[i])) need.Add(i);
                }
                if (need.Count == 0) continue;
                if (fmt != 5) throw new Exception("nieobslugiwany format strony czcionki " + fmt);
                Texture2D page = DecodeDxt3Alpha(b, texPos, w, h);
                foreach (int i in need)
                {
                    have.Add(chars[i]);
                    FontGlyph g = new FontGlyph();
                    g.Char = chars[i];
                    g.Page = page;
                    float gx = rects[i * 4], gy = rects[i * 4 + 1];
                    g.W = rects[i * 4 + 2];
                    g.H = rects[i * 4 + 3];
                    g.Uv = new Rect(gx / w, 1f - (gy + g.H) / h, g.W / w, g.H / h);
                    g.CropX = crops[i * 4];
                    g.CropY = crops[i * 4 + 1];
                    g.A = BitConverter.ToSingle(b, kernPos + 12 * i);
                    g.B = BitConverter.ToSingle(b, kernPos + 12 * i + 4);
                    g.C = BitConverter.ToSingle(b, kernPos + 12 * i + 8);
                    font.Glyphs.Add(g);
                }
            }
            return font;
        }

        private static int[] ReadRects(byte[] b, ref int p)
        {
            Read7(b, ref p);
            int c = BitConverter.ToInt32(b, p);
            p += 4;
            int[] r = new int[c * 4];
            for (int i = 0; i < c * 4; i++) r[i] = BitConverter.ToInt32(b, p + i * 4);
            p += 16 * c;
            return r;
        }

        // Litery czcionki sa biale, wystarczy kanal alfa z DXT3.
        private static Texture2D DecodeDxt3Alpha(byte[] b, int pos, int w, int h)
        {
            Color32[] px = new Color32[w * h];
            int o = pos;
            for (int by = 0; by < h; by += 4)
            {
                for (int bx = 0; bx < w; bx += 4)
                {
                    ulong alpha = BitConverter.ToUInt64(b, o);
                    o += 16;
                    for (int i = 0; i < 16; i++)
                    {
                        int a = (int)((alpha >> (4 * i)) & 0xF);
                        int x = bx + i % 4, y = by + i / 4;
                        if (x >= w || y >= h) continue;
                        px[(h - 1 - y) * w + x] = new Color32(255, 255, 255, (byte)(a * 17));
                    }
                }
            }
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        // ---------------- LZX ----------------
        private class BitReader
        {
            private readonly byte[] d;
            public int Pos;
            private uint buf;
            public int Left;

            public BitReader(byte[] data, int pos)
            {
                d = data;
                Pos = pos;
            }

            public void Ensure(int n)
            {
                while (Left < n)
                {
                    int lo = Pos < d.Length ? d[Pos] : 0;
                    int hi = Pos + 1 < d.Length ? d[Pos + 1] : 0;
                    Pos += 2;
                    buf |= ((uint)((hi << 8) | lo)) << (16 - Left);
                    Left += 16;
                }
            }

            public uint Peek16()
            {
                return buf >> 16;
            }

            public void Remove(int n)
            {
                buf = n >= 32 ? 0u : buf << n;
                Left -= n;
            }

            public int Read(int n)
            {
                if (n == 0) return 0;
                Ensure(n);
                int v = (int)(buf >> (32 - n));
                Remove(n);
                return v;
            }

            public void Reset()
            {
                buf = 0;
                Left = 0;
            }
        }

        private class Huff
        {
            public readonly ushort[] Sym = new ushort[65536];
            public readonly byte[] Len = new byte[65536];

            public Huff(int[] lengths, int count)
            {
                int code = 0;
                for (int l = 1; l <= 16; l++)
                {
                    for (int s = 0; s < count; s++)
                    {
                        if (lengths[s] != l) continue;
                        int start = code << (16 - l);
                        int end = start + (1 << (16 - l));
                        if (end > 65536) throw new Exception("zly kod Huffmana");
                        for (int k = start; k < end; k++)
                        {
                            Sym[k] = (ushort)s;
                            Len[k] = (byte)l;
                        }
                        code++;
                    }
                    code <<= 1;
                }
            }

            public int Read(BitReader br)
            {
                br.Ensure(16);
                int idx = (int)br.Peek16();
                int l = Len[idx];
                if (l == 0) throw new Exception("zly kod Huffmana");
                br.Remove(l);
                return Sym[idx];
            }
        }

        private class Lzx
        {
            private static readonly int[] ExtraBits = new int[52];
            private static readonly int[] PositionBase = new int[51];

            static Lzx()
            {
                int j = 0;
                for (int i = 0; i < 51; i += 2)
                {
                    ExtraBits[i] = j;
                    ExtraBits[i + 1] = j;
                    if (i != 0 && j < 17) j++;
                }
                j = 0;
                for (int i = 0; i < 51; i++)
                {
                    PositionBase[i] = j;
                    j += 1 << ExtraBits[i];
                }
            }

            private readonly int wsize;
            private readonly byte[] window;
            private int wpos;
            private readonly int mainElements;
            private readonly int[] mainLen;
            private readonly int[] lenLen = new int[249];
            private readonly int[] r = { 1, 1, 1 };
            private bool headerRead;
            private int blockRemaining, blockType, blockLength;
            private Huff main, lenTree, aligned;

            public Lzx(int windowBits)
            {
                wsize = 1 << windowBits;
                window = new byte[wsize];
                int posnSlots;
                switch (windowBits)
                {
                    case 15: posnSlots = 30; break;
                    case 16: posnSlots = 32; break;
                    case 17: posnSlots = 34; break;
                    case 18: posnSlots = 36; break;
                    case 19: posnSlots = 38; break;
                    case 20: posnSlots = 42; break;
                    default: posnSlots = 50; break;
                }
                mainElements = 256 + posnSlots * 8;
                mainLen = new int[mainElements];
            }

            private static void ReadLengths(BitReader br, int[] lens, int first, int last)
            {
                int[] pre = new int[20];
                for (int i = 0; i < 20; i++) pre[i] = br.Read(4);
                Huff pt = new Huff(pre, 20);
                int x = first;
                while (x < last)
                {
                    int z = pt.Read(br);
                    if (z == 17)
                    {
                        int y = br.Read(4) + 4;
                        for (int k = 0; k < y; k++) lens[x++] = 0;
                    }
                    else if (z == 18)
                    {
                        int y = br.Read(5) + 20;
                        for (int k = 0; k < y; k++) lens[x++] = 0;
                    }
                    else if (z == 19)
                    {
                        int y = br.Read(1) + 4;
                        z = pt.Read(br);
                        z = lens[x] - z;
                        if (z < 0) z += 17;
                        for (int k = 0; k < y; k++) lens[x++] = z;
                    }
                    else
                    {
                        z = lens[x] - z;
                        if (z < 0) z += 17;
                        lens[x++] = z;
                    }
                }
            }

            public byte[] Decompress(byte[] data, int start, int inLen, int outLen)
            {
                BitReader br = new BitReader(data, start);
                if (!headerRead)
                {
                    if (br.Read(1) != 0)
                    {
                        br.Read(16);
                        br.Read(16);
                    }
                    headerRead = true;
                }
                int togo = outLen;
                int mask = wsize - 1;
                while (togo > 0)
                {
                    if (blockRemaining == 0)
                    {
                        if (blockType == 3)
                        {
                            if ((blockLength & 1) != 0) br.Pos++;
                            br.Reset();
                        }
                        blockType = br.Read(3);
                        int i = br.Read(16);
                        int j = br.Read(8);
                        blockRemaining = blockLength = (i << 8) | j;
                        if (blockType == 2)
                        {
                            int[] al = new int[8];
                            for (int k = 0; k < 8; k++) al[k] = br.Read(3);
                            aligned = new Huff(al, 8);
                        }
                        if (blockType == 1 || blockType == 2)
                        {
                            ReadLengths(br, mainLen, 0, 256);
                            ReadLengths(br, mainLen, 256, mainElements);
                            main = new Huff(mainLen, mainElements);
                            ReadLengths(br, lenLen, 0, 249);
                            lenTree = new Huff(lenLen, 249);
                        }
                        else if (blockType == 3)
                        {
                            br.Ensure(16);
                            if (br.Left > 16) br.Pos -= 2;
                            r[0] = BitConverter.ToInt32(data, br.Pos);
                            r[1] = BitConverter.ToInt32(data, br.Pos + 4);
                            r[2] = BitConverter.ToInt32(data, br.Pos + 8);
                            br.Pos += 12;
                        }
                        else
                        {
                            throw new Exception("zly typ bloku LZX " + blockType);
                        }
                    }
                    while (blockRemaining > 0 && togo > 0)
                    {
                        int thisRun = Math.Min(blockRemaining, togo);
                        togo -= thisRun;
                        blockRemaining -= thisRun;
                        if (blockType == 3)
                        {
                            for (int k = 0; k < thisRun; k++)
                            {
                                window[wpos] = data[br.Pos++];
                                wpos = (wpos + 1) & mask;
                            }
                            continue;
                        }
                        while (thisRun > 0)
                        {
                            int me = main.Read(br);
                            if (me < 256)
                            {
                                window[wpos] = (byte)me;
                                wpos = (wpos + 1) & mask;
                                thisRun--;
                                continue;
                            }
                            me -= 256;
                            int ml = me & 7;
                            if (ml == 7) ml += lenTree.Read(br);
                            ml += 2;
                            int mo = me >> 3;
                            if (mo > 2)
                            {
                                if (blockType == 1)
                                {
                                    if (mo != 3)
                                    {
                                        int eb = ExtraBits[mo];
                                        mo = PositionBase[mo] - 2 + br.Read(eb);
                                    }
                                    else
                                    {
                                        mo = 1;
                                    }
                                }
                                else
                                {
                                    int eb = ExtraBits[mo];
                                    mo = PositionBase[mo] - 2;
                                    if (eb > 3)
                                    {
                                        mo += br.Read(eb - 3) << 3;
                                        mo += aligned.Read(br);
                                    }
                                    else if (eb == 3)
                                    {
                                        mo += aligned.Read(br);
                                    }
                                    else if (eb > 0)
                                    {
                                        mo += br.Read(eb);
                                    }
                                    else
                                    {
                                        mo = 1;
                                    }
                                }
                                r[2] = r[1];
                                r[1] = r[0];
                                r[0] = mo;
                            }
                            else if (mo == 0)
                            {
                                mo = r[0];
                            }
                            else if (mo == 1)
                            {
                                mo = r[1];
                                r[1] = r[0];
                                r[0] = mo;
                            }
                            else
                            {
                                mo = r[2];
                                r[2] = r[0];
                                r[0] = mo;
                            }
                            thisRun -= ml;
                            int src = (wpos - mo) & mask;
                            for (int k = 0; k < ml; k++)
                            {
                                window[wpos] = window[src];
                                wpos = (wpos + 1) & mask;
                                src = (src + 1) & mask;
                            }
                        }
                    }
                }
                byte[] output = new byte[outLen];
                int sp = (wpos - outLen) & mask;
                for (int k = 0; k < outLen; k++) output[k] = window[(sp + k) & mask];
                return output;
            }
        }
    }
}
