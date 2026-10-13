// Pasek zdrowia bossa jak w Terrarii (grafika UI_BossBar z Terrarii, liczby HP na pasku).
// Pokazuje sie po pierwszym trafieniu bossa i znika po jego smierci albo zmianie lokacji.
// Bossowie z kilku czesci (Mantis Lords, Watcher Knights, Oblobbles...) maja jeden wspolny pasek.

using System.Collections.Generic;
using UnityEngine;

namespace TerrariaArsenal
{
    public static class BossBar
    {
        // Poczatek nazwy obiektu w HK -> nazwa na pasku.
        private static readonly string[,] Names =
        {
            { "False Knight Dream", "Failed Champion" },
            { "False Knight", "False Knight" },
            { "Hornet Boss 1", "Hornet" },
            { "Hornet Boss 2", "Hornet Sentinel" },
            { "Mantis Traitor Lord", "Traitor Lord" },
            { "Mantis Lord", "Mantis Lords" },
            { "Dream Mage Lord", "Soul Tyrant" },
            { "Mage Lord", "Soul Master" },
            { "Infected Knight", "Broken Vessel" },
            { "Lost Kin", "Lost Kin" },
            { "Black Knight", "Watcher Knights" },
            { "Mega Jellyfish", "Uumuu" },
            { "HK Prime", "Pure Vessel" },
            { "Hollow Knight Boss", "The Hollow Knight" },
            { "Absolute Radiance", "Absolute Radiance" },
            { "Radiance", "The Radiance" },
            { "Giant Fly", "Gruz Mother" },
            { "Giant Buzzer", "Vengefly King" },
            { "Mawlek Body", "Brooding Mawlek" },
            { "Mega Moss Charger", "Massive Moss Charger" },
            { "White Defender", "White Defender" },
            { "Dung Defender", "Dung Defender" },
            { "Fluke Mother", "Flukemarm" },
            { "Mimic Spider", "Nosk" },
            { "Hive Knight", "Hive Knight" },
            { "Nightmare Grimm", "Nightmare King Grimm" },
            { "Grimm Boss", "Troupe Master Grimm" },
            { "Jar Collector", "The Collector" },
            { "Lobster", "God Tamer" },
            { "Lancer", "God Tamer" },
            { "Oro", "Brothers Oro & Mato" },
            { "Mato", "Brothers Oro & Mato" },
            { "Sheo Boss", "Paintmaster Sheo" },
            { "Sly Boss", "Great Nailsage Sly" },
            { "Grey Prince", "Grey Prince Zote" },
            { "Mega Zombie Beam Miner", "Crystal Guardian" },
            { "Zombie Beam Miner Rematch", "Enraged Guardian" },
            { "Mega Fat Bee", "Oblobbles" },
            { "Ghost Warrior Xero", "Xero" },
            { "Ghost Warrior Slug", "Gorb" },
            { "Ghost Warrior Marmu", "Marmu" },
            { "Ghost Warrior No Eyes", "No Eyes" },
            { "Ghost Warrior Hu", "Elder Hu" },
            { "Ghost Warrior Galien", "Galien" },
            { "Ghost Warrior Markoth", "Markoth" }
        };

        private class Part
        {
            public HealthManager Hm;
            public int MaxHp;
        }

        private static readonly List<Part> Parts = new List<Part>();
        private static string current;       // nazwa bossa na pasku (null = brak)
        public static string Current { get { return current; } }
        private static float scanTimer;
        private static float shownFrac = 1f; // plynne zjezdzanie paska

        public static string BossName(HealthManager hm)
        {
            if (hm == null) return null;
            string n = hm.gameObject.name.Replace("(Clone)", "").Trim();
            for (int i = 0; i < Names.GetLength(0); i++)
            {
                if (n.StartsWith(Names[i, 0])) return Names[i, 1];
            }
            return null;
        }

        // Flagi PlayerData ustawiane, gdy boss jest pokonany. Niektorzy bossowie (np. False Knight) po smierci
        // zostaja na scenie z hp > 0, wiec pasek znika po tej fladze, a nie dopiero po zmianie sceny.
        private static readonly Dictionary<string, string> DefeatFlags = new Dictionary<string, string>
        {
            { "killedFalseKnight", "False Knight" }, { "falseKnightDefeated", "False Knight" },
            { "falseKnightDreamDefeated", "Failed Champion" },
            { "killedHornet", "Hornet" }, { "hornet1Defeated", "Hornet" },
            { "hornetOutskirtsDefeated", "Hornet Sentinel" },
            { "defeatedMantisLords", "Mantis Lords" },
            { "killedTraitorLord", "Traitor Lord" },
            { "mageLordDefeated", "Soul Master" }, { "mageLordDreamDefeated", "Soul Tyrant" },
            { "killedInfectedKnight", "Broken Vessel" }, { "infectedKnightDreamDefeated", "Lost Kin" },
            { "defeatedMegaJelly", "Uumuu" },
            { "killedHollowKnight", "The Hollow Knight" },
            { "killedHollowKnightPrime", "Pure Vessel" },
            { "giantFlyDefeated", "Gruz Mother" }, { "giantBuzzerDefeated", "Vengefly King" },
            { "mawlekDefeated", "Brooding Mawlek" },
            { "megaMossChargerDefeated", "Massive Moss Charger" },
            { "defeatedDungDefender", "Dung Defender" }, { "whiteDefenderDefeated", "White Defender" },
            { "flukeMotherDefeated", "Flukemarm" },
            { "killedMimicSpider", "Nosk" },
            { "killedHiveKnight", "Hive Knight" },
            { "killedGrimm", "Troupe Master Grimm" }, { "defeatedNightmareGrimm", "Nightmare King Grimm" },
            { "collectorDefeated", "The Collector" },
            { "killedLobsterLancer", "God Tamer" },
            { "killedNailBros", "Brothers Oro & Mato" },
            { "killedPaintmaster", "Paintmaster Sheo" },
            { "killedNailsage", "Great Nailsage Sly" },
            { "greyPrinceDefeated", "Grey Prince Zote" },
            { "defeatedMegaBeamMiner", "Crystal Guardian" }, { "defeatedMegaBeamMiner2", "Enraged Guardian" }
        };

        private static string defeated;   // boss pokonany w tej scenie - jego pasek juz nie wraca

        public static void OnFlag(string flag)
        {
            string boss;
            if (!DefeatFlags.TryGetValue(flag, out boss)) return;
            defeated = boss;
            if (current == boss) Clear();
        }

        // Zmiana sceny: pasek znika, a pokonany boss moze miec pasek znowu (np. w Hall of Gods).
        public static void SceneChanged()
        {
            Clear();
            defeated = null;
        }

        public static void Clear()
        {
            Parts.Clear();
            current = null;
            shownFrac = 1f;
        }

        // Wywolywane przy kazdym obrazeniu (hook TakeDamage) - wlacza pasek dla bossa.
        public static void OnDamaged(HealthManager hm, int hpBefore)
        {
            string name = BossName(hm);
            if (name == null || name == defeated) return;
            if (current != name)
            {
                Parts.Clear();
                current = name;
                shownFrac = 1f;
            }
            Part p = Find(hm);
            if (p == null)
            {
                p = new Part();
                p.Hm = hm;
                Parts.Add(p);
            }
            p.MaxHp = Mathf.Max(p.MaxHp, hpBefore);
            Scan();
        }

        private static Part Find(HealthManager hm)
        {
            foreach (Part p in Parts)
            {
                if (p.Hm == hm) return p;
            }
            return null;
        }

        // Dopisuje pozostale czesci tego samego bossa (np. drugiego Mantis Lorda, druga faze Soul Mastera).
        private static void Scan()
        {
            if (current == null) return;
            foreach (HealthManager hm in Object.FindObjectsOfType<HealthManager>())
            {
                if (hm == null || BossName(hm) != current) continue;
                Part p = Find(hm);
                if (p == null)
                {
                    p = new Part();
                    p.Hm = hm;
                    Parts.Add(p);
                }
                if (!hm.GetIsDead()) p.MaxHp = Mathf.Max(p.MaxHp, hm.hp);
            }
        }

        public static void Tick(float dt)
        {
            if (current == null) return;
            scanTimer -= dt;
            if (scanTimer <= 0f)
            {
                scanTimer = 0.5f;
                Scan();
            }
            int hp, max;
            Totals(out hp, out max);
            if (max <= 0 || hp <= 0)
            {
                Clear();
                return;
            }
            float frac = hp / (float)max;
            shownFrac = Mathf.MoveTowards(shownFrac, frac, dt * 0.8f);
            if (shownFrac < frac) shownFrac = frac;
        }

        private static void Totals(out int hp, out int max)
        {
            hp = 0;
            max = 0;
            foreach (Part p in Parts)
            {
                max += p.MaxHp;
                if (p.Hm != null && !p.Hm.GetIsDead() && p.Hm.gameObject.activeInHierarchy)
                    hp += Mathf.Clamp(p.Hm.hp, 0, p.MaxHp);
            }
        }

        public static void Draw(float ui)
        {
            if (current == null) return;
            int hp, max;
            Totals(out hp, out max);
            if (max <= 0) return;

            Sprite frame = Assets.Get("BossBar_Frame", 32f);
            Sprite back = Assets.Get("BossBar_Back", 32f);
            Sprite fill = Assets.Get("BossBar_Fill", 32f);
            Sprite fillEnd = Assets.Get("BossBar_FillEnd", 32f);
            if (frame == null || back == null || fill == null) return;

            float k = 1.5f * ui;                         // skala pikseli Terrarii
            float w = 516f * k, h = 58f * k;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - h - 40f * ui;
            float bx = x + 32f * k, by = y + 22f * k, bw = 456f * k, bh = 22f * k;
            float frac = Mathf.Clamp01(hp / (float)max);

            GUI.DrawTexture(new Rect(bx, by, bw, bh), back.texture);
            // Bialy "slad" po stracie zycia (jak w Terrarii pasek zjezdza z opoznieniem).
            if (shownFrac > frac)
            {
                Color old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.55f);
                GUI.DrawTexture(new Rect(bx, by, bw * shownFrac, bh), fill.texture);
                GUI.color = old;
            }
            GUI.DrawTexture(new Rect(bx, by, bw * frac, bh), fill.texture);
            if (fillEnd != null && frac > 0f)
                GUI.DrawTexture(new Rect(bx + bw * frac - 2f * k, by, 2f * k, bh), fillEnd.texture);
            GUI.DrawTexture(new Rect(x, y, w, h), frame.texture);

            float ts = 0.9f * ui;
            TerrariaFont.DrawCentered(x + w * 0.5f, by + (bh - TerrariaFont.LineHeight(ts)) * 0.5f + 2f * ui,
                hp + "/" + max, Color.white, ts);
            float ns = 1.05f * ui;
            TerrariaFont.DrawCentered(x + w * 0.5f, y - TerrariaFont.LineHeight(ns) * 0.6f, current,
                new Color(1f, 0.85f, 0.4f), ns);
        }
    }
}
