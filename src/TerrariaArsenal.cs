// Terraria Arsenal - mod do Hollow Knight (Modding API 1.5.78)
// Wersja 0.13.0: grafiki, dzwieki i muzyka czytane z instalacji Terrarii gracza, wyglad postaci do ustawienia.
// Wczesniej: pokoj ze skrzyniami, 36 broni z Terrarii, osobna mana odnawiajaca sie sama, animowane miniony,
// liczby obrazen, krytyki i efekty mieczy, postac z Terrarii i akcesoria ruchu zamiast umiejetnosci HK,
// dzwieki z Terrarii, atak LPM, celowanie kursorem, bicze przywolywacza
// i pasek zdrowia bossow.
// Kod zgodny z C# 5, zeby kompilowal sie tez wbudowanym kompilatorem Windows.

using System.Collections.Generic;
using HutongGames.PlayMaker;
using Modding;
using UnityEngine;

namespace TerrariaArsenal
{
    // Stan zapisywany razem z save'em HK (osobno dla kazdego slotu).
    public class SaveData
    {
        public List<int> UnlockedRooms = new List<int>();
        public List<int> OpenedRooms = new List<int>();
        public List<string> Weapons = new List<string>();
        public int KeyForRoom = -1; // pokoj, do ktorego gracz ma klucz (-1 = brak)
        // Wybrany miecz i wybrana laska przywolywacza (mozna miec kilka broni z kazdej klasy).
        public string ActiveMelee = "";
        public string ActiveSummon = "";
    }

    // Ustawienia globalne moda (plik TerrariaArsenal.GlobalSettings.json obok zapisow gry).
    public class ArsenalSettings
    {
        // true = oprocz haka zostaje tez skok od sciany z Mantis Claw (awaryjnie, gdyby gdzies nie dalo sie przejsc).
        public bool KeepWallJump = false;
        // Folder Terrarii (z Content). Pusty = szukaj automatycznie przez Steam.
        public string TerrariaPath = "";
        // true = muzyka biomow Terrarii takze w lokacjach (nie tylko na bossach).
        public bool TerrariaAreaMusic = true;
        // Wyglad postaci z Terrarii dla kazdego slotu zapisu (1-4), ustawiany na ekranie wyboru zapisu.
        public Dictionary<int, Look> Looks = new Dictionary<int, Look>();
    }

    public class TerrariaArsenal : Mod, ILocalSettings<SaveData>, IGlobalSettings<ArsenalSettings>
    {
        public ArsenalSettings Settings = new ArsenalSettings();

        public void OnLoadGlobal(ArsenalSettings s)
        {
            Settings = s ?? new ArsenalSettings();
            if (Settings.Looks == null) Settings.Looks = new Dictionary<int, Look>();
        }

        // Zapis ustawien od razu (np. po zmianie wygladu postaci w menu).
        public void SaveSettings()
        {
            SaveGlobalSettings();
        }

        public ArsenalSettings OnSaveGlobal()
        {
            return Settings;
        }

        public static TerrariaArsenal Instance;
        public SaveData Data = new SaveData();

        // Flaga w PlayerData -> numer pokoju ze skrzyniami.
        private static readonly Dictionary<string, int> BossRooms = new Dictionary<string, int>
        {
            { "killedFalseKnight", 1 },
            { "hornet1Defeated", 2 },
            { "defeatedMantisLords", 3 },
            { "killedMageLord", 4 },        // Soul Master
            { "killedInfectedKnight", 5 },  // Broken Vessel
            { "killedBlackKnight", 6 },     // Watcher Knights
            { "killedMegaJellyfish", 7 },   // Uumuu
            { "killedHollowKnight", 8 }
        };

        // Nazwa widoczna w grze (lista modow). Wewnetrznie mod nazywa sie dalej TerrariaArsenal
        // (DLL, folder, plik ustawien TerrariaArsenal.GlobalSettings.json).
        public const string DisplayName = "Hollow Knight: But Terraria Wanted Me to Play It, So It Took My Game and Forced Me to Play It";

        public TerrariaArsenal() : base(DisplayName)
        {
            Instance = this;
        }

        public override string GetVersion()
        {
            return "1.0.0.0";
        }

        public override void Initialize()
        {
            Log(DisplayName + " " + GetVersion() + " zaladowany.");
            ModHooks.SetPlayerBoolHook += OnSetPlayerBool;
            ModHooks.NewGameHook += OnNewGame;
            ModHooks.AfterSavegameLoadHook += OnSaveLoaded;
            ModHooks.HitInstanceHook += OnHitInstance;
            ModHooks.AfterAttackHook += Effects.OnAttack;
            On.HealthManager.TakeDamage += OnTakeDamage;
            ModHooks.LanguageGetHook += Accessories.Translate;
            ModHooks.AfterTakeDamageHook += OnHeroDamage;
            On.HeroController.DoAttack += OnDoAttack;
            On.HeroController.CanNailCharge += OnCanNailCharge;
            On.HeroController.CanWallJump += OnCanWallJump;
            On.HeroController.CanWallSlide += OnCanWallSlide;
            ModHooks.BeforePlayerDeadHook += OnHeroDead;
            ModHooks.BeforeAddHealthHook += OnHeroHeal;

            GameObject runner = new GameObject("TerrariaArsenal_Runner");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<Runner>();
            runner.AddComponent<TerrariaPlayer>();
            runner.AddComponent<GrappleHook>();
            runner.AddComponent<TerrariaMusic>();
            runner.AddComponent<CharacterMenu>();
        }

        // --- Zapis w save'ie ---
        public void OnLoadLocal(SaveData s)
        {
            Data = s ?? new SaveData();
        }

        public SaveData OnSaveLocal()
        {
            return Data;
        }

        // Najnizszy odblokowany pokoj, w ktorym gracz jeszcze nie wybral broni (-1 = brak).
        public int PendingRoom()
        {
            int best = -1;
            foreach (int r in Data.UnlockedRooms)
            {
                if (!Data.OpenedRooms.Contains(r) && (best < 0 || r < best)) best = r;
            }
            return best;
        }

        // --- Hooki ---
        private void OnNewGame()
        {
            Data = new SaveData();
            UnlockRoom(0);
            if (Runner.Instance != null) Runner.Instance.ResetActive();
        }

        private void OnSaveLoaded(SaveGameData save)
        {
            // 0.7.0: Spinal Tap to bicz, wiec laska z pokoju 5 to teraz Spider Staff.
            int old = Data.Weapons.IndexOf("SpinalTap");
            if (old >= 0) Data.Weapons[old] = "SpiderStaff";
            UnlockRoom(0);
            if (Runner.Instance != null) Runner.Instance.ResetActive();
            foreach (KeyValuePair<string, int> pair in BossRooms)
            {
                if (PlayerData.instance.GetBool(pair.Key)) UnlockRoom(pair.Value);
            }
            Log("Wczytano save. Odblokowane pokoje: " + Data.UnlockedRooms.Count +
                ", bronie: " + string.Join(", ", Data.Weapons.ToArray()));
        }

        private bool OnSetPlayerBool(string name, bool orig)
        {
            int room;
            if (orig && BossRooms.TryGetValue(name, out room)) UnlockRoom(room);
            Accessory acc = Accessories.ByFlag(name);
            if (orig && acc != null && PlayerData.instance != null && !PlayerData.instance.GetBool(name) && Runner.Instance != null)
            {
                Runner.Instance.ShowMessage("Nowe akcesorium: " + acc.Name + " - " + acc.Desc + ".");
            }
            return orig;
        }

        // Bronie wojownika: gwozdz zadaje obrazenia najlepszej posiadanej broni, jesli sa wieksze.
        private HitInstance OnHitInstance(Fsm owner, HitInstance hit)
        {
            if (hit.AttackType == AttackTypes.Nail)
            {
                int melee = Runner.MeleeDamage();
                if (melee > hit.DamageDealt) hit.DamageDealt = melee;
            }
            return hit;
        }

        // Kazde obrazenie zadane wrogowi: krytyk dla broni z moda + liczba nad wrogiem.
        private void OnTakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hit)
        {
            bool crit = false;
            bool ours = false;
            try
            {
                ours = Effects.IsOurs(hit);
                if (ours && UnityEngine.Random.value < Effects.CritChance)
                {
                    hit.DamageDealt *= 2;
                    crit = true;
                }
            }
            catch (System.Exception e)
            {
                LogError(e.ToString());
            }

            int before = self.hp;
            orig(self, hit);

            try
            {
                int dealt = before - self.hp;
                if (dealt <= 0) dealt = Mathf.RoundToInt(hit.DamageDealt * hit.Multiplier);
                Vector3 top = self.transform.position;
                Collider2D col = self.GetComponent<Collider2D>();
                if (col != null) top = new Vector3(col.bounds.center.x, col.bounds.max.y, top.z);
                DamageNumbers.Add(top, dealt, crit);
                BossBar.OnDamaged(self, before);
                if (hit.AttackType == AttackTypes.Nail) Effects.OnNailHit(self);
            }
            catch (System.Exception e)
            {
                LogError(e.ToString());
            }
        }

        // Gdy w rece jest luk/magia, przycisk ataku strzela zamiast ciac gwozdziem.
        private void OnDoAttack(On.HeroController.orig_DoAttack orig, HeroController self)
        {
            if (Runner.BlockNail()) return;
            orig(self);
        }

        // Amethyst Hook zastepuje skok od sciany i slizganie (chyba ze KeepWallJump w ustawieniach).
        private bool OnCanWallJump(On.HeroController.orig_CanWallJump orig, HeroController self)
        {
            if (GrappleHook.BlocksWallJump()) return false;
            return orig(self);
        }

        private bool OnCanWallSlide(On.HeroController.orig_CanWallSlide orig, HeroController self)
        {
            if (GrappleHook.BlocksWallJump()) return false;
            return orig(self);
        }

        private bool OnCanNailCharge(On.HeroController.orig_CanNailCharge orig, HeroController self)
        {
            if (Runner.BlockNail()) return false;
            return orig(self);
        }

        // Dzwieki Terrarii przy otrzymaniu obrazen i smierci.
        private int OnHeroDamage(int hazardType, int damage)
        {
            if (damage > 0 && TerrariaPlayer.Enabled) Sounds.PlayRandom("Player_Hit_", 3, 0.9f);
            return damage;
        }

        // Leczenie (Focus, Lifeblood itp.): zielone liczby jak w Terrarii.
        private int OnHeroHeal(int amount)
        {
            HeroController hero = HeroController.instance;
            if (hero != null && amount > 0)
                DamageNumbers.AddHeal(hero.transform.position + new Vector3(0f, 1.1f, 0f), amount);
            return amount;
        }

        private void OnHeroDead()
        {
            if (TerrariaPlayer.Instance != null) TerrariaPlayer.Instance.OnDeath();
            if (TerrariaPlayer.Enabled) Sounds.Play("Player_Killed", 0.9f);
        }

        public void UnlockRoom(int room)
        {
            if (Data.UnlockedRooms.Contains(room)) return;
            Data.UnlockedRooms.Add(room);
            Log("Odblokowano pokoj ze skrzyniami nr " + room + ": " + global::TerrariaArsenal.Weapons.RoomNames[room]);
        }
    }
}
