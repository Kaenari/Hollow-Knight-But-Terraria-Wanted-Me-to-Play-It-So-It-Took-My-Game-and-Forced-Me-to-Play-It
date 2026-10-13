// Postac z Terrarii zamiast Rycerza: grafika i animacje z Terrarii, pod spodem kontroler HK.
// Do tego ruch "jak w Terrarii" (rozpedzanie sie) i wbudowane Hermes Boots (sprint po rozbiegu).
// F9 przelacza wyglad Terraria <-> Rycerz.

using System.Reflection;
using UnityEngine;

namespace TerrariaArsenal
{
    public class TerrariaPlayer : MonoBehaviour
    {
        public static TerrariaPlayer Instance;
        public static bool Enabled = true;
        public KeyCode ToggleKey = KeyCode.F9;

        // Klatki w res/Player.png (pasek pionowy 40x56).
        private const int FrameCount = 26;
        private const int Idle = 0, Jump = 1, Walk = 2, WalkFrames = 14, Use = 16, HoldForward = 20, HoldUp = 21, AirUse = 22;
        private const float Ppu = 30f;
        private const float FeetOffset = 26f / Ppu;    // od srodka klatki do podeszew
        private const float AttackTime = 0.28f;

        private GameObject root;          // odbijany w poziomie przy patrzeniu w lewo
        private SpriteRenderer body;
        private SpriteRenderer held;
        private Sprite[] frames;

        private float walkPhase;
        private float attackT = 99f;
        private int attackDir;            // 0 zwykly, 1 w gore, 2 w dol
        private bool whipping;
        private const float WhipTime = 0.42f;   // tyle co zamach w WhipSwing
        private int lastFrame;

        // Polozenie dloni w klatkach Terrarii (wzgledem srodka klatki, postac patrzy w prawo),
        // zmierzone z arkusza dloni Player_0_5.
        private static readonly Vector2 HandIdle = new Vector2(-0.233f, -0.4f);
        private static readonly Vector2[] HandUse =
        {
            new Vector2(-0.3f, 0.267f),   // reka w gorze za glowa
            new Vector2(0.167f, 0.233f),  // skos w gore do przodu
            new Vector2(0.267f, -0.233f), // do przodu
            new Vector2(0.167f, -0.4f)    // skos w dol do przodu
        };
        private float holdT = 99f;
        private Weapon holdWeapon;
        private Vector2 holdAim;
        private bool hidKnight;
        private bool wasDoubleJumping;

        // Ruch
        private bool speedsSaved;
        private float origRun, origRunCh, origRunCombo;
        private float runTime;
        private float lastDir;
        private float dustTimer;
        public bool Sprinting { get; private set; }

        private static FieldInfo wingsField;

        private void Awake()
        {
            Instance = this;
            wingsField = typeof(HeroController).GetField("dJumpWingsPrefab",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        // --- Zdarzenia z innych czesci moda ---
        public void OnAttack(int dir)
        {
            attackT = 0f;
            attackDir = dir;
            whipping = false;
        }

        // Odbicie w dol bronia z reki (pogo): postac wali w dol lukiem, rozdzka, biczem...
        private const float BonkTime = 0.3f;
        private float bonkT = 99f;
        private string bonkIcon;
        private Weapon bonkWeapon;
        private bool bonkSpark;

        public void OnBonk(string icon, Weapon w)
        {
            bonkT = 0f;
            bonkIcon = icon;
            bonkWeapon = w;
            bonkSpark = false;
            attackT = 99f;
            whipping = false;
        }

        // Zamach biczem: ta sama animacja reki, ale bez miecza w dloni (bicz rysuje WhipSwing).
        public void OnWhip(Vector2 aim)
        {
            attackT = 0f;
            attackDir = aim.y > 0.6f ? 1 : (aim.y < -0.6f ? 2 : 0);
            whipping = true;
        }

        // Dlon postaci w swiecie (bicz zaczepiony jest tutaj). null-bezpieczne: bez postaci = przy Rycerzu.
        public Vector2 HandWorld(HeroController hero)
        {
            if (root == null || !root.activeSelf || !Enabled)
                return (Vector2)hero.transform.position + new Vector2(hero.cState.facingRight ? 0.2f : -0.2f, -0.1f);
            Vector2 local;
            if (lastFrame >= Use && lastFrame < Use + 4) local = HandUse[lastFrame - Use];
            else if (lastFrame >= AirUse && lastFrame < AirUse + 4) local = HandUse[lastFrame - AirUse];
            else if (lastFrame == HoldForward) local = HandUse[2];
            else if (lastFrame == HoldUp) local = HandUse[1];
            else local = HandIdle;
            local.x *= root.transform.localScale.x;
            return (Vector2)root.transform.position + local;
        }


        public void OnFire(Weapon w, Vector2 aim)
        {
            holdT = 0f;
            holdWeapon = w;
            holdAim = aim;
        }

        // Arkusz postaci zalezy od wygladu ustawionego dla aktualnego slotu zapisu.
        private int frameSlot = -1;
        private int frameVersion = -1;

        private void RefreshFrames()
        {
            int slot = GameManager.instance != null ? GameManager.instance.profileID : 0;
            if (frames != null && slot == frameSlot && PlayerLook.Version == frameVersion) return;
            frameSlot = slot;
            frameVersion = PlayerLook.Version;
            frames = PlayerLook.Frames(slot, Ppu);
        }

        private void Build()
        {
            RefreshFrames();
            root = new GameObject("TA_Terrarian");
            DontDestroyOnLoad(root);
            GameObject b = new GameObject("Body");
            b.transform.SetParent(root.transform, false);
            body = b.AddComponent<SpriteRenderer>();
            body.sprite = frames != null ? frames[0] : null;
            GameObject h = new GameObject("Held");
            h.transform.SetParent(root.transform, false);
            held = h.AddComponent<SpriteRenderer>();
            held.enabled = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                Enabled = !Enabled;
                if (Runner.Instance != null)
                    Runner.Instance.ShowMessage(Enabled ? "Wygląd: postać z Terrarii" : "Wygląd: Rycerz");
            }
            attackT += Time.deltaTime;
            bonkT += Time.deltaTime;
            holdT += Time.deltaTime;

            HeroController hero = HeroController.instance;
            GameManager gm = GameManager.instance;
            if (hero == null || gm == null || !gm.IsGameplayScene())
            {
                Sprinting = false;
                return;
            }
            UpdateMovement(hero);
        }

        // --- Ruch: rozpedzanie sie jak w Terrarii + Hermes Boots ---
        private void UpdateMovement(HeroController hero)
        {
            if (!speedsSaved)
            {
                origRun = hero.RUN_SPEED;
                origRunCh = hero.RUN_SPEED_CH;
                origRunCombo = hero.RUN_SPEED_CH_COMBO;
                speedsSaved = true;
            }

            Rigidbody2D rb = hero.GetComponent<Rigidbody2D>();
            float vx = rb != null ? rb.velocity.x : 0f;
            HeroControllerStates cs = hero.cState;
            float dir = Mathf.Abs(vx) > 0.1f ? Mathf.Sign(vx) : 0f;

            if (cs.onGround)
            {
                if (dir == 0f || dir != lastDir || cs.dashing) runTime = 0f;
                if (dir != 0f) runTime += Time.deltaTime;
            }
            else if (dir != 0f && dir != lastDir)
            {
                runTime = 0f; // zawrocenie w powietrzu gubi rozped
            }
            if (dir != 0f) lastDir = dir;
            if (cs.wallSliding || cs.swimming || hero.cState.transitioning) runTime = 0f;

            // 0-0.2 s: rozbieg od 65% predkosci, potem pelna. Hermes Boots: po 0.8 s sprint do 140%.
            float mult = Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(runTime / 0.2f));
            float sprint = Mathf.Clamp01((runTime - 0.8f) / 0.4f);
            mult *= 1f + 0.4f * sprint;
            Sprinting = sprint > 0.5f && dir != 0f;

            hero.RUN_SPEED = origRun * mult;
            hero.RUN_SPEED_CH = origRunCh * mult;
            hero.RUN_SPEED_CH_COMBO = origRunCombo * mult;

            if (Sprinting && cs.onGround)
            {
                dustTimer -= Time.deltaTime;
                if (dustTimer <= 0f)
                {
                    dustTimer = 0.04f;
                    Vector3 feet = Feet(hero);
                    Dust.Spawn(feet + new Vector3(-dir * 0.3f, 0.05f, 0f),
                        new Vector2(-dir * Random.Range(0.5f, 1.5f), Random.Range(0.3f, 1f)),
                        new Color(0.85f, 0.8f, 0.7f, 0.8f), 0.5f, 0.22f);
                }
            }
        }

        private static Vector3 Feet(HeroController hero)
        {
            Collider2D col = hero.GetComponent<Collider2D>();
            Vector3 p = hero.transform.position;
            if (col != null) return new Vector3(col.bounds.center.x, col.bounds.min.y, p.z);
            return p + new Vector3(0f, -0.65f, 0f);
        }

        // --- Grafika ---
        private void LateUpdate()
        {
            HeroController hero = HeroController.instance;
            GameManager gm = GameManager.instance;
            bool active = Enabled && hero != null && gm != null && gm.IsGameplayScene();

            tk2dSprite knight = hero != null ? hero.GetComponent<tk2dSprite>() : null;
            if (!active)
            {
                if (root != null) root.SetActive(false);
                if (hidKnight && knight != null)
                {
                    Color c = knight.color;
                    c.a = 1f;
                    knight.color = c;
                }
                hidKnight = false;
                if (hero != null) MuteKnightSounds(hero, false);
                return;
            }

            if (root == null) Build();
            RefreshFrames();
            if (frames == null) return;

            // Rycerz staje sie niewidoczny (alfa 0), ale caly jego kod dziala dalej.
            if (knight != null && knight.color.a > 0f)
            {
                Color c = knight.color;
                c.a = 0f;
                knight.color = c;
            }
            hidKnight = true;
            MuteKnightSounds(hero, true);

            MeshRenderer mr = hero.GetComponent<MeshRenderer>();
            HeroControllerStates cs = hero.cState;
            bool visible = mr != null && mr.enabled && hero.gameObject.activeInHierarchy && !cs.dead;
            root.SetActive(visible);
            if (!visible) return;

            HideWings(hero);
            if (cs.doubleJumping && !wasDoubleJumping) CloudPuff(hero);
            wasDoubleJumping = cs.doubleJumping;

            Vector3 feet = Feet(hero);
            root.transform.position = new Vector3(feet.x, feet.y + FeetOffset, hero.transform.position.z - 0.002f);
            root.transform.localScale = new Vector3(cs.facingRight ? 1f : -1f, 1f, 1f);
            body.sortingLayerID = mr.sortingLayerID;
            body.sortingOrder = mr.sortingOrder;
            held.sortingLayerID = mr.sortingLayerID;
            held.sortingOrder = mr.sortingOrder + 1;

            // Miganie po otrzymaniu obrazen.
            float alpha = 1f;
            if (cs.invulnerable && !cs.dashing && !cs.shadowDashing)
                alpha = Mathf.Repeat(Time.time * 10f, 1f) < 0.5f ? 0.35f : 1f;
            body.color = new Color(1f, 1f, 1f, alpha);
            held.color = new Color(1f, 1f, 1f, alpha);

            lastFrame = PickFrame(hero, cs);
            body.sprite = frames[lastFrame];
            UpdateHeld(hero, cs);
            AbilityEffects(hero, cs);
        }

        // --- Umiejetnosci w stylu Terrarii ---
        // Dash (Tabi): biale powidoki i kurz. Shade dash (Master Ninja Gear): czarne powidoki i dym.
        // Super dash (Shield of Cthulhu): tarcza przed postacia, czerwone iskry; przy ladowaniu tarcza pulsuje.
        private float afterTimer;
        private SpriteRenderer shield;

        private void AbilityEffects(HeroController hero, HeroControllerStates cs)
        {
            bool shadow = cs.shadowDashing;
            bool dash = cs.dashing || cs.backDashing;
            bool super = cs.superDashing;
            string sdState = hero.superDash != null ? hero.superDash.ActiveStateName : "";
            bool charging = !super && sdState != null && sdState.Contains("Charg");

            if (shield == null)
            {
                GameObject g = new GameObject("Shield");
                g.transform.SetParent(root.transform, false);
                shield = g.AddComponent<SpriteRenderer>();
                shield.sprite = Assets.Get("A_ShieldOfCthulhu", 34f);
            }
            shield.enabled = super || charging;
            if (shield.enabled)
            {
                shield.sortingLayerID = body.sortingLayerID;
                shield.sortingOrder = body.sortingOrder + 2;
                float pulse = charging ? 0.6f + 0.4f * Mathf.Sin(Time.time * 18f) : 1f;
                shield.color = new Color(1f, 1f, 1f, pulse);
                shield.transform.localPosition = new Vector3(0.38f, -0.05f, -0.002f);
            }

            if (!(dash || shadow || super)) return;
            afterTimer -= Time.deltaTime;
            if (afterTimer > 0f) return;
            afterTimer = 0.035f;

            Color tint = shadow ? new Color(0.05f, 0.05f, 0.08f, 0.7f) : (super ? new Color(1f, 0.35f, 0.3f, 0.55f) : new Color(1f, 1f, 1f, 0.45f));
            Afterimage.Spawn(body, root.transform, tint);

            Vector3 feet = Feet(hero);
            float back = cs.facingRight ? -1f : 1f;
            if (shadow)
            {
                Dust.Spawn(hero.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.5f, 0.5f), 0f),
                    new Vector2(back * Random.Range(1f, 2f), Random.Range(-0.3f, 0.3f)), new Color(0.05f, 0.05f, 0.05f, 0.85f), 0.45f, 0.4f);
            }
            else if (super)
            {
                Dust.Spawn(hero.transform.position + new Vector3(back * 0.3f, Random.Range(-0.4f, 0.4f), 0f),
                    new Vector2(back * Random.Range(2f, 4f), Random.Range(-0.5f, 0.5f)), new Color(1f, 0.25f, 0.2f, 1f), 0.3f, 0.12f);
            }
            else
            {
                Dust.Spawn(feet + new Vector3(0f, 0.05f, 0f), new Vector2(back * Random.Range(0.5f, 1.5f), Random.Range(0.2f, 0.8f)),
                    new Color(0.85f, 0.85f, 0.8f, 0.8f), 0.35f, 0.22f);
            }
        }

        // Smierc jak w Terrarii: krew i nagrobek, ktory spada na ziemie.
        public void OnDeath()
        {
            HeroController hero = HeroController.instance;
            if (hero == null || !Enabled) return;
            Vector3 p = hero.transform.position;
            for (int i = 0; i < 18; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Dust.Spawn(p, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1.5f, 5f),
                    new Color(0.75f, 0.05f, 0.05f, 1f), 0.6f, 0.14f);
            }
            GameObject t = Combat.MakeSprite("TA_Tombstone", Assets.Get("P_Tombstone", 28f), p + new Vector3(0f, 0.3f, -0.01f), Color.white);
            t.AddComponent<Tombstone>();
        }

        private int PickFrame(HeroController hero, HeroControllerStates cs)
        {
            bool air = !cs.onGround;
            if (bonkT < BonkTime) return (air ? AirUse : Use) + (bonkT < BonkTime * 0.4f ? 2 : 3);
            // Bicz: reka idzie z gory zza glowy do przodu w rytmie zamachu, potem zostaje wyciagnieta.
            if (whipping && attackT < WhipTime)
            {
                float kw = attackT / WhipTime;
                int stepW = kw < 0.15f ? 0 : (kw < 0.35f ? 1 : (kw < 0.6f ? 2 : 3));
                if (attackDir == 1) stepW = Mathf.Min(stepW, 1);
                return (air ? AirUse : Use) + stepW;
            }
            if (attackT < AttackTime && !whipping)
            {
                float k = attackT / AttackTime;
                int baseUse = air ? AirUse : Use;
                int step;
                if (attackDir == 1) step = 2 - Mathf.Min(2, (int)(k * 3f));      // do gory
                else if (attackDir == 2) step = 2 + Mathf.Min(1, (int)(k * 2f)); // w dol
                else step = Mathf.Min(3, (int)(k * 4f));                          // zamach z gory na dol
                return baseUse + step;
            }
            if (holdT < 0.25f && holdWeapon != null)
            {
                Vector2 la = holdAim;
                if (!cs.facingRight) la.x = -la.x;
                return (air ? AirUse : Use) + HoldStep(Mathf.Atan2(la.y, la.x) * Mathf.Rad2Deg);
            }
            if (cs.casting) return HoldForward;
            if (air || cs.wallSliding) return Jump;
            if (cs.dashing || cs.superDashing) return Walk + 3;

            Rigidbody2D rb = hero.GetComponent<Rigidbody2D>();
            float vx = rb != null ? Mathf.Abs(rb.velocity.x) : 0f;
            if (vx > 0.5f)
            {
                walkPhase += vx * Time.deltaTime * 2.4f;
                return Walk + (int)walkPhase % WalkFrames;
            }
            walkPhase = 0f;
            return Idle;
        }

        // Bron w rece: miecz przy zamachu, bron dystansowa/magiczna przy strzale.
        private void UpdateHeld(HeroController hero, HeroControllerStates cs)
        {
            Transform t = held.transform;
            Vector3 shoulder = new Vector3(-2f / Ppu, 4f / Ppu, 0f);

            if (bonkT < BonkTime && bonkIcon != null)
            {
                float k = bonkT / BonkTime;
                float e = 1f - (1f - k) * (1f - k);
                float ang = Mathf.Lerp(15f, -95f, e);   // zamach od przodu do pionu w dol
                float offset = bonkWeapon != null && KindOf(bonkWeapon) == HoldKind.Staff ? -45f : (bonkWeapon == null ? -45f : 0f);
                Vector2 hand = HandUse[k < 0.4f ? 2 : 3];
                held.enabled = true;
                held.sprite = Assets.GetPivot(bonkIcon, HeldPpu, new Vector2(0.25f, 0.25f));
                t.localPosition = new Vector3(hand.x, hand.y, -0.001f);
                t.localRotation = Quaternion.Euler(0f, 0f, ang + offset);
                if (!bonkSpark && k > 0.55f)
                {
                    bonkSpark = true;
                    Vector3 tip = t.position + new Vector3(0f, -0.6f, 0f);
                    for (int i = 0; i < 7; i++)
                    {
                        float a = Random.Range(200f, 340f) * Mathf.Deg2Rad;
                        Dust.Spawn(tip, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2f, 4f),
                            new Color(1f, 1f, 0.85f, 1f), 0.25f, 0.12f);
                    }
                }
                return;
            }

            Weapon melee = Weapons.ActiveMelee();
            if (attackT < AttackTime && melee != null && !whipping)
            {
                float k = attackT / AttackTime;
                float from = 125f, to = -55f;
                if (attackDir == 1) { from = 10f; to = 100f; }
                else if (attackDir == 2) { from = -10f; to = -95f; }
                float ang = Mathf.Lerp(from, to, k);
                held.enabled = true;
                held.sprite = Assets.GetPivot(melee.Id, 28f, new Vector2(0.1f, 0.1f));
                Vector3 dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad), 0f);
                t.localPosition = shoulder + dir * (9f / Ppu) + new Vector3(0f, 0f, -0.001f);
                t.localRotation = Quaternion.Euler(0f, 0f, ang - 45f);
                return;
            }

            if (holdT < 0.25f && holdWeapon != null)
            {
                Vector2 aim = holdAim;
                if (!cs.facingRight) aim.x = -aim.x; // rodzic jest juz odbity
                Vector2 grip, muzzle;
                float rot;
                int step;
                Layout(holdWeapon, aim, out grip, out rot, out muzzle, out step);
                held.enabled = true;
                held.sprite = Assets.GetPivot(holdWeapon.Id, HeldPpu, PivotOf(KindOf(holdWeapon)));
                t.localPosition = new Vector3(grip.x, grip.y, -0.001f);
                t.localRotation = Quaternion.Euler(0f, 0f, rot);
                return;
            }
            held.enabled = false;
        }

        // Wycisza dzwieki Rycerza, ktore zastepujemy dzwiekami z Terrarii (zamach gwozdziem, otrzymanie ciosu).
        private HeroController mutedHero;
        private AudioSource[] mutedSources;
        private bool mutedState;

        private void MuteKnightSounds(HeroController hero, bool mute)
        {
            if (mutedHero != hero || mutedSources == null)
            {
                System.Collections.Generic.List<AudioSource> list = new System.Collections.Generic.List<AudioSource>();
                HeroAudioController hac = hero.GetComponentInChildren<HeroAudioController>(true);
                if (hac != null && hac.takeHit != null) list.Add(hac.takeHit);
                foreach (NailSlash slash in hero.GetComponentsInChildren<NailSlash>(true))
                {
                    AudioSource a = slash.GetComponent<AudioSource>();
                    if (a != null) list.Add(a);
                }
                mutedSources = list.ToArray();
                mutedHero = hero;
                mutedState = !mute;
            }
            if (mutedState == mute) return;
            foreach (AudioSource a in mutedSources)
            {
                if (a != null) a.mute = mute;
            }
            mutedState = mute;
        }

        // --- Trzymanie broni strzelajacej (jak w Terrarii) ---
        // Luk: srodek luku w dloni, strzala wylatuje ze srodka. Pistolet: rekojesc w dloni, kula z lufy.
        // Rozdzka/laska: rekojesc w dloni, grafika obrocona o -45 st. (w Terrarii sa narysowane po skosie),
        // pocisk wylatuje z czubka. Ksiega i pryzmat: trzymane przed soba.
        private const float HeldPpu = 30f;
        private enum HoldKind { Bow, Gun, Staff, Rod, Front }

        private static HoldKind KindOf(Weapon w)
        {
            if (w.Id == "Handgun" || w.Id == "SDMG") return HoldKind.Gun;
            if (w.Class == WeaponClass.Ranged) return HoldKind.Bow;
            if (w.Id == "BookOfSkulls" || w.Id == "LastPrism" || w.Id == "RazorbladeTyphoon") return HoldKind.Front; // ksiegi i pryzmat
            if (w.Id == "AquaScepter") return HoldKind.Rod; // narysowany poziomo, rekojesc z lewej
            return HoldKind.Staff;
        }

        private static Vector2 PivotOf(HoldKind k)
        {
            switch (k)
            {
                case HoldKind.Gun: return new Vector2(0.2f, 0.35f);
                case HoldKind.Staff: return new Vector2(0.12f, 0.12f);
                case HoldKind.Rod: return new Vector2(0.06f, 0.5f);
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        // Ktora klatka reki pasuje do kierunku celowania (kat w stopniach, postac patrzy w prawo).
        private static int HoldStep(float ang)
        {
            if (ang > 70f) return 0;
            if (ang > 5f) return 1;
            if (ang > -37f) return 2;
            return 3;
        }

        // Uklad broni w ukladzie postaci patrzacej w prawo (srodek klatki = 0,0).
        private static void Layout(Weapon w, Vector2 aim, out Vector2 grip, out float rot, out Vector2 muzzle, out int step)
        {
            if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;
            Vector2 dir = aim.normalized;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            step = HoldStep(ang);
            Vector2 hand = HandUse[step];
            Sprite sp = Assets.Get(w.Id, HeldPpu);
            float wpx = sp != null ? sp.texture.width : 30f;
            float hpx = sp != null ? sp.texture.height : 30f;
            switch (KindOf(w))
            {
                case HoldKind.Bow:
                    grip = hand + dir * (2f / HeldPpu);
                    rot = ang;
                    muzzle = grip;
                    break;
                case HoldKind.Gun:
                    grip = hand;
                    rot = ang;
                    muzzle = hand + dir * (wpx * 0.75f / HeldPpu) + new Vector2(-dir.y, dir.x) * (hpx * 0.2f / HeldPpu);
                    break;
                case HoldKind.Staff:
                    grip = hand;
                    rot = ang - 45f;
                    muzzle = hand + dir * (Mathf.Sqrt(wpx * wpx + hpx * hpx) * 0.85f / HeldPpu);
                    break;
                case HoldKind.Rod:
                    grip = hand;
                    rot = ang;
                    muzzle = hand + dir * (wpx * 0.95f / HeldPpu);
                    break;
                default:
                    grip = hand + dir * (6f / HeldPpu);
                    rot = 0f;
                    muzzle = grip + dir * (wpx * 0.5f / HeldPpu);
                    break;
            }
        }

        // Punkt, z ktorego ma wyleciec pocisk (srodek luku, lufa, czubek rozdzki). false = brak postaci z Terrarii.
        public bool TryMuzzle(HeroController hero, Weapon w, Vector2 worldAim, out Vector3 point)
        {
            point = Vector3.zero;
            if (!Enabled || hero == null || frames == null) return false;
            bool right = hero.cState.facingRight;
            Vector2 aim = worldAim;
            if (!right) aim.x = -aim.x;
            Vector2 grip, muzzle;
            float rot;
            int step;
            Layout(w, aim, out grip, out rot, out muzzle, out step);
            if (!right) muzzle.x = -muzzle.x;
            Vector3 feet = Feet(hero);
            point = new Vector3(feet.x + muzzle.x, feet.y + FeetOffset + muzzle.y, hero.transform.position.z - 0.01f);
            return true;
        }

        // Cloud in a Bottle zamiast skrzydel Monarch Wings.
        private static void HideWings(HeroController hero)
        {
            if (wingsField == null) return;
            GameObject wings = wingsField.GetValue(hero) as GameObject;
            if (wings != null && wings.activeSelf && wings.scene.IsValid()) wings.SetActive(false);
        }

        private static void CloudPuff(HeroController hero)
        {
            Sounds.Play("Double_Jump", 0.7f);
            Vector3 feet = Feet(hero);
            for (int i = 0; i < 8; i++)
            {
                float a = Random.Range(200f, 340f) * Mathf.Deg2Rad;
                Dust.Spawn(feet + new Vector3(Random.Range(-0.3f, 0.3f), 0.1f, 0f),
                    new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(1f, 2.5f),
                    new Color(1f, 1f, 1f, 0.9f), 0.6f, 0.4f);
            }
        }
    }

    // Powidok postaci (kopia aktualnej klatki), znika w 0.2 s.
    public class Afterimage : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Color color;
        private float life = 0.2f;

        public static void Spawn(SpriteRenderer source, Transform root, Color tint)
        {
            GameObject g = new GameObject("TA_Afterimage");
            g.transform.position = source.transform.position + new Vector3(0f, 0f, 0.001f);
            g.transform.localScale = root.localScale;
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder - 1;
            sr.color = tint;
            Afterimage a = g.AddComponent<Afterimage>();
            a.sr = sr;
            a.color = tint;
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            Color c = color;
            c.a = color.a * (life / 0.2f);
            sr.color = c;
        }
    }

    // Nagrobek z Terrarii: spada z lekkim obrotem i lezy w miejscu smierci do zmiany lokacji.
    public class Tombstone : MonoBehaviour
    {
        private Vector2 velocity = new Vector2(0f, 4f);
        private bool landed;

        private void Start()
        {
            Destroy(gameObject, 30f);
        }

        private void Update()
        {
            if (landed) return;
            float dt = Time.deltaTime;
            velocity.y -= 20f * dt;
            Vector2 pos = transform.position;
            Vector2 next = pos + velocity * dt;
            RaycastHit2D hit = Physics2D.Linecast(pos, next + new Vector2(0f, -0.45f), Combat.TerrainMask);
            if (hit.collider != null && velocity.y < 0f)
            {
                transform.position = new Vector3(hit.point.x, hit.point.y + 0.45f, transform.position.z);
                landed = true;
                return;
            }
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }
    }

    // Prosty pylek/obloczek (kurz Hermes Boots, chmurka podwojnego skoku).
    public class Dust : MonoBehaviour
    {
        private Vector2 velocity;
        private float life, maxLife;
        private SpriteRenderer sr;
        private Color color;

        public static void Spawn(Vector3 pos, Vector2 velocity, Color color, float life, float scale)
        {
            GameObject go = Combat.MakeSprite("TA_Dust", Assets.Get("Dust", 8f), pos, color);
            go.transform.localScale = Vector3.one * scale * Random.Range(0.8f, 1.2f); // Dust.png ma 1 jednostke
            Dust d = go.AddComponent<Dust>();
            d.velocity = velocity;
            d.life = d.maxLife = life;
            d.color = color;
            d.sr = go.GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            velocity *= 1f - 2f * dt;
            transform.position += (Vector3)(velocity * dt);
            Color c = color;
            c.a = color.a * (life / maxLife);
            sr.color = c;
        }
    }
}
