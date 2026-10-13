// Glowna petla moda: pokoje ze skrzyniami, bronie dystansowe/magiczne, minion i napisy.

using System.Collections.Generic;
using UnityEngine;

namespace TerrariaArsenal
{
    public class Runner : MonoBehaviour
    {
        public static Runner Instance;

        // Sterowanie (klawiatura): F - strzal/czar, R - zmiana broni.
        public KeyCode FireKey = KeyCode.F;
        public KeyCode CycleKey = KeyCode.R;

        private string lastScene = "";
        private float groundTime;
        private float fireCooldown;
        private int selected;          // indeks w liscie broni strzelajacych
        private const int SlotNail = 0, SlotWhip = 1, SlotShooter = 2;
        // Numery miejsc na pasku: 0 = sam gwozdz, MeleeBase+i = i-ty miecz, WhipBase+i = bicz i-tej laski, ShooterBase+i = bron strzelajaca.
        private const int MeleeBase = 1000, WhipBase = 2000, ShooterBase = 3000;
        private int activeKind = SlotNail; // co jest w rece: gwozdz/miecz, bicz albo bron strzelajaca
        private float whipCooldown;
        private bool activeInit;
        private float mouseActiveTime;  // >0 = myszka byla niedawno uzywana (widac kursor)
        private Vector3 lastMouse;
        private GameObject minion;
        private string message = "";
        private float messageTime;

        // Mana jak w Terrarii: osobna od SOUL, odnawia sie sama (szybciej, gdy stoisz w miejscu).
        public const float MaxMana = 100f;
        private const float ManaRegenDelay = 1f;   // przerwa po rzuceniu czaru
        private const float ManaRegenRate = 15f;   // many na sekunde
        private float mana = MaxMana;
        private float manaRegenDelay;

        private static SaveData Data
        {
            get { return TerrariaArsenal.Instance.Data; }
        }

        private void Awake()
        {
            Instance = this;
        }

        public void ShowMessage(string text)
        {
            message = text;
            messageTime = 4f;
        }

        private void Update()
        {
            if (messageTime > 0f) messageTime -= Time.deltaTime;
            fireCooldown -= Time.deltaTime;
            DamageNumbers.Tick(Time.deltaTime);

            HeroController hero = HeroController.instance;
            GameManager gm = GameManager.instance;
            if (hero == null || gm == null || !gm.IsGameplayScene())
            {
                return;
            }

            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (scene != lastScene)
            {
                lastScene = scene;
                groundTime = 0f;
                ChestRoom.Clear();
                DamageNumbers.Clear();
                BossBar.Clear();
            }

            BossBar.Tick(Time.deltaTime);
#if !RELEASE
            CheatCheck();
#endif
            UpdateRoom(hero);
            UpdateKeyAndChests(hero);
            UpdateMana(hero);
            UpdateWeapons(hero);
            UpdateMinion();
        }

#if !RELEASE
        // Tylko wersja testowa (build.bat): wpisanie QUPA daje wszystkie bronie i umiejetnosci.
        // build_release.bat kompiluje z RELEASE, wiec w wersji dla graczy tego kodu nie ma.
        private string cheatBuffer = "";

        private void CheatCheck()
        {
            string typed = Input.inputString;
            if (string.IsNullOrEmpty(typed)) return;
            cheatBuffer = (cheatBuffer + typed.ToUpperInvariant());
            if (cheatBuffer.Length > 8) cheatBuffer = cheatBuffer.Substring(cheatBuffer.Length - 8);
            if (!cheatBuffer.EndsWith("QUPA")) return;
            cheatBuffer = "";
            int added = 0;
            foreach (Weapon w in Weapons.All)
            {
                if (Data.Weapons.Contains(w.Id)) continue;
                Data.Weapons.Add(w.Id);
                added++;
            }
            // Wszystkie umiejetnosci ruchu (akcesoria + hak), zaklecia na max i Dream Nail.
            PlayerData pd = PlayerData.instance;
            if (pd != null)
            {
                string[] bools =
                {
                    "hasDash", "canDash", "hasWalljump", "canWallJump", "hasDoubleJump", "hasSuperDash", "canSuperDash",
                    "hasAcidArmour", "hasShadowDash", "canShadowDash", "hasLantern", "hasDreamNail", "dreamNailUpgraded",
                    "hasNailArt", "hasCyclone", "hasDashSlash", "hasUpwardSlash", "hasAllNailArts", "hasSpell"
                };
                foreach (string b in bools) pd.SetBool(b, true);
                pd.SetInt("fireballLevel", 2);
                pd.SetInt("quakeLevel", 2);
                pd.SetInt("screamLevel", 2);
            }
            activeInit = false;
            mana = MaxMana;
            TerrariaArsenal.Instance.Log("Kod QUPA: dodano " + added + " broni.");
            ShowMessage("Kod testowy: masz wszystkie bronie (" + Weapons.All.Count + ") i wszystkie umiejętności. Zmiana broni: " + CycleKey + " albo kółko myszy.");
        }
#endif

        // --- Pokoj ze skrzyniami ---
        private void UpdateRoom(HeroController hero)
        {
            // Nowa gra lub stary save bez danych moda: zawsze jest pokoj startowy.
            if (Data.UnlockedRooms.Count == 0) TerrariaArsenal.Instance.UnlockRoom(0);
            int room = TerrariaArsenal.Instance.PendingRoom();
            if (room < 0)
            {
                if (ChestRoom.SpawnedRoom >= 0) ChestRoom.Clear();
                return;
            }
            if (ChestRoom.SpawnedRoom == room) return;

            // Czekamy, az Rycerz stanie na ziemi i odzyska sterowanie (koniec cutscenki, upadku itp.).
            if (hero.cState.onGround && hero.acceptingInput && !hero.cState.transitioning)
            {
                groundTime += Time.deltaTime;
            }
            else
            {
                groundTime = 0f;
            }
            if (groundTime < 0.75f) return;

            ChestRoom.Spawn(room, Data.KeyForRoom != room);
            ShowMessage(room == 0
                ? "Cztery skrzynie i klucz. Klucz otwiera jedną - wybierz broń. Po każdym bossie znów możesz wziąć broń z dowolnej klasy."
                : "Nowe skrzynie po pokonaniu: " + Weapons.RoomNames[room] + ". Wybierz jedną broń z dowolnej klasy.");
        }

        private void UpdateKeyAndChests(HeroController hero)
        {
            Vector2 hp = hero.transform.position;

            if (ChestRoom.Key != null && Vector2.Distance(hp, ChestRoom.Key.transform.position) < 1.1f)
            {
                Data.KeyForRoom = ChestRoom.Key.Room;
                Destroy(ChestRoom.Key.gameObject);
                ChestRoom.Key = null;
                ShowMessage("Masz Simple Key. Podejdź do skrzyni i naciśnij W GÓRĘ, żeby ją otworzyć.");
            }

            bool upPressed = InputHandler.Instance != null && InputHandler.Instance.inputActions.up.WasPressed;
            Chest opened = null;
            foreach (Chest c in ChestRoom.Chests)
            {
                if (c == null) continue;
                c.PlayerNear = Vector2.Distance(hp, c.transform.position) < 1.3f;
                if (c.PlayerNear && upPressed)
                {
                    if (Data.KeyForRoom == c.Room)
                    {
                        opened = c;
                    }
                    else
                    {
                        ShowMessage("Skrzynia jest zamknięta. Potrzebujesz klucza.");
                    }
                }
            }
            if (opened != null) OpenChest(opened);
        }

        private void OpenChest(Chest c)
        {
            Data.Weapons.Add(c.Weapon.Id);
            Data.OpenedRooms.Add(c.Room);
            Data.KeyForRoom = -1;
            TerrariaArsenal.Instance.Log("Wybrano bron: " + c.Weapon.Name + " (pokoj " + c.Room + ")");
            ChestRoom.Clear();

            string how;
            switch (c.Weapon.Class)
            {
                case WeaponClass.Melee:
                    Data.ActiveMelee = c.Weapon.Id;
                    activeKind = SlotNail;
                    how = "Atak (LPM albo X) tnie mieczem: " + MeleeDamage() + " obrażeń. " + CycleKey + " albo kółko myszy zmienia przedmiot w ręce.";
                    break;
                case WeaponClass.Summon:
                    Data.ActiveSummon = c.Weapon.Id;
                    activeKind = SlotWhip;
                    Whip wh = Weapons.WhipFor(Data.Weapons);
                    how = "Minion lata przy tobie i atakuje wrogów. Do tego bicz " + (wh != null ? wh.Name : "") +
                          ": atak (LPM albo X) smaga wroga, a minion skupia się na trafionym celu. Wybranie innej laski na pasku zmienia miniona.";
                    break;
                case WeaponClass.Magic:
                    SelectWeapon(c.Weapon.Id);
                    how = "Atak (LPM albo X) rzuca czar w stronę kursora. Czar kosztuje " + c.Weapon.ManaCost
                        + " many (gwiazdki w prawym górnym rogu). " + CycleKey + " albo kółko myszy zmienia przedmiot w ręce.";
                    break;
                default:
                    SelectWeapon(c.Weapon.Id);
                    how = "Atak (LPM albo X) strzela w stronę kursora. " + CycleKey + " albo kółko myszy zmienia przedmiot w ręce.";
                    break;
            }
            ShowMessage("Zdobyto: " + c.Weapon.Name + " (" + c.Weapon.Damage + " obrażeń). " + how);
        }

        // --- Bronie ---
        public static int MeleeDamage()
        {
            Weapon w = Weapons.ActiveMelee();
            return w == null ? 0 : w.Damage;
        }

        private List<Weapon> FiringWeapons()
        {
            List<Weapon> list = new List<Weapon>();
            foreach (string id in Data.Weapons)
            {
                Weapon w = Weapons.Get(id);
                if (w != null && (w.Class == WeaponClass.Ranged || w.Class == WeaponClass.Magic)) list.Add(w);
            }
            return list;
        }

        private void SelectWeapon(string id)
        {
            List<Weapon> list = FiringWeapons();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id == id)
                {
                    selected = i;
                    activeKind = SlotShooter;
                }
            }
        }

        // Po wczytaniu save'a / nowej grze: wybierz przedmiot do reki od nowa.
        public void ResetActive()
        {
            activeInit = false;
            mana = MaxMana;
        }

        private string NailName()
        {
            Weapon m = Weapons.ActiveMelee();
            return m != null ? m.Name : "Gwóźdź";
        }

        private Whip CurrentWhip()
        {
            return Weapons.WhipFor(Weapons.ActiveSummon());
        }

        // Pasek przedmiotow jak w Terrarii: kazdy miecz, kazdy bicz (z laska i jej minionem), kazda bron strzelajaca.
        private List<int> Slots()
        {
            List<int> slots = new List<int>();
            int nm = Weapons.Owned(WeaponClass.Melee).Count;
            if (nm == 0) slots.Add(0);
            for (int i = 0; i < nm; i++) slots.Add(MeleeBase + i);
            List<Weapon> staffs = Weapons.Owned(WeaponClass.Summon);
            for (int i = 0; i < staffs.Count; i++)
            {
                if (Weapons.WhipFor(staffs[i]) != null) slots.Add(WhipBase + i);
            }
            int n = FiringWeapons().Count;
            for (int i = 0; i < n; i++) slots.Add(ShooterBase + i);
            return slots;
        }

        private static bool IsNailSlot(int slot)
        {
            return slot < WhipBase;
        }

        private static bool IsWhipSlot(int slot)
        {
            return slot >= WhipBase && slot < ShooterBase;
        }

        // Co jest w rece, gdy to nie gwozdz/miecz: ikona i bron (bicz: bron = null). null = gwozdz.
        public string InHandIcon(out Weapon weapon)
        {
            weapon = null;
            int slot = CurrentSlot();
            if (IsWhipSlot(slot))
            {
                Whip wh = CurrentWhip();
                return wh != null ? wh.Icon : null;
            }
            if (slot >= ShooterBase)
            {
                weapon = CurrentWeapon();
                return weapon != null ? weapon.Id : null;
            }
            return null;
        }

        private int CurrentSlot()
        {
            if (activeKind == SlotWhip && CurrentWhip() != null)
            {
                int si = Weapons.Owned(WeaponClass.Summon).IndexOf(Weapons.ActiveSummon());
                return WhipBase + Mathf.Max(0, si);
            }
            if (activeKind == SlotShooter && CurrentWeapon() != null) return ShooterBase + selected;
            Weapon m = Weapons.ActiveMelee();
            if (m == null) return 0;
            return MeleeBase + Mathf.Max(0, Weapons.Owned(WeaponClass.Melee).IndexOf(m));
        }

        private void SetSlot(int slot)
        {
            if (slot >= ShooterBase)
            {
                activeKind = SlotShooter;
                selected = slot - ShooterBase;
            }
            else if (slot >= WhipBase)
            {
                List<Weapon> staffs = Weapons.Owned(WeaponClass.Summon);
                int i = slot - WhipBase;
                if (i < staffs.Count && Data.ActiveSummon != staffs[i].Id)
                {
                    Data.ActiveSummon = staffs[i].Id;
                    Sounds.Play("Item_44", 0.6f);   // przywolanie miniona
                }
                activeKind = SlotWhip;
            }
            else
            {
                List<Weapon> swords = Weapons.Owned(WeaponClass.Melee);
                int i = slot - MeleeBase;
                if (slot >= MeleeBase && i < swords.Count) Data.ActiveMelee = swords[i].Id;
                activeKind = SlotNail;
            }
        }

        private string SlotName(int slot)
        {
            if (IsWhipSlot(slot))
            {
                List<Weapon> staffs = Weapons.Owned(WeaponClass.Summon);
                int i = slot - WhipBase;
                Weapon staff = i < staffs.Count ? staffs[i] : null;
                Whip wh = Weapons.WhipFor(staff);
                return wh != null ? wh.Name + " + " + staff.Name : "";
            }
            if (slot >= ShooterBase)
            {
                List<Weapon> list = FiringWeapons();
                return slot - ShooterBase < list.Count ? list[slot - ShooterBase].Name : "";
            }
            return NailName();
        }

        private void Cycle(int dir)
        {
            List<int> slots = Slots();
            if (slots.Count <= 1)
            {
                activeKind = SlotNail;
                return;
            }
            int idx = slots.IndexOf(CurrentSlot());
            if (idx < 0) idx = 0;
            idx = ((idx + dir) % slots.Count + slots.Count) % slots.Count;
            SetSlot(slots[idx]);
            ShowMessage("W ręce: " + SlotName(slots[idx]));
        }

        // Dol + atak w powietrzu zawsze tnie gwozdziem (odbijanie od kolcow i wrogow).
        private static bool PogoInput(HeroController hero)
        {
            return hero != null && !hero.cState.onGround && InputHandler.Instance != null &&
                   InputHandler.Instance.inputActions.down.IsPressed;
        }

        // Czy zablokowac cios gwozdziem (bo w rece jest bicz, luk albo magia)? Hook w TerrariaArsenal.
        public static bool BlockNail()
        {
            Runner r = Instance;
            HeroController hero = HeroController.instance;
            if (r == null || hero == null) return false;
            if (IsNailSlot(r.CurrentSlot())) return false;
            return !PogoInput(hero);
        }

        // LPM = atak gwozdziem HK: wolamy prywatne CanAttack/DoAttack gry (te same co przycisk ataku).
        private static System.Reflection.MethodInfo canAttackMethod, doAttackMethod;

        private static void NailAttack(HeroController hero)
        {
            if (canAttackMethod == null)
            {
                System.Reflection.BindingFlags f = System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
                canAttackMethod = typeof(HeroController).GetMethod("CanAttack", f);
                doAttackMethod = typeof(HeroController).GetMethod("DoAttack", f);
                if (canAttackMethod == null || doAttackMethod == null) return;
            }
            if ((bool)canAttackMethod.Invoke(hero, null)) doAttackMethod.Invoke(hero, null);
        }

        // Kursor celuje, jesli myszka byla uzywana w ostatnich 30 s (gra tylko na klawiaturze = celowanie przed siebie).
        private bool MouseAiming
        {
            get { return mouseActiveTime > 0f; }
        }

        private static Vector2 MouseAim(HeroController hero)
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector2.zero;
            Vector3 sp = Input.mousePosition;
            sp.z = hero.transform.position.z - cam.transform.position.z;
            Vector3 wp = cam.ScreenToWorldPoint(sp);
            Vector2 d = wp - (hero.transform.position + new Vector3(0f, 0.2f, 0f));
            return d.sqrMagnitude < 0.0001f ? Vector2.zero : d.normalized;
        }

        // Kierunek haka: kursor (gdy uzywasz myszki), inaczej skos w gore do przodu jak w Terrarii na padzie.
        public Vector2 HookAim(HeroController hero)
        {
            if (MouseAiming)
            {
                Vector2 m = MouseAim(hero);
                if (m.sqrMagnitude > 0.001f) return m;
            }
            bool up = InputHandler.Instance != null && InputHandler.Instance.inputActions.up.IsPressed;
            float x = hero.cState.facingRight ? 1f : -1f;
            return up ? new Vector2(0f, 1f) : new Vector2(x * 0.7f, 0.7f);
        }

        private static void FaceTowards(HeroController hero, Vector2 aim)
        {
            if (aim.x < -0.15f && hero.cState.facingRight) hero.FaceLeft();
            else if (aim.x > 0.15f && !hero.cState.facingRight) hero.FaceRight();
        }

        // Kierunek bez myszki: przod, z W w gore.
        private static Vector2 KeyboardAim(HeroController hero)
        {
            bool right = hero.cState.facingRight;
            bool up = InputHandler.Instance != null && InputHandler.Instance.inputActions.up.IsPressed;
            return up ? new Vector2(right ? 0.5f : -0.5f, 0.87f) : new Vector2(right ? 1f : -1f, 0.04f);
        }

        private Weapon CurrentWeapon()
        {
            List<Weapon> list = FiringWeapons();
            if (list.Count == 0) return null;
            if (selected >= list.Count || selected < 0) selected = list.Count - 1;
            return list[selected];
        }

        private bool HasMagic()
        {
            return Weapons.Best(Data.Weapons, WeaponClass.Magic) != null;
        }

        private void UpdateMana(HeroController hero)
        {
            if (manaRegenDelay > 0f)
            {
                manaRegenDelay -= Time.deltaTime;
                return;
            }
            if (mana >= MaxMana) return;
            Rigidbody2D rb = hero.GetComponent<Rigidbody2D>();
            bool still = rb != null && rb.velocity.sqrMagnitude < 0.01f;
            mana = Mathf.Min(MaxMana, mana + ManaRegenRate * (still ? 2f : 1f) * Time.deltaTime);
        }

        private void UpdateWeapons(HeroController hero)
        {
            // Pad: prawa galka = strzal w kierunku wychylenia, wcisniecie galki = zmiana broni.
            InControl.InputDevice pad = InControl.InputManager.ActiveDevice;
            Vector2 stick = pad != null ? pad.RightStick.Vector : Vector2.zero;
            bool padCycle = pad != null && pad.RightStickButton.WasPressed;
            whipCooldown -= Time.deltaTime;

            if (!activeInit)
            {
                if (FiringWeapons().Count > 0) activeKind = SlotShooter;
                else if (CurrentWhip() != null) activeKind = SlotWhip;
                else activeKind = SlotNail;
                activeInit = true;
            }

            Vector3 mp = Input.mousePosition;
            bool mouseDown = Input.GetMouseButtonDown(0);
            bool mouseHeld = Input.GetMouseButton(0);
            if ((mp - lastMouse).sqrMagnitude > 4f || mouseHeld) mouseActiveTime = 30f;
            else mouseActiveTime -= Time.deltaTime;
            lastMouse = mp;

            float wheel = Input.mouseScrollDelta.y;
            if (Input.GetKeyDown(CycleKey) || padCycle) Cycle(1);
            else if (wheel > 0.1f) Cycle(-1);
            else if (wheel < -0.1f) Cycle(1);

            if (!hero.acceptingInput) return;
            int slot = CurrentSlot();
            bool pogo = PogoInput(hero);
            bool attackHeld = mouseHeld || (InputHandler.Instance != null && InputHandler.Instance.inputActions.attack.IsPressed);

            // Kierunek ataku: prawa galka > kursor > klawiatura.
            bool padFire = stick.magnitude > 0.5f;
            Vector2 aim = padFire ? stick.normalized : (MouseAiming ? MouseAim(hero) : KeyboardAim(hero));

            // Gwozdz/miecz pod LPM (klawiatura wola atak HK sama).
            if (mouseDown && (IsNailSlot(slot) || pogo))
            {
                if (!pogo && MouseAiming) FaceTowards(hero, aim);
                NailAttack(hero);
            }

            // Bicz.
            if (IsWhipSlot(slot) && attackHeld && !pogo && whipCooldown <= 0f)
            {
                Whip wh = CurrentWhip();
                if (wh != null)
                {
                    FaceTowards(hero, aim);
                    WhipSwing.Spawn(wh, aim);
                    if (TerrariaPlayer.Instance != null) TerrariaPlayer.Instance.OnWhip(aim);
                    whipCooldown = Mathf.Max(0.42f, wh.Cooldown);
                }
            }

            // Luk / magia: atak (gdy w rece), F zawsze, albo prawa galka.
            bool viaAttack = slot >= ShooterBase && attackHeld && !pogo;
            if (!(Input.GetKey(FireKey) || padFire || viaAttack) || fireCooldown > 0f) return;
            Weapon w = CurrentWeapon();
            if (w == null) return;
            FaceTowards(hero, aim);

            if (w.ManaCost > 0)
            {
                if (mana < w.ManaCost)
                {
                    if (messageTime < 3f) ShowMessage("Za mało many (" + w.ManaCost + " na czar). Mana odnawia się sama.");
                    fireCooldown = 0.25f;
                    return;
                }
                mana -= w.ManaCost;
                manaRegenDelay = ManaRegenDelay;
            }
            Fire(hero, w, aim);
            fireCooldown = w.Cooldown;
        }

        private void Fire(HeroController hero, Weapon w, Vector2 aim)
        {
            bool right = hero.cState.facingRight;
            Vector2 dir = aim;
            if (dir == Vector2.zero)
            {
                bool up = InputHandler.Instance != null && InputHandler.Instance.inputActions.up.IsPressed;
                dir = up ? new Vector2(right ? 0.5f : -0.5f, 0.87f) : new Vector2(right ? 1f : -1f, 0.04f);
            }
            Vector3 origin = hero.transform.position + new Vector3(dir.x * 0.6f, 0.2f + dir.y * 0.4f, -0.01f);
            // Z postacia z Terrarii pocisk wylatuje z broni w rece (srodek luku, lufa, czubek rozdzki).
            Vector3 muzzle;
            if (TerrariaPlayer.Instance != null && TerrariaPlayer.Instance.TryMuzzle(hero, w, dir, out muzzle)) origin = muzzle;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Sprite sprite = Assets.Get(w.Sprite, w.Ppu);
            if (TerrariaPlayer.Instance != null) TerrariaPlayer.Instance.OnFire(w, dir);
            if (w.Class == WeaponClass.Magic) Sounds.Play("Item_8", w.Cooldown < 0.3f ? 0.35f : 0.6f);
            else if (w.Sprite == "P_Bullet") Sounds.Play("Item_11", 0.6f);
            else Sounds.Play("Item_5", 0.7f);

            // Daedalus Stormbow / Meteor Staff: pociski spadaja z nieba na punkt celowania.
            if (w.FromSky)
            {
                Vector2 target = MouseAiming ? MouseWorld(hero) : (Vector2)hero.transform.position + dir * 7f;
                for (int i = 0; i < w.Count; i++)
                {
                    Vector3 from = new Vector3(target.x + Random.Range(-3f, 3f), hero.transform.position.y + 11f, origin.z);
                    Vector2 v = (target + new Vector2(Random.Range(-0.6f, 0.6f), 0f) - (Vector2)from).normalized * w.Speed * 1.2f;
                    SpawnShot(w, sprite, from, v, true);
                }
                return;
            }

            for (int i = 0; i < w.Count; i++)
            {
                float angle = baseAngle;
                if (w.Count > 1) angle += Mathf.Lerp(-w.Spread, w.Spread, i / (float)(w.Count - 1));
                else if (w.Spread > 0f) angle += Random.Range(-w.Spread, w.Spread);
                float rad = angle * Mathf.Deg2Rad;

                SpawnShot(w, sprite, origin, new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * w.Speed * Random.Range(0.95f, 1.05f), false);
            }
        }

        private static void SpawnShot(Weapon w, Sprite sprite, Vector3 origin, Vector2 velocity, bool fromSky)
        {
            GameObject go = Combat.MakeSprite("TA_Shot", sprite, origin, w.Tint);
            Projectile p = go.AddComponent<Projectile>();
            p.Velocity = velocity;
            p.Gravity = fromSky ? 0f : w.Gravity;
            p.Life = fromSky ? 1.5f : w.Life;
            p.Pierce = w.Pierce;
            p.Spin = w.Spin;
            p.SpriteAngleOffset = w.AngleOffset;
            p.Damage = w.Damage;
            p.Burn = w.Burn;
            p.Bounce = w.Bounce;
            p.Homing = w.Homing;
            p.BeesOnHit = w.BeesOnHit;
            // Z nieba pociski przelatuja przez sufit, zeby dalo sie ich uzywac w jaskiniach.
            p.IgnoreTerrain = fromSky;
        }

        private static Vector2 MouseWorld(HeroController hero)
        {
            Camera cam = Camera.main;
            if (cam == null) return hero.transform.position;
            Vector3 sp = Input.mousePosition;
            sp.z = hero.transform.position.z - cam.transform.position.z;
            return cam.ScreenToWorldPoint(sp);
        }

        private string minionSprite = "";

        private void UpdateMinion()
        {
            Weapon best = Weapons.ActiveSummon();
            if (best == null || (minion != null && minionSprite != best.Sprite))
            {
                if (minion != null) Destroy(minion);
                minion = null;
                if (best == null) return;
            }
            if (minion == null)
            {
                Vector3 pos = HeroController.instance.transform.position + new Vector3(0f, 1.4f, -0.01f);
                Sprite[] frames = Assets.GetFrames(best.Sprite, 30f, best.Frames);
                minion = Combat.MakeSprite("TA_Minion", frames != null ? frames[0] : null, pos, Color.white);
                DontDestroyOnLoad(minion);
                minion.AddComponent<Minion>().Frames = frames;
                minionSprite = best.Sprite;
            }
            Minion m = minion.GetComponent<Minion>();
            m.Damage = best.Damage;
            m.Cooldown = best.Cooldown;
        }

        // Kursor z Terrarii (gdy uzywasz myszki).
        private void DrawCursor(float ui)
        {
            if (mouseActiveTime <= 0f) return;
            Sprite cur = Assets.Get("Cursor", 32f);
            if (cur == null) return;
            Vector3 mp = Input.mousePosition;
            float size = 18f * 1.6f * ui;
            GUI.DrawTexture(new Rect(mp.x, Screen.height - mp.y, size, size), cur.texture);
        }

        // Pasek many: kolumna 10 gwiazdek z Terrarii w prawym gornym rogu, kazda = 10 many.
        private void DrawMana(float ui)
        {
            Sprite star = Assets.Get("Mana", 32f);
            if (star == null) return;
            Texture2D tex = star.texture;
            float size = 22f * 1.6f * ui;
            float step = 24f * 1.6f * ui;
            float x = Screen.width - 40f * ui - size;
            float y = 40f * ui;
            float hs = 0.8f * ui;
            TerrariaFont.DrawCentered(x + size * 0.5f, y, "Mana", Color.white, hs);
            y += TerrariaFont.LineHeight(hs);

            int stars = Mathf.RoundToInt(MaxMana / 10f);
            Color old = GUI.color;
            for (int i = 0; i < stars; i++)
            {
                float fill = Mathf.Clamp01((mana - i * 10f) / 10f);
                // Pusta gwiazdka: ciemna i mniejsza; czesciowo pelna: rosnie i jasnieje (jak w Terrarii).
                float sc = Mathf.Lerp(0.6f, 1f, fill);
                float a = fill <= 0f ? 0.25f : Mathf.Lerp(0.5f, 1f, fill);
                float s = size * sc;
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(new Rect(x + (size - s) * 0.5f, y + i * step + (size - s) * 0.5f, s, s * 24f / 22f), tex);
            }
            GUI.color = old;
        }

        // --- Napisy (czcionka z Terrarii) ---
        private void OnGUI()
        {
            if (TerrariaArsenal.Instance == null || HeroController.instance == null) return;
            if (GameManager.instance == null || !GameManager.instance.IsGameplayScene()) return;

            float ui = Screen.height / 1080f;
            Color gold = new Color(1f, 0.85f, 0.4f);
            Color grey = new Color(0.8f, 0.8f, 0.8f);

            Camera cam = Camera.main;
            if (cam != null)
            {
                foreach (Chest c in ChestRoom.Chests)
                {
                    if (c == null) continue;
                    Vector3 sp = cam.WorldToScreenPoint(c.transform.position + new Vector3(0f, 2.2f, 0f));
                    if (sp.z < 0f) continue;
                    int cls = (int)c.Weapon.Class;
                    float x = sp.x;
                    float y = Screen.height - sp.y - TerrariaFont.LineHeight(ui * 1.1f);
                    List<string> lines = new List<string>();
                    List<Color> colors = new List<Color>();
                    List<float> sizes = new List<float>();
                    lines.Add(Weapons.ClassNames[cls]); colors.Add(Weapons.ClassColors[cls]); sizes.Add(1.1f);
                    if (c.PlayerNear)
                    {
                        string label = c.Weapon.Name;
                        if (c.Weapon.Class == WeaponClass.Summon)
                        {
                            foreach (Whip wh in Weapons.Whips)
                            {
                                if (wh.Room == c.Room) label += " + " + wh.Name + " (bicz " + wh.Damage + ")";
                            }
                        }
                        lines.Add(label); colors.Add(Color.white); sizes.Add(1f);
                        lines.Add("Obrażenia: " + c.Weapon.Damage + " (Terraria: " + c.Weapon.TerrariaDamage + ")");
                        colors.Add(grey); sizes.Add(0.85f);
                        bool hasKey = Data.KeyForRoom == c.Room;
                        lines.Add(hasKey ? "[W GÓRĘ] otwórz" : "Zamknięta - potrzebny klucz");
                        colors.Add(hasKey ? gold : grey); sizes.Add(0.85f);
                        // Przy blizszej skrzyni napis rosnie w gore, zeby nie zaslaniac ikony.
                        y -= TerrariaFont.LineHeight(ui) * 2.6f;
                    }
                    for (int i = 0; i < lines.Count; i++)
                    {
                        float sc = sizes[i] * ui;
                        TerrariaFont.DrawCentered(x, y, lines[i], colors[i], sc);
                        y += TerrariaFont.LineHeight(sc) * 0.9f;
                    }
                }
            }

            // HUD w lewym dolnym rogu.
            List<string> hud = new List<string>();
            int melee = MeleeDamage();
            Weapon cur = CurrentWeapon();
            string inHand;
            int slotNow = CurrentSlot();
            Whip curWhip = CurrentWhip();
            if (IsWhipSlot(slotNow) && curWhip != null) inHand = curWhip.Name + " (" + curWhip.Damage + ", bicz)";
            else if (slotNow >= ShooterBase && cur != null) inHand = cur.Name + " (" + cur.Damage + (cur.ManaCost > 0 ? ", " + cur.ManaCost + " many" : "") + ")";
            else inHand = NailName() + (melee > 0 ? " (" + melee + ")" : "");
            hud.Add("W ręce [LPM/X]: " + inHand);
            if (cur != null) hud.Add("Strzał [" + FireKey + "]: " + cur.Name + "   [" + CycleKey + "/kółko] zmiana");
            Weapon summon = Weapons.ActiveSummon();
            if (summon != null) hud.Add("Minion: " + summon.Name + " (" + summon.Damage + ")");
            float hs = 0.9f * ui;
            float hy = Screen.height - 24f * ui - TerrariaFont.LineHeight(hs) * (hud.Count + 1);
            Accessories.DrawHud(24f * ui, hy - 40f * ui, ui);
            TerrariaFont.Draw(24f * ui, hy, "Terraria", gold, hs);
            for (int i = 0; i < hud.Count; i++)
            {
                TerrariaFont.Draw(24f * ui, hy + TerrariaFont.LineHeight(hs) * (i + 1), hud[i], Color.white, hs);
            }

            DamageNumbers.Draw(ui);
            BossBar.Draw(ui);
            DrawCursor(ui);
            if (HasMagic()) DrawMana(ui);

            // Komunikaty na srodku ekranu (zanikaja pod koniec).
            if (messageTime > 0f && message.Length > 0)
            {
                float ms = 1.15f * ui;
                Color mc = new Color(1f, 1f, 1f, Mathf.Clamp01(messageTime));
                List<string> wrapped = TerrariaFont.Wrap(message, ms, Screen.width * 0.6f);
                float my = Screen.height * 0.18f;
                foreach (string l in wrapped)
                {
                    TerrariaFont.DrawCentered(Screen.width * 0.5f, my, l, mc, ms);
                    my += TerrariaFont.LineHeight(ms);
                }
            }
        }
    }
}
