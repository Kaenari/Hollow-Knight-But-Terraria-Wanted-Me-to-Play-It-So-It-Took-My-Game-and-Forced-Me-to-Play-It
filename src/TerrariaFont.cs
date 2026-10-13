// Czcionka bitmapowa z Terrarii (Mouse_Text): atlas Font.png + metryki Font.txt.
// Rysuje jak Terraria: czarny obrys + kolorowy tekst. Wywolywac tylko z OnGUI.

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TerrariaArsenal
{
    public static class TerrariaFont
    {
        private class Glyph
        {
            public Rect Uv;
            public Texture2D Tex;
            public float W, H, CropX, CropY, A, B, C;
        }

        private static Dictionary<char, Glyph> glyphs;
        private static Texture2D atlas;
        private static float lineSpacing = 29f;
        private static float spacing;

        public static void Reset()
        {
            glyphs = null;
            atlas = null;
        }

        private static bool Load()
        {
            if (glyphs != null) return atlas != null;
            glyphs = new Dictionary<char, Glyph>();
            Xnb.FontData fd = TerrariaContent.Font();
            if (fd != null && fd.Glyphs.Count > 0)
            {
                lineSpacing = fd.LineSpacing;
                spacing = fd.Spacing;
                foreach (Xnb.FontGlyph fg in fd.Glyphs)
                {
                    Glyph g = new Glyph();
                    g.Tex = fg.Page;
                    g.Uv = fg.Uv;
                    g.W = fg.W;
                    g.H = fg.H;
                    g.CropX = fg.CropX;
                    g.CropY = fg.CropY;
                    g.A = fg.A;
                    g.B = fg.B;
                    g.C = fg.C;
                    glyphs[fg.Char] = g;
                }
                atlas = fd.Glyphs[0].Page;
                return true;
            }
            Sprite s = Assets.Get("Font", 100f);
            Stream txt = typeof(TerrariaFont).Assembly.GetManifestResourceStream("TerrariaArsenal.Font.txt");
            if (s == null || txt == null) return false;
            atlas = s.texture;
            atlas.filterMode = FilterMode.Bilinear;
            float tw = atlas.width, th = atlas.height;

            StreamReader reader = new StreamReader(txt);
            string[] head = reader.ReadLine().Split(' ');
            lineSpacing = float.Parse(head[0], System.Globalization.CultureInfo.InvariantCulture);
            spacing = float.Parse(head[1], System.Globalization.CultureInfo.InvariantCulture);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] f = line.Split(' ');
                if (f.Length < 10) continue;
                float[] v = new float[10];
                for (int i = 0; i < 10; i++) v[i] = float.Parse(f[i], System.Globalization.CultureInfo.InvariantCulture);
                Glyph g = new Glyph();
                g.W = v[3];
                g.H = v[4];
                g.Tex = atlas;
                g.Uv = new Rect(v[1] / tw, 1f - (v[2] + v[4]) / th, v[3] / tw, v[4] / th);
                g.CropX = v[5];
                g.CropY = v[6];
                g.A = v[7];
                g.B = v[8];
                g.C = v[9];
                glyphs[(char)(int)v[0]] = g;
            }
            reader.Close();
            return true;
        }

        public static float LineHeight(float scale)
        {
            return lineSpacing * scale;
        }

        public static float Measure(string text, float scale)
        {
            if (!Load()) return 0f;
            float x = 0f, best = 0f;
            bool first = true;
            foreach (char ch in text)
            {
                if (ch == '\n')
                {
                    best = Mathf.Max(best, x);
                    x = 0f;
                    first = true;
                    continue;
                }
                Glyph g;
                if (!glyphs.TryGetValue(ch, out g) && !glyphs.TryGetValue('*', out g)) continue;
                x += first ? Mathf.Max(g.A, 0f) : spacing + g.A;
                x += g.B + g.C;
                first = false;
            }
            return Mathf.Max(best, x) * scale;
        }

        // Rysuje jedna linie tekstu z lewym gornym rogiem w (x, y), z obrysem.
        public static void Draw(float x, float y, string text, Color color, float scale)
        {
            if (!Load() || Event.current.type != EventType.Repaint) return;
            float o = Mathf.Max(1f, 2f * scale);
            Color shadow = new Color(0f, 0f, 0f, color.a * 0.85f);
            DrawRaw(x - o, y, text, shadow, scale);
            DrawRaw(x + o, y, text, shadow, scale);
            DrawRaw(x, y - o, text, shadow, scale);
            DrawRaw(x, y + o, text, shadow, scale);
            DrawRaw(x, y, text, color, scale);
        }

        public static void DrawCentered(float centerX, float y, string text, Color color, float scale)
        {
            Draw(centerX - Measure(text, scale) * 0.5f, y, text, color, scale);
        }

        // Dzieli tekst na linie nie szersze niz maxWidth.
        public static List<string> Wrap(string text, float scale, float maxWidth)
        {
            List<string> lines = new List<string>();
            foreach (string para in text.Split('\n'))
            {
                string cur = "";
                foreach (string word in para.Split(' '))
                {
                    string test = cur.Length == 0 ? word : cur + " " + word;
                    if (cur.Length > 0 && Measure(test, scale) > maxWidth)
                    {
                        lines.Add(cur);
                        cur = word;
                    }
                    else
                    {
                        cur = test;
                    }
                }
                lines.Add(cur);
            }
            return lines;
        }

        private static void DrawRaw(float x, float y, string text, Color color, float scale)
        {
            Color old = GUI.color;
            GUI.color = color;
            float ox = 0f;
            bool first = true;
            foreach (char ch in text)
            {
                Glyph g;
                if (!glyphs.TryGetValue(ch, out g) && !glyphs.TryGetValue('*', out g)) continue;
                ox += first ? Mathf.Max(g.A, 0f) : spacing + g.A;
                first = false;
                if (g.W > 1f)
                {
                    Rect r = new Rect(x + (ox + g.CropX) * scale, y + g.CropY * scale, g.W * scale, g.H * scale);
                    GUI.DrawTextureWithTexCoords(r, g.Tex, g.Uv);
                }
                ox += g.B + g.C;
            }
            GUI.color = old;
        }
    }
}
