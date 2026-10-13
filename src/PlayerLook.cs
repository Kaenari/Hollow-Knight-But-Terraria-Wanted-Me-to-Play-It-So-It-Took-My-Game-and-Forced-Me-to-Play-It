// Wyglad postaci z Terrarii skladany z warstw Player_{wariant}_{warstwa} i Player_Hair_{n} z instalacji Terrarii,
// zabarwionych kolorami gracza (jak w kreatorze postaci Terrarii). Wyglad zapisany osobno dla kazdego slotu zapisu.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TerrariaArsenal
{
    [Serializable]
    public class Look
    {
        public int Variant = 0;           // Player_0..9 (0-3 meskie, 4-7 damskie, 8-9 dodatkowe)
        public int Hair = 1;              // Player_Hair_1..
        public string Skin = "FF7D5A";
        public string HairColor = "D75A37";
        public string Eyes = "695A4B";
        public string Shirt = "AFA58C";
        public string Undershirt = "A0B4D7";
        public string Pants = "FFE6AF";
        public string Shoes = "A0693C";

        public Look Copy()
        {
            return (Look)MemberwiseClone();
        }

        public static Color32 Parse(string hex)
        {
            int v;
            if (hex == null || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) v = 0xFFFFFF;
            return new Color32((byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF), 255);
        }

        public static string Hex(Color32 c)
        {
            return c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2");
        }
    }

    public static class PlayerLook
    {
        public const int FrameW = 40, FrameH = 56, FrameCount = 26;
        public const int Variants = 10;

        // Typy ciala z Terrarii (PlayerVariantID): meskie i damskie w kolejnosci z kreatora postaci.
        public static readonly int[] VariantOrder = { 0, 1, 2, 3, 8, 4, 5, 6, 7, 9 };
        public static readonly string[] VariantNames =
        {
            "Męski 1", "Męski 2", "Męski 3", "Męski 4", "Męski 5",
            "Damski 1", "Damski 2", "Damski 3", "Damski 4", "Damski 5"
        };

        public static bool IsFemale(int variant)
        {
            return variant == 4 || variant == 5 || variant == 6 || variant == 7 || variant == 9;
        }
        public const int MaxHair = 165;

        // Klatki arkusza postaci: (klatka nog, komorka reki kolumna/wiersz). Sprawdzone piksel w piksel z Player.png.
        private static readonly int[] Legs = { 0, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 0, 0, 0, 0, 0, 0, 5, 5, 5, 5 };
        private static readonly int[,] Arms =
        {
            { 2, 0 }, { 2, 1 }, { 3, 1 }, { 4, 1 }, { 5, 1 }, { 6, 1 }, { 6, 1 }, { 5, 1 }, { 4, 1 }, { 3, 1 },
            { 2, 0 }, { 2, 0 }, { 3, 1 }, { 4, 1 }, { 5, 1 }, { 6, 1 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 },
            { 5, 0 }, { 4, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 }, { 6, 0 }
        };

        // Klatki nog, w ktorych tulow podskakuje o 2 piksele.
        private static readonly HashSet<int> Bob = new HashSet<int> { 7, 8, 9, 14, 15, 16 };

        public static int Version { get; private set; }

        private static readonly Dictionary<int, Sprite[]> FrameCache = new Dictionary<int, Sprite[]>();
        private static readonly Dictionary<int, int> FrameCacheVersion = new Dictionary<int, int>();

        public static Look Get(int slot)
        {
            Dictionary<int, Look> looks = TerrariaArsenal.Instance.Settings.Looks;
            Look l;
            if (!looks.TryGetValue(slot, out l) || l == null)
            {
                l = new Look();
                looks[slot] = l;
            }
            return l;
        }

        public static void Set(int slot, Look look)
        {
            TerrariaArsenal.Instance.Settings.Looks[slot] = look;
            Version++;
        }

        public static void Changed()
        {
            Version++;
        }

        // Klatki postaci dla slotu zapisu. Bez Terrarii: wbudowany Player.png (domyslny wyglad).
        public static Sprite[] Frames(int slot, float ppu)
        {
            Sprite[] cached;
            int ver;
            if (FrameCache.TryGetValue(slot, out cached) && FrameCacheVersion.TryGetValue(slot, out ver) && ver == Version)
            {
                return cached;
            }
            Texture2D sheet = BuildSheet(Get(slot));
            Sprite[] frames;
            if (sheet == null)
            {
                frames = Assets.GetFrames("Player", ppu, FrameCount);
            }
            else
            {
                frames = new Sprite[FrameCount];
                for (int i = 0; i < FrameCount; i++)
                {
                    Rect r = new Rect(0, sheet.height - (i + 1) * FrameH, FrameW, FrameH);
                    frames[i] = Sprite.Create(sheet, r, new Vector2(0.5f, 0.5f), ppu);
                }
            }
            if (cached != null && cached.Length > 0 && cached[0] != null && sheet != null)
            {
                Texture2D old = cached[0].texture;
                if (old != null && old != sheet && old.name == "TA_PlayerSheet") UnityEngine.Object.Destroy(old);
            }
            FrameCache[slot] = frames;
            FrameCacheVersion[slot] = Version;
            return frames;
        }

        private class Layers
        {
            public Color32[][] P = new Color32[14][];
            public int[] W = new int[14], H = new int[14];
            public Color32[] Hair;
            public int HairW, HairH;
        }

        private static Layers LoadLayers(Look look)
        {
            Layers l = new Layers();
            int v = Mathf.Clamp(look.Variant, 0, Variants - 1);
            int[] needed = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            // Terraria trzyma w wariancie tylko warstwy, ktore sie roznia (np. koszula); reszta jest z wariantu
            // podstawowego tej samej plci (0 albo 4), a na koncu z 0.
            int baseV = IsFemale(v) ? 4 : 0;
            foreach (int i in needed)
            {
                string name = "Player_" + v + "_" + i;
                if (!TerrariaContent.HasImage(name)) name = "Player_" + baseV + "_" + i;
                if (!TerrariaContent.HasImage(name)) name = "Player_0_" + i;
                l.P[i] = TerrariaContent.Pixels(name, out l.W[i], out l.H[i]);
                if (l.P[i] == null) return null;
            }
            l.Hair = TerrariaContent.Pixels("Player_Hair_" + Mathf.Clamp(look.Hair, 1, MaxHair), out l.HairW, out l.HairH);
            return l;
        }

        // Caly arkusz 40 x (26*56). Null gdy brak Terrarii.
        public static Texture2D BuildSheet(Look look)
        {
            if (!TerrariaContent.Available) return null;
            Layers l = LoadLayers(look);
            if (l == null) return null;
            Color32[] px = new Color32[FrameW * FrameH * FrameCount];
            for (int i = 0; i < FrameCount; i++) DrawFrame(px, i * FrameH, FrameH * FrameCount, l, look, Legs[i], Arms[i, 0], Arms[i, 1]);
            Texture2D tex = TerrariaContent.ToTexture(px, FrameW, FrameH * FrameCount);
            tex.name = "TA_PlayerSheet";
            return tex;
        }

        // Pojedyncza klatka (stojaca postac) do podgladu w menu.
        public static Texture2D BuildPreview(Look look)
        {
            if (!TerrariaContent.Available) return null;
            Layers l = LoadLayers(look);
            if (l == null) return null;
            Color32[] px = new Color32[FrameW * FrameH];
            DrawFrame(px, 0, FrameH, l, look, Legs[0], Arms[0, 0], Arms[0, 1]);
            Texture2D tex = TerrariaContent.ToTexture(px, FrameW, FrameH);
            tex.name = "TA_PlayerPreview";
            return tex;
        }

        private static Color32 Dim(Color32 c)
        {
            return new Color32((byte)(int)(c.r * 0.8f), (byte)(int)(c.g * 0.8f), (byte)(int)(c.b * 0.8f), 255);
        }

        private static void DrawFrame(Color32[] dst, int top, int dstH, Layers l, Look look, int legs, int armCol, int armRow)
        {
            Color32 skin = Look.Parse(look.Skin), hair = Look.Parse(look.HairColor), eyes = Look.Parse(look.Eyes);
            Color32 shirt = Look.Parse(look.Shirt), under = Look.Parse(look.Undershirt);
            Color32 pants = Look.Parse(look.Pants), shoes = Look.Parse(look.Shoes);
            Color32 white = new Color32(255, 255, 255, 255);
            int dy = Bob.Contains(legs) ? -2 : 0;

            // Reka z tylu (przyciemniona)
            Cell(dst, top, dstH, l, 7, 8, 0, Dim(skin), dy);
            Cell(dst, top, dstH, l, 8, 8, 0, Dim(under), dy);
            Cell(dst, top, dstH, l, 9, 8, 0, Dim(skin), dy);
            // Nogi
            Cell(dst, top, dstH, l, 10, 0, legs, skin, 0);
            Cell(dst, top, dstH, l, 11, 0, legs, pants, 0);
            Cell(dst, top, dstH, l, 12, 0, legs, shoes, 0);
            // Tulow
            Cell(dst, top, dstH, l, 3, 0, 0, skin, dy);
            Cell(dst, top, dstH, l, 6, 0, 0, shirt, dy);
            // Glowa i oczy
            Cell(dst, top, dstH, l, 0, 0, 0, skin, dy);
            Cell(dst, top, dstH, l, 1, 0, 0, white, dy);
            Cell(dst, top, dstH, l, 2, 0, 0, eyes, dy);
            // Wlosy
            if (l.Hair != null)
            {
                Rows(dst, top, dstH, l.Hair, l.HairW, l.HairH, 0, 0, hair, dy);
            }
            // Reka z przodu
            Cell(dst, top, dstH, l, 7, armCol, armRow, skin, dy);
            Cell(dst, top, dstH, l, 4, armCol, armRow, under, dy);
            Cell(dst, top, dstH, l, 5, armCol, armRow, skin, dy);
        }

        private static void Cell(Color32[] dst, int top, int dstH, Layers l, int layer, int col, int row, Color32 tint, int dy)
        {
            if (l.P[layer] == null) return;
            Rows(dst, top, dstH, l.P[layer], l.W[layer], l.H[layer], col * FrameW, row * FrameH, tint, dy);
        }

        // Kopiuje komorke 40x56 przesunieta o dy, przycieta do wlasnej klatki.
        private static void Rows(Color32[] dst, int top, int dstH, Color32[] src, int sw, int sh, int sx, int sy, Color32 tint, int dy)
        {
            for (int j = 0; j < FrameH; j++)
            {
                int ty = top + j + dy;
                if (ty < top || ty >= top + FrameH) continue;
                TerrariaContent.Layer(dst, FrameW, dstH, src, sw, sh, sx, sy + j, FrameW, 1, 0, ty, tint);
            }
        }
    }
}
