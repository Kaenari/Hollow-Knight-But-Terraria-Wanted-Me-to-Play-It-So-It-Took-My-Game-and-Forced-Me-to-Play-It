// Grafiki i dzwieki Terrarii czytane z instalacji gracza (Content\Images, Content\Sounds, Content\Fonts).
// Tabela nizej mowi, z ktorego pliku Terrarii i z jakiego prostokata (x, y od lewego gornego rogu)
// pochodzi kazda grafika moda. Bez Terrarii mod uzywa grafik wbudowanych (tylko wersja deweloperska).

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TerrariaArsenal
{
    public static class TerrariaContent
    {
        private class Src
        {
            public string File;
            public int X, Y, W, H;
            public int Stride, Count;   // >0 = pionowy pasek klatek (miniony)

            public Src(string file, int x, int y, int w, int h)
            {
                File = file;
                X = x;
                Y = y;
                W = w;
                H = h;
            }

            public Src(string file, int x, int y, int w, int h, int stride, int count) : this(file, x, y, w, h)
            {
                Stride = stride;
                Count = count;
            }
        }

        private static readonly Dictionary<string, Src> Sources = new Dictionary<string, Src>
        {
            { "A_AmethystHook", new Src("Item_1236", 0, 0, 24, 32) },
            { "A_ClimbingClaws", new Src("Item_953", 0, 0, 30, 24) },
            { "A_CloudInABottle", new Src("Item_53", 0, 0, 20, 26) },
            { "A_Flipper", new Src("Item_187", 0, 0, 30, 30) },
            { "A_HermesBoots", new Src("Item_54", 0, 0, 34, 28) },
            { "A_MasterNinjaGear", new Src("Item_984", 0, 0, 30, 30) },
            { "A_MiningHelmet", new Src("Item_88", 0, 0, 22, 16) },
            { "A_ShieldOfCthulhu", new Src("Item_3097", 0, 0, 30, 32) },
            { "A_Tabi", new Src("Item_977", 0, 0, 26, 28) },
            { "AquaScepter", new Src("Item_157", 0, 0, 48, 18) },
            { "BeeKeeper", new Src("Item_1123", 0, 0, 50, 50) },
            { "BeesKnees", new Src("Item_2888", 0, 0, 30, 56) },
            { "BookOfSkulls", new Src("Item_1313", 0, 0, 28, 30) },
            { "BossBar_Back", new Src("UI/UI_BossBar", 32, 196, 456, 22) },
            { "BossBar_Fill", new Src("UI/UI_BossBar", 32, 80, 2, 22) },
            { "BossBar_FillEnd", new Src("UI/UI_BossBar", 32, 138, 2, 22) },
            { "BossBar_Frame", new Src("UI/UI_BossBar", 0, 0, 516, 58) },
            { "Chest", new Src("Item_48", 0, 0, 32, 28) },
            { "CoolWhip", new Src("Item_4911", 0, 0, 38, 36) },
            { "CopperBroadsword", new Src("Item_3508", 0, 0, 36, 36) },
            { "DaedalusStormbow", new Src("Item_3029", 0, 0, 30, 62) },
            { "DarkHarvest", new Src("Item_4680", 0, 0, 40, 32) },
            { "DemonBow", new Src("Item_44", 0, 0, 18, 40) },
            { "DiamondStaff", new Src("Item_744", 0, 0, 42, 40) },
            { "Durendal", new Src("Item_4678", 0, 0, 36, 36) },
            { "EnchantedSword", new Src("Item_989", 0, 0, 34, 34) },
            { "Excalibur", new Src("Item_368", 0, 0, 48, 48) },
            { "FinchStaff", new Src("Item_4281", 0, 0, 38, 38) },
            { "Firecracker", new Src("Item_4912", 0, 0, 38, 40) },
            { "FlinxStaff", new Src("Item_5069", 0, 0, 38, 40) },
            { "FlowerOfFire", new Src("Item_112", 0, 0, 30, 28) },
            { "GoldBow", new Src("Item_3516", 0, 0, 16, 32) },
            { "GoldBroadsword", new Src("Item_3520", 0, 0, 40, 40) },
            { "GoldChest", new Src("Item_306", 0, 0, 32, 28) },
            { "Handgun", new Src("Item_164", 0, 0, 42, 30) },
            { "HornetStaff", new Src("Item_2364", 0, 0, 34, 34) },
            { "ImpStaff", new Src("Item_2365", 0, 0, 46, 46) },
            { "Kaleidoscope", new Src("Item_4914", 0, 0, 42, 44) },
            { "Key", new Src("Item_327", 0, 0, 14, 24) },
            { "LastPrism", new Src("Item_3541", 0, 0, 26, 30) },
            { "LeatherWhip", new Src("Item_4672", 0, 0, 38, 34) },
            { "M_Prism", new Src("Projectile_946", 0, 0, 94, 30) },
            { "Mana", new Src("Mana", 0, 0, 22, 24) },
            { "MeteorStaff", new Src("Item_2750", 0, 0, 44, 42) },
            { "MoltenFury", new Src("Item_120", 0, 0, 18, 36) },
            { "MorningStar", new Src("Item_4679", 0, 0, 40, 26) },
            { "Muramasa", new Src("Item_155", 0, 0, 64, 64) },
            { "NightsEdge", new Src("Item_273", 0, 0, 50, 54) },
            { "P_AmethystChain", new Src("GemChain_1", 0, 0, 14, 8) },
            { "P_AmethystHook", new Src("Projectile_230", 0, 0, 18, 20) },
            { "P_Arrow", new Src("Projectile_1", 0, 0, 14, 32) },
            { "P_Bee", new Src("Projectile_181", 0, 0, 10, 10) },
            { "P_Bullet", new Src("Projectile_14", 0, 0, 2, 20) },
            { "P_Diamond", new Src("Projectile_126", 0, 0, 54, 54) },
            { "P_EnchantedBeam", new Src("Projectile_173", 0, 0, 28, 28) },
            { "P_Fire", new Src("Projectile_15", 0, 0, 16, 16) },
            { "P_FireArrow", new Src("Projectile_2", 0, 0, 14, 32) },
            { "P_HolyArrow", new Src("Projectile_91", 0, 0, 14, 32) },
            { "P_Laser", new Src("Projectile_632", 0, 0, 26, 78) },
            { "P_Meteor", new Src("Projectile_424", 0, 0, 36, 36) },
            { "P_Muramasa", new Src("Projectile_977", 1, 5, 16, 62) },
            { "P_Ruby", new Src("Projectile_125", 0, 0, 54, 54) },
            { "P_Skull", new Src("Projectile_837", 2, 2, 22, 26) },
            { "P_SlashArc", new Src("Projectile_972", 0, 0, 170, 680) },
            { "P_TerraBeam", new Src("Projectile_132", 0, 0, 40, 40) },
            { "P_Tombstone", new Src("Projectile_43", 0, 0, 26, 30) },
            { "P_Typhoon", new Src("Projectile_409", 4, 4, 62, 62) },
            { "P_Water", new Src("Projectile_22", 0, 0, 12, 12) },
            { "PirateStaff", new Src("Item_2584", 0, 0, 32, 32) },
            { "RazorbladeTyphoon", new Src("Item_2622", 0, 0, 28, 30) },
            { "RubyStaff", new Src("Item_743", 0, 0, 42, 40) },
            { "SDMG", new Src("Item_1553", 0, 0, 66, 32) },
            { "Snapthorn", new Src("Item_4913", 0, 0, 42, 40) },
            { "SpiderStaff", new Src("Item_2551", 0, 0, 42, 42) },
            { "SpinalTap", new Src("Item_5074", 0, 0, 38, 30) },
            { "TempestStaff", new Src("Item_2621", 0, 0, 34, 34) },
            { "TerraBlade", new Src("Item_757", 0, 0, 46, 54) },
            { "Terraprisma", new Src("Item_5005", 0, 0, 52, 52) },
            { "Tsunami", new Src("Item_2624", 0, 0, 28, 58) },
            { "VampireFrogStaff", new Src("Item_4273", 0, 0, 46, 44) },
            { "WandOfSparking", new Src("Item_3069", 0, 0, 28, 28) },
            { "Wh_CoolWhip_H", new Src("Projectile_912", 6, 4, 10, 20) },
            { "Wh_CoolWhip_S", new Src("Projectile_912", 6, 60, 10, 12) },
            { "Wh_CoolWhip_T", new Src("Projectile_912", 4, 104, 14, 24) },
            { "Wh_DarkHarvest_H", new Src("Projectile_849", 18, 2, 10, 24) },
            { "Wh_DarkHarvest_S", new Src("Projectile_849", 18, 62, 10, 14) },
            { "Wh_DarkHarvest_T", new Src("Projectile_849", 16, 118, 28, 20) },
            { "Wh_Durendal_H", new Src("Projectile_847", 0, 0, 22, 26) },
            { "Wh_Durendal_S", new Src("Projectile_847", 6, 66, 10, 14) },
            { "Wh_Durendal_T", new Src("Projectile_847", 6, 122, 10, 16) },
            { "Wh_Firecracker_H", new Src("Projectile_913", 2, 4, 18, 22) },
            { "Wh_Firecracker_S", new Src("Projectile_913", 4, 64, 14, 14) },
            { "Wh_Firecracker_T", new Src("Projectile_913", 2, 112, 18, 26) },
            { "Wh_Kaleidoscope_H", new Src("Projectile_915", 4, 4, 14, 22) },
            { "Wh_Kaleidoscope_S", new Src("Projectile_915", 6, 72, 10, 16) },
            { "Wh_Kaleidoscope_T", new Src("Projectile_915", 4, 128, 14, 30) },
            { "Wh_LeatherWhip_H", new Src("Projectile_841", 0, 0, 10, 26) },
            { "Wh_LeatherWhip_S", new Src("Projectile_841", 2, 64, 6, 16) },
            { "Wh_LeatherWhip_T", new Src("Projectile_841", 0, 120, 10, 18) },
            { "Wh_MorningStar_H", new Src("Projectile_848", 0, 2, 14, 24) },
            { "Wh_MorningStar_S", new Src("Projectile_848", 2, 66, 10, 10) },
            { "Wh_MorningStar_T", new Src("Projectile_848", 0, 122, 14, 14) },
            { "Wh_Snapthorn_H", new Src("Projectile_914", 6, 2, 10, 22) },
            { "Wh_Snapthorn_S", new Src("Projectile_914", 2, 62, 20, 18) },
            { "Wh_Snapthorn_T", new Src("Projectile_914", 0, 112, 22, 26) },
            { "Wh_SpinalTapWhip_H", new Src("Projectile_952", 2, 2, 16, 26) },
            { "Wh_SpinalTapWhip_S", new Src("Projectile_952", 4, 70, 14, 12) },
            { "Wh_SpinalTapWhip_T", new Src("Projectile_952", 4, 130, 14, 18) },
            { "WoodenBow", new Src("Item_39", 0, 0, 16, 32) },
            { "Zenith", new Src("Item_4956", 0, 0, 56, 56) },
            // Miniony: klatki wyciete z arkuszy pociskow i ulozone w pionowy pasek.
            { "M_Finch", new Src("Projectile_759", 0, 0, 28, 22, 24, 4) },
            { "M_Flinx", new Src("Projectile_951", 2, 0, 40, 40, 42, 12) },
            { "M_Frog", new Src("Projectile_758", 52, 24, 38, 28, 54, 6) },
            { "M_Hornet", new Src("Projectile_373", 2, 2, 40, 34, 38, 3) },
            { "M_Imp", new Src("Projectile_375", 2, 0, 52, 44, 46, 8) },
            { "M_Spider", new Src("Projectile_390", 4, 16, 24, 16, 34, 4) },
            { "M_Pirate", new Src("Projectile_393", 0, 422, 40, 38, 42, 4) },
            { "M_Tempest", new Src("Projectile_407", 0, 0, 40, 40, 44, 6) }
        };

        private static readonly Dictionary<string, Color32[]> PixelCache = new Dictionary<string, Color32[]>();
        private static readonly Dictionary<string, Vector2> SizeCache = new Dictionary<string, Vector2>();
        private static Xnb.FontData font;
        private static bool fontTried;

        // Czysci wszystko, co wczytano (albo nie udalo sie wczytac) z Terrarii - po zmianie sciezki.
        public static void ReloadAll()
        {
            TerrariaFiles.Reset();
            PixelCache.Clear();
            SizeCache.Clear();
            font = null;
            fontTried = false;
            Assets.Reset();
            Sounds.Reset();
            TerrariaFont.Reset();
            PlayerLook.Changed();
            if (TerrariaMusic.Instance != null) TerrariaMusic.Instance.ResetBank();
        }

        public static bool Available
        {
            get { return TerrariaFiles.ContentDir() != null; }
        }

        // Czy grafika moda pochodzi z Terrarii (ma wpis w tabeli).
        public static bool IsTerrariaAsset(string name)
        {
            return Sources.ContainsKey(name) || name == "Cursor" || name == "Player" || name == "Font";
        }

        // Czy w Terrarii jest taki obrazek (bez wczytywania i bez ostrzezen w logu).
        public static bool HasImage(string file)
        {
            string dir = TerrariaFiles.ContentDir();
            if (dir == null) return false;
            return File.Exists(Path.Combine(Path.Combine(dir, "Images"), file.Replace('/', Path.DirectorySeparatorChar) + ".xnb"));
        }

        // Piksele obrazka Terrarii (uklad: wiersz 0 = GORA obrazka). Null gdy brak pliku.
        public static Color32[] Pixels(string file, out int w, out int h)
        {
            w = h = 0;
            Color32[] px;
            if (PixelCache.TryGetValue(file, out px))
            {
                if (px != null)
                {
                    Vector2 s = SizeCache[file];
                    w = (int)s.x;
                    h = (int)s.y;
                }
                return px;
            }
            PixelCache[file] = null;
            string dir = TerrariaFiles.ContentDir();
            if (dir == null) return null;
            string path = Path.Combine(Path.Combine(dir, "Images"), file.Replace('/', Path.DirectorySeparatorChar) + ".xnb");
            if (!File.Exists(path))
            {
                string ui = Path.Combine(Path.Combine(Path.Combine(dir, "Images"), "UI"), file + ".xnb");
                if (!File.Exists(ui))
                {
                    TerrariaArsenal.Instance.LogWarn("Brak pliku Terrarii: Images/" + file + ".xnb");
                    return null;
                }
                path = ui;
            }
            try
            {
                byte[] b = Xnb.Load(path);
                int p = Xnb.SkipHeader(b);
                Color32[] bottomUp = Xnb.ReadTexture(b, ref p, out w, out h);
                px = new Color32[w * h];
                for (int y = 0; y < h; y++) Array.Copy(bottomUp, (h - 1 - y) * w, px, y * w, w);
                PixelCache[file] = px;
                SizeCache[file] = new Vector2(w, h);
                return px;
            }
            catch (Exception e)
            {
                TerrariaArsenal.Instance.LogError("Nie udalo sie odczytac " + file + ": " + e.Message);
                return null;
            }
        }

        // Tekstura Unity z pikseli w ukladzie "od gory".
        public static Texture2D ToTexture(Color32[] topDown, int w, int h)
        {
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++) Array.Copy(topDown, y * w, px, (h - 1 - y) * w, w);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        // Kopiuje prostokat (x, y, w, h) z obrazka do bufora docelowego w miejscu (dx, dy); poza obrazkiem = przezroczyste.
        public static void Blit(Color32[] src, int sw, int sh, int x, int y, int w, int h,
                                Color32[] dst, int dw, int dx, int dy)
        {
            for (int j = 0; j < h; j++)
            {
                int sy = y + j;
                if (sy < 0 || sy >= sh) continue;
                for (int i = 0; i < w; i++)
                {
                    int sx = x + i;
                    if (sx < 0 || sx >= sw) continue;
                    dst[(dy + j) * dw + dx + i] = src[sy * sw + sx];
                }
            }
        }

        // Zbudowana grafika moda z plikow Terrarii albo null.
        public static Texture2D Build(string name)
        {
            if (name == "Cursor") return BuildCursor();
            Src s;
            if (!Sources.TryGetValue(name, out s)) return null;
            int sw, sh;
            Color32[] px = Pixels(s.File, out sw, out sh);
            if (px == null) return null;
            int count = Math.Max(1, s.Count);
            Color32[] outPx = new Color32[s.W * s.H * count];
            for (int i = 0; i < count; i++)
            {
                Blit(px, sw, sh, s.X, s.Y + i * s.Stride, s.W, s.H, outPx, s.W, 0, i * s.H);
            }
            return ToTexture(outPx, s.W, s.H * count);
        }

        // Kursor Terrarii: obwodka Cursor_11 + srodek Cursor_0 w kolorze kursora.
        private static Texture2D BuildCursor()
        {
            int bw, bh, cw, ch;
            Color32[] border = Pixels("UI/Cursor_11", out bw, out bh);
            Color32[] inner = Pixels("UI/Cursor_0", out cw, out ch);
            if (border == null || inner == null) return null;
            Color32[] outPx = new Color32[18 * 18];
            Layer(outPx, 18, 18, border, bw, bh, 0, 0, bw, bh, 0, 0, new Color32(230, 230, 230, 255));
            Layer(outPx, 18, 18, inner, cw, ch, 0, 0, cw, ch, 2, 2, new Color32(255, 50, 95, 255));
            return ToTexture(outPx, 18, 18);
        }

        // Naklada (alpha "over") prostokat z obrazka zabarwiony kolorem tint (mnozenie jak w Terrarii).
        public static void Layer(Color32[] dst, int dw, int dh, Color32[] src, int sw, int sh,
                                 int sx, int sy, int w, int h, int dx, int dy, Color32 tint)
        {
            for (int j = 0; j < h; j++)
            {
                int ty = dy + j, yy = sy + j;
                if (ty < 0 || ty >= dh || yy < 0 || yy >= sh) continue;
                for (int i = 0; i < w; i++)
                {
                    int tx = dx + i, xx = sx + i;
                    if (tx < 0 || tx >= dw || xx < 0 || xx >= sw) continue;
                    Color32 c = src[yy * sw + xx];
                    if (c.a == 0) continue;
                    int r = c.r * tint.r / 255, g = c.g * tint.g / 255, b = c.b * tint.b / 255;
                    int o = ty * dw + tx;
                    Color32 d = dst[o];
                    if (c.a == 255 || d.a == 0)
                    {
                        dst[o] = new Color32((byte)r, (byte)g, (byte)b, c.a);
                        continue;
                    }
                    float sa = c.a / 255f, da = d.a / 255f;
                    float oa = sa + da * (1f - sa);
                    dst[o] = new Color32(
                        (byte)Mathf.RoundToInt((r * sa + d.r * da * (1f - sa)) / oa),
                        (byte)Mathf.RoundToInt((g * sa + d.g * da * (1f - sa)) / oa),
                        (byte)Mathf.RoundToInt((b * sa + d.b * da * (1f - sa)) / oa),
                        (byte)Mathf.RoundToInt(oa * 255f));
                }
            }
        }

        public static AudioClip Sound(string name)
        {
            string dir = TerrariaFiles.ContentDir();
            if (dir == null) return null;
            string path = Path.Combine(Path.Combine(dir, "Sounds"), name + ".xnb");
            if (!File.Exists(path)) return null;
            try
            {
                return Xnb.LoadSound(path, "TA_" + name);
            }
            catch (Exception e)
            {
                TerrariaArsenal.Instance.LogError("Nie udalo sie odczytac dzwieku " + name + ": " + e.Message);
                return null;
            }
        }

        // Czcionka Mouse_Text (tylko potrzebne znaki: ASCII + polskie litery).
        public static Xnb.FontData Font()
        {
            if (fontTried) return font;
            fontTried = true;
            string dir = TerrariaFiles.ContentDir();
            if (dir == null) return null;
            string path = Path.Combine(Path.Combine(dir, "Fonts"), "Mouse_Text.xnb");
            if (!File.Exists(path)) return null;
            const string extra = "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ–·…";
            try
            {
                font = Xnb.LoadFont(path, delegate(char c) { return (c >= 32 && c < 127) || extra.IndexOf(c) >= 0; });
                TerrariaArsenal.Instance.Log("Czcionka Terrarii: " + font.Glyphs.Count + " znakow");
            }
            catch (Exception e)
            {
                TerrariaArsenal.Instance.LogError("Nie udalo sie odczytac czcionki: " + e.Message);
                font = null;
            }
            return font;
        }
    }
}
