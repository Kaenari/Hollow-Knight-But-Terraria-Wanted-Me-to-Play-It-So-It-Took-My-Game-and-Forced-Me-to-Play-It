// Ladowanie sprite'ow wbudowanych w DLL (folder res, dolaczany przez /resource).

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TerrariaArsenal
{
    public static class Assets
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite[]> FrameCache = new Dictionary<string, Sprite[]>();

        public static void Reset()
        {
            Cache.Clear();
            FrameCache.Clear();
            TexCache.Clear();
        }

        // pixelsPerUnit: im wiecej, tym mniejszy sprite w swiecie gry.
        public static Sprite Get(string name, float pixelsPerUnit)
        {
            string key = name + "@" + pixelsPerUnit;
            Sprite cached;
            if (Cache.TryGetValue(key, out cached)) return cached;

            // Najpierw z instalacji Terrarii gracza, potem (wersja deweloperska) z DLL.
            Texture2D tex = LoadTexture(name);
            if (tex == null)
            {
                Cache[key] = null;
                return null;
            }
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            Cache[key] = sprite;
            return sprite;
        }

        private static readonly Dictionary<string, Texture2D> TexCache = new Dictionary<string, Texture2D>();

        private static Texture2D LoadTexture(string name)
        {
            Texture2D tex;
            if (TexCache.TryGetValue(name, out tex)) return tex;
            tex = null;
            if (TerrariaContent.IsTerrariaAsset(name) && name != "Player" && name != "Font")
            {
                tex = TerrariaContent.Build(name);
            }
            Texture2D embedded = LoadEmbedded(name);
            if (tex != null && embedded != null) Compare(name, tex, embedded);
            if (tex == null)
            {
                tex = embedded;
                if (tex != null && TerrariaContent.IsTerrariaAsset(name))
                {
                    TerrariaArsenal.Instance.LogWarn("Grafika " + name + " z DLL (brak w Terrarii)");
                }
            }
            TexCache[name] = tex;
            return tex;
        }

        private static Texture2D LoadEmbedded(string name)
        {
            Stream s = typeof(Assets).Assembly.GetManifestResourceStream("TerrariaArsenal." + name + ".png");
            if (s == null) return null;
            byte[] bytes = new byte[s.Length];
            s.Read(bytes, 0, bytes.Length);
            s.Close();
            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, bytes);
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        // Kontrola (wersja deweloperska): grafika z Terrarii powinna byc identyczna z wbudowana.
        private static void Compare(string name, Texture2D a, Texture2D b)
        {
            if (a.width != b.width || a.height != b.height)
            {
                TerrariaArsenal.Instance.LogWarn("Kontrola grafik: " + name + " rozmiar " + a.width + "x" + a.height +
                                                " zamiast " + b.width + "x" + b.height);
                return;
            }
            Color32[] pa = a.GetPixels32(), pb = b.GetPixels32();
            int diff = 0;
            for (int i = 0; i < pa.Length; i++)
            {
                if (pa[i].a == 0 && pb[i].a == 0) continue;
                if (Mathf.Abs(pa[i].r - pb[i].r) > 3 || Mathf.Abs(pa[i].g - pb[i].g) > 3 ||
                    Mathf.Abs(pa[i].b - pb[i].b) > 3 || Mathf.Abs(pa[i].a - pb[i].a) > 3) diff++;
            }
            if (diff > 0) TerrariaArsenal.Instance.LogWarn("Kontrola grafik: " + name + " rozni sie w " + diff + " pikselach");
            else TerrariaArsenal.Instance.Log("Kontrola grafik: " + name + " OK");
        }

        // Pionowy pasek klatek (jak arkusze minionow w Terrarii) -> osobne sprite'y.
        public static Sprite[] GetFrames(string name, float pixelsPerUnit, int count)
        {
            Sprite whole = Get(name, pixelsPerUnit);
            if (whole == null) return null;
            if (count <= 1) return new Sprite[] { whole };
            string key = name + "@" + pixelsPerUnit + "#" + count;
            Sprite[] cached;
            if (FrameCache.TryGetValue(key, out cached)) return cached;
            Texture2D tex = whole.texture;
            int fh = tex.height / count;
            Sprite[] frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                // Tekstura w Unity liczy y od dolu, klatka 0 jest na gorze obrazka.
                Rect r = new Rect(0, tex.height - (i + 1) * fh, tex.width, fh);
                frames[i] = Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), pixelsPerUnit);
            }
            FrameCache[key] = frames;
            return frames;
        }

        // Sprite z wlasnym punktem zaczepienia (np. rekojesc miecza w lewym dolnym rogu).
        public static Sprite GetPivot(string name, float pixelsPerUnit, Vector2 pivot)
        {
            string key = name + "@" + pixelsPerUnit + "^" + pivot.x + "," + pivot.y;
            Sprite cached;
            if (Cache.TryGetValue(key, out cached)) return cached;
            Sprite whole = Get(name, pixelsPerUnit);
            Sprite s = whole == null ? null : Sprite.Create(whole.texture,
                new Rect(0, 0, whole.texture.width, whole.texture.height), pivot, pixelsPerUnit);
            Cache[key] = s;
            return s;
        }

        // Ikona broni; bronie bez wlasnej grafiki dostaja ikone startowej broni swojej klasy.
        public static Sprite WeaponIcon(Weapon w, float pixelsPerUnit)
        {
            Sprite s = Get(w.Id, pixelsPerUnit);
            if (s != null) return s;
            Weapon starter = Weapons.Get(0, w.Class);
            return Get(starter.Id, pixelsPerUnit);
        }
    }
}
