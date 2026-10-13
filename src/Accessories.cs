// Umiejetnosci ruchu z HK pokazane jako akcesoria z Terrarii.
// Zdobywa sie je w tych samych miejscach co w HK; zmienia sie nazwa, ikona i komunikat.

using System.Collections.Generic;
using UnityEngine;

namespace TerrariaArsenal
{
    public class Accessory
    {
        public string Flag;      // bool w PlayerData ("" = wbudowane)
        public string Name;
        public string Icon;
        public string HkName;    // nazwa w HK (angielska), podmieniana w tekstach gry
        public string Desc;

        public Accessory(string flag, string name, string icon, string hkName, string desc)
        {
            Flag = flag;
            Name = name;
            Icon = icon;
            HkName = hkName;
            Desc = desc;
        }

        public bool Owned()
        {
            if (Flag.Length == 0) return true;
            return PlayerData.instance != null && PlayerData.instance.GetBool(Flag);
        }
    }

    public static class Accessories
    {
        public static readonly List<Accessory> All = new List<Accessory>
        {
            new Accessory("", "Hermes Boots", "A_HermesBoots", "", "biegnij dłużej, a przyspieszysz do sprintu"),
            new Accessory("hasDash", "Tabi", "A_Tabi", "Mothwing Cloak", "dash"),
            new Accessory("hasWalljump", "Amethyst Hook", "A_AmethystHook", "Mantis Claw", "hak pod klawiszem E: czepia się ścian i sufitów, skok odczepia"),
            new Accessory("hasDoubleJump", "Cloud in a Bottle", "A_CloudInABottle", "Monarch Wings", "podwójny skok"),
            new Accessory("hasSuperDash", "Shield of Cthulhu", "A_ShieldOfCthulhu", "Crystal Heart", "szarża (przytrzymaj przycisk super dasha)"),
            new Accessory("hasAcidArmour", "Flipper", "A_Flipper", "Isma's Tear", "pływanie w kwasie"),
            new Accessory("hasShadowDash", "Master Ninja Gear", "A_MasterNinjaGear", "Shade Cloak", "dash przez wrogów i ataki"),
            new Accessory("hasLantern", "Mining Helmet", "A_MiningHelmet", "Lumafly Lantern", "światło w ciemnych jaskiniach")
        };

        public static Accessory ByFlag(string flag)
        {
            foreach (Accessory a in All)
            {
                if (a.Flag == flag && flag.Length > 0) return a;
            }
            return null;
        }

        // Podmiana nazw w tekstach gry (ekwipunek, okienka po zdobyciu, dialogi).
        public static string Translate(string key, string sheet, string orig)
        {
            if (string.IsNullOrEmpty(orig)) return orig;
            string s = orig;
            foreach (Accessory a in All)
            {
                if (a.HkName.Length == 0) continue;
                if (s.IndexOf(a.HkName, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                s = s.Replace(a.HkName, a.Name);
                s = s.Replace(a.HkName.ToUpper(), a.Name.ToUpper());
                s = s.Replace(a.HkName.ToLower(), a.Name.ToLower());
            }
            return s;
        }

        // Rzad ikon zdobytych akcesoriow (lewy dolny rog, nad napisami).
        public static void DrawHud(float x, float y, float ui)
        {
            float size = 30f * ui;
            float step = 36f * ui;
            int n = 0;
            foreach (Accessory a in All)
            {
                if (!a.Owned()) continue;
                Sprite s = Assets.Get(a.Icon, 32f);
                if (s == null) continue;
                Texture2D t = s.texture;
                float k = Mathf.Min(size / t.width, size / t.height);
                float w = t.width * k, h = t.height * k;
                GUI.DrawTexture(new Rect(x + n * step + (size - w) * 0.5f, y + (size - h) * 0.5f, w, h), t);
                n++;
            }
        }
    }
}
