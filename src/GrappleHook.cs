// Amethyst Hook z Terrarii pod klawiszem E (zamiast Mantis Claw / skoku od sciany).
// Hak leci w strone kursora (bez myszki: skos w gore do przodu), czepia sie terenu, przyciaga postac
// i trzyma ja w miejscu. Skok albo ponowne E odczepia.
// Dane z wiki Terrarii: zasieg 18,75 kafelka, pocisk 10 px/klatke, przyciaganie 11 px/klatke.

using System.Collections.Generic;
using UnityEngine;

namespace TerrariaArsenal
{
    public class GrappleHook : MonoBehaviour
    {
        public static GrappleHook Instance;
        public KeyCode HookKey = KeyCode.E;

        private const float Range = 10f;        // 18,75 kafelka Terrarii w jednostkach HK
        private const float ShootSpeed = 30f;
        private const float PullSpeed = 18f;
        private const float ChainStep = 8f / 30f;

        private enum State { None, Flying, Retracting, Attached }
        private State state = State.None;
        private Vector2 hookPos;
        private Vector2 hookVel;
        private bool controlTaken;
        private bool hanging;

        private GameObject head;
        private readonly List<SpriteRenderer> links = new List<SpriteRenderer>();

        // Czy gracz ma hak: dostaje go tam, gdzie w HK jest Mantis Claw.
        public static bool Owned()
        {
            return PlayerData.instance != null && PlayerData.instance.GetBool("hasWalljump");
        }

        // Czy wylaczyc skok od sciany (hak go zastepuje, chyba ze w ustawieniach zostawiono oba).
        public static bool BlocksWallJump()
        {
            return Owned() && !TerrariaArsenal.Instance.Settings.KeepWallJump;
        }

        public bool Busy
        {
            get { return state != State.None; }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            HeroController hero = HeroController.instance;
            GameManager gm = GameManager.instance;
            if (hero == null || gm == null || !gm.IsGameplayScene() || hero.cState.transitioning || hero.cState.dead)
            {
                Release(hero, false);
                state = State.None;
                DrawChain(null);
                return;
            }

            if (Input.GetKeyDown(HookKey) && Owned())
            {
                if (state == State.Attached || state == State.Flying) Release(hero, false);
                if (hero.acceptingInput || controlTaken) Launch(hero);
            }

            switch (state)
            {
                case State.Flying: Fly(hero); break;
                case State.Retracting: Retract(hero); break;
                case State.Attached: Pull(hero); break;
            }
            DrawChain(hero);
        }

        private void Launch(HeroController hero)
        {
            Vector2 aim = Runner.Instance != null ? Runner.Instance.HookAim(hero) : Vector2.up;
            hookPos = (Vector2)hero.transform.position + new Vector2(0f, 0.2f);
            hookVel = aim.normalized * ShootSpeed;
            state = State.Flying;
            Sounds.Play("Item_1", 0.5f);
        }

        private void Fly(HeroController hero)
        {
            Vector2 next = hookPos + hookVel * Time.deltaTime;
            RaycastHit2D hit = Physics2D.Linecast(hookPos, next, Combat.TerrainMask);
            if (hit.collider != null)
            {
                hookPos = hit.point;
                state = State.Attached;
                hanging = false;
                Sounds.Play("Item_1", 0.35f);
                return;
            }
            hookPos = next;
            if (Vector2.Distance(hookPos, hero.transform.position) > Range) state = State.Retracting;
        }

        private void Retract(HeroController hero)
        {
            Vector2 to = (Vector2)hero.transform.position - hookPos;
            float step = ShootSpeed * 1.4f * Time.deltaTime;
            if (to.magnitude <= step)
            {
                state = State.None;
                return;
            }
            hookPos += to.normalized * step;
        }

        private void Pull(HeroController hero)
        {
            Rigidbody2D rb = hero.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Release(hero, false);
                return;
            }
            if (!controlTaken)
            {
                hero.RelinquishControl();
                hero.AffectedByGravity(false);
                controlTaken = true;
            }

            // Obrazenia albo odrzut przerywaja (jak w HK przy trafieniu).
            if (hero.cState.recoiling || hero.cState.hazardDeath)
            {
                Release(hero, false);
                return;
            }

            bool jump = InputHandler.Instance != null && InputHandler.Instance.inputActions.jump.WasPressed;
            if (jump)
            {
                Release(hero, true);
                return;
            }

            // Dash w trakcie przyciagania: odczepia hak i od razu robi dash (jak w Terrarii).
            bool dash = InputHandler.Instance != null && InputHandler.Instance.inputActions.dash.WasPressed;
            if (dash)
            {
                Release(hero, false);
                TryDash(hero);
                return;
            }

            Vector2 target = hookPos + ((Vector2)hero.transform.position - hookPos).normalized * 0.7f;
            Vector2 to = target - (Vector2)hero.transform.position;
            if (to.magnitude < 0.15f)
            {
                rb.velocity = Vector2.zero;
                hanging = true;
            }
            else
            {
                rb.velocity = to.normalized * Mathf.Min(PullSpeed, to.magnitude / Time.deltaTime);
            }
            if (Mathf.Abs(to.x) > 0.05f)
            {
                if (to.x < 0f && hero.cState.facingRight) hero.FaceLeft();
                else if (to.x > 0f && !hero.cState.facingRight) hero.FaceRight();
            }
        }

        private static System.Reflection.MethodInfo canDashMethod, heroDashMethod;

        // Prywatne CanDash/HeroDash z HeroController - te same, ktorych gra uzywa po wcisnieciu dasha.
        private static void TryDash(HeroController hero)
        {
            if (canDashMethod == null)
            {
                System.Reflection.BindingFlags f = System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
                canDashMethod = typeof(HeroController).GetMethod("CanDash", f);
                heroDashMethod = typeof(HeroController).GetMethod("HeroDash", f);
            }
            if (canDashMethod == null || heroDashMethod == null) return;
            try
            {
                if ((bool)canDashMethod.Invoke(hero, null)) heroDashMethod.Invoke(hero, null);
            }
            catch (System.Exception e)
            {
                TerrariaArsenal.Instance.LogError("Dash z haka: " + e.Message);
            }
        }

        // Odczepienie. jumpOff = odbicie w gore (skok z haka).
        public void Release(HeroController hero, bool jumpOff)
        {
            if (controlTaken && hero != null)
            {
                hero.AffectedByGravity(true);
                hero.RegainControl();
                Rigidbody2D rb = hero.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float x = 0f;
                    if (InputHandler.Instance != null)
                    {
                        if (InputHandler.Instance.inputActions.left.IsPressed) x = -8f;
                        else if (InputHandler.Instance.inputActions.right.IsPressed) x = 8f;
                    }
                    rb.velocity = jumpOff ? new Vector2(x, 17f) : (hanging ? Vector2.zero : rb.velocity);
                }
            }
            controlTaken = false;
            hanging = false;
            // Hak wraca do postaci (jak w Terrarii), potem znika.
            if (state != State.None) state = State.Retracting;
        }

        // --- Grafika: lancuch z ogniw + glowica haka ---
        private void DrawChain(HeroController hero)
        {
            bool show = state != State.None && hero != null;
            if (head == null)
            {
                head = Combat.MakeSprite("TA_HookHead", Assets.Get("P_AmethystHook", 30f), Vector3.zero, Color.white);
                DontDestroyOnLoad(head);
            }
            head.SetActive(show);
            if (!show)
            {
                foreach (SpriteRenderer l in links) l.enabled = false;
                return;
            }

            Vector2 from = (Vector2)hero.transform.position + new Vector2(0f, 0.1f);
            Vector2 d = hookPos - from;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float z = hero.transform.position.z - 0.003f;
            head.transform.position = new Vector3(hookPos.x, hookPos.y, z);
            head.transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);   // tekstura haka: czubek w gore

            int need = Mathf.Min(60, Mathf.FloorToInt(d.magnitude / ChainStep));
            Sprite link = Assets.Get("P_AmethystChain", 30f);
            while (links.Count < need)
            {
                GameObject g = Combat.MakeSprite("TA_HookLink", link, Vector3.zero, Color.white);
                DontDestroyOnLoad(g);
                links.Add(g.GetComponent<SpriteRenderer>());
            }
            Vector2 n = d.normalized;
            for (int i = 0; i < links.Count; i++)
            {
                bool on = i < need;
                links[i].enabled = on;
                if (!on) continue;
                Vector2 p = from + n * (ChainStep * (i + 0.5f));
                links[i].transform.position = new Vector3(p.x, p.y, z + 0.001f);
                links[i].transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f); // ogniwo: os pionowa wzdluz lancucha
            }
        }
    }
}
