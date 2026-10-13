// Zadawanie obrazen, pociski (strzaly, iskry) i miniony.

using System.Collections.Generic;
using UnityEngine;

namespace TerrariaArsenal
{
    public static class Combat
    {
        public const int TerrainMask = 1 << 8;
        public const int EnemyMask = 1 << 11;

        // Zadaje obrazenia wrogowi trafionemu kolizja. Zwraca true, jesli trafil.
        public static bool Damage(Collider2D col, int damage, GameObject source, float directionDeg)
        {
            if (col == null) return false;
            HealthManager hm = col.GetComponent<HealthManager>();
            if (hm == null) hm = col.GetComponentInParent<HealthManager>();
            if (hm == null || hm.GetIsDead()) return false;

            HitInstance hit = new HitInstance();
            hit.Source = source;
            hit.AttackType = AttackTypes.Generic;
            hit.DamageDealt = damage;
            hit.Direction = directionDeg;
            hit.Multiplier = 1f;
            hit.MagnitudeMultiplier = 1f;
            // Tak samo jak cios gwozdzia (DamageEnemies.DoDamage): zdarzenie FSM + trafienie.
            FSMUtility.SendEventToGameObject(col.gameObject, "TAKE DAMAGE");
            hm.Hit(hit);
            return true;
        }

        public static HealthManager NearestEnemy(Vector2 from, float range)
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(from, range, EnemyMask);
            HealthManager best = null;
            float bestDist = float.MaxValue;
            foreach (Collider2D c in cols)
            {
                HealthManager hm = c.GetComponent<HealthManager>();
                if (hm == null) hm = c.GetComponentInParent<HealthManager>();
                if (hm == null || hm.GetIsDead()) continue;
                float dist = Vector2.Distance(from, c.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = hm;
                }
            }
            return best;
        }

        // --- Niszczenie otoczenia (jak cios gwozdziem) ---
        private static bool slashInfo;
        private static int slashLayer = -1;
        private static string slashTag;

        // Daje obiektowi "cialo" ciosu gwozdziem: wyzwalacz z tagiem i warstwa ciecia Rycerza.
        // Dzieki temu dzwignie, pekajace sciany i podlogi reaguja na pociski i miniony.
        public static void MakeHitter(GameObject go, float radius)
        {
            if (!slashInfo)
            {
                HeroController hero = HeroController.instance;
                NailSlash slash = hero != null ? hero.GetComponentInChildren<NailSlash>(true) : null;
                if (slash == null) return;
                slashLayer = slash.gameObject.layer;
                slashTag = slash.gameObject.tag;
                slashInfo = true;
            }
            go.layer = slashLayer;
            go.tag = slashTag;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            // Bez tego cialo kinematyczne nie "widzi" statycznych obiektow (kokony, skaly z geo, sciany):
            // Box2D tworzy kontakt kinematyczny-statyczny tylko z pelnymi kontaktami kinematycznymi.
            rb.useFullKinematicContacts = true;
            CircleCollider2D c = go.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = radius / Mathf.Max(0.01f, go.transform.lossyScale.x);

            // Kokony lifeblood, skaly z geo, slupy itp. po trafieniu "Nail Attack" szukaja na atakujacym
            // FSM "damages_enemy" (np. zmiennej "direction"). Gra znajduje tylko WLACZONE FSM,
            // wiec dajemy wlaczone FSM z jednym pustym stanem (nic nie robi) i tymi zmiennymi.
            PlayMakerFSM fsm = go.AddComponent<PlayMakerFSM>();
            fsm.FsmName = "damages_enemy";
            HutongGames.PlayMaker.FsmState idle = new HutongGames.PlayMaker.FsmState(fsm.Fsm);
            idle.Name = "Idle";
            fsm.Fsm.States = new HutongGames.PlayMaker.FsmState[] { idle };
            fsm.Fsm.StartState = "Idle";
            HutongGames.PlayMaker.FsmFloat dir = new HutongGames.PlayMaker.FsmFloat("direction");
            HutongGames.PlayMaker.FsmFloat mag = new HutongGames.PlayMaker.FsmFloat("magnitudeMult");
            mag.Value = 1f;
            fsm.FsmVariables.FloatVariables = new HutongGames.PlayMaker.FsmFloat[] { dir, mag };
            HutongGames.PlayMaker.FsmInt dmg = new HutongGames.PlayMaker.FsmInt("damageDealt");
            HutongGames.PlayMaker.FsmInt type = new HutongGames.PlayMaker.FsmInt("attackType");
            HutongGames.PlayMaker.FsmInt special = new HutongGames.PlayMaker.FsmInt("Special Type");
            fsm.FsmVariables.IntVariables = new HutongGames.PlayMaker.FsmInt[] { dmg, type, special };
            HutongGames.PlayMaker.FsmBool move = new HutongGames.PlayMaker.FsmBool("moveDirection");
            fsm.FsmVariables.BoolVariables = new HutongGames.PlayMaker.FsmBool[] { move };
            HitDirection hd = go.AddComponent<HitDirection>();
            hd.Direction = dir;
        }

        // Kierunek trafienia (0 = w prawo, 90 = w gore, 180 = w lewo, 270 = w dol) dla obiektow z FSM.
        public static void SetHitDirection(GameObject go, Vector2 velocity)
        {
            if (go == null || velocity.sqrMagnitude < 0.0001f) return;
            HitDirection hd = go.GetComponent<HitDirection>();
            if (hd == null || hd.Direction == null) return;
            float a = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            if (a < 0f) a += 360f;
            // HK uzywa czterech kierunkow
            hd.Direction.Value = a < 45f || a >= 315f ? 0f : (a < 135f ? 90f : (a < 225f ? 180f : 270f));
        }

        // Doniczki, skrzynki, latarnie, trawa itp. (obiekty z IHitResponder poza wrogami).
        public static void HitBreakables(Vector2 pos, float radius, List<Collider2D> done, GameObject source, float directionDeg)
        {
            Collider2D[] cols = Physics2D.OverlapCircleAll(pos, radius);
            foreach (Collider2D c in cols)
            {
                if (c == null || done.Contains(c)) continue;
                GameObject g = c.gameObject;
                if (g.layer == 11 || g == source || g.GetComponent<HitDirection>() != null) continue;
                if (HeroController.instance != null && g.transform.IsChildOf(HeroController.instance.transform)) continue;
                // Skaly z geo, pekajace sciany, dzwignie itp. czekaja w FSM na zdarzenie "TAKE DAMAGE",
                // ktore gwozdz wysyla do wszystkiego, w co trafi.
                FSMUtility.SendEventToGameObject(g, "TAKE DAMAGE");
                done.Add(c);
                foreach (MonoBehaviour mb in c.GetComponents<MonoBehaviour>())
                {
                    IHitResponder r = mb as IHitResponder;
                    if (r == null || mb is HealthManager) continue;
                    HitInstance hit = new HitInstance();
                    hit.Source = source;
                    hit.AttackType = AttackTypes.Nail;
                    hit.DamageDealt = 1;
                    hit.Direction = directionDeg;
                    hit.Multiplier = 1f;
                    hit.MagnitudeMultiplier = 1f;
                    try
                    {
                        r.Hit(hit);
                    }
                    catch (System.Exception)
                    {
                    }
                }
            }
        }

        public static GameObject MakeSprite(string name, Sprite sprite, Vector3 pos, Color tint)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = 100;
            return go;
        }
    }

    public class HitDirection : MonoBehaviour
    {
        public HutongGames.PlayMaker.FsmFloat Direction;

    }

    public class Projectile : MonoBehaviour
    {
        public Vector2 Velocity;
        public float Gravity;
        public float Life = 1.5f;
        public int Damage;
        public int Pierce;              // ile wrogow moze przebic (0 = znika po pierwszym)
        public float SpriteAngleOffset; // obrot grafiki wzgledem kierunku lotu
        public bool Spin;
        public float Homing;            // stopnie/s skretu w strone najblizszego wroga (0 = brak)
        public bool FaceByFlip;         // zamiast obracac grafike, odbij ja w poziomie (pszczoly)
        public bool IgnoreTerrain;
        public float Burn;              // podpalenie trafionego wroga (sekundy)
        public int Bounce;              // ile razy odbija sie od scian
        public int BeesOnHit;           // pszczoly po trafieniu
        private Vector3 prevPos;

        private SpriteRenderer sr;
        private bool dying;
        private readonly List<Collider2D> alreadyHit = new List<Collider2D>();
        private readonly List<Collider2D> brokeHit = new List<Collider2D>();

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            Combat.MakeHitter(gameObject, 0.3f);
        }

        // Odbicie od sciany: promien z poprzedniej pozycji daje normalna powierzchni.
        private bool TryBounce()
        {
            Vector2 from = prevPos == Vector3.zero ? (Vector2)transform.position - Velocity * 0.05f : (Vector2)prevPos;
            Vector2 dir = Velocity.normalized;
            RaycastHit2D hit = Physics2D.Raycast(from, dir, Velocity.magnitude * 0.1f + 0.5f, Combat.TerrainMask);
            if (hit.collider == null) return false;
            Velocity = Vector2.Reflect(Velocity, hit.normal) * 0.8f;
            transform.position = (Vector3)(hit.point + hit.normal * 0.15f) + new Vector3(0f, 0f, transform.position.z);
            prevPos = transform.position;
            return true;
        }

        // Przy uderzeniu w sciane pocisk znika chwile pozniej, zeby sciana/dzwignia zdazyla poczuc trafienie.
        private void Stop()
        {
            dying = true;
            Velocity = Vector2.zero;
            if (sr != null) sr.enabled = false;
            Destroy(gameObject, 0.08f);
        }

        private void Update()
        {
            if (dying) return;
            float dt = Time.deltaTime;
            Life -= dt;
            if (Life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Velocity.y -= Gravity * dt;
            if (Homing > 0f)
            {
                HealthManager t = Combat.NearestEnemy(transform.position, 8f);
                if (t != null)
                {
                    Vector2 want = ((Vector2)(t.transform.position - transform.position)).normalized * Velocity.magnitude;
                    Velocity = Vector3.RotateTowards(Velocity, want, Homing * Mathf.Deg2Rad * dt, 0f);
                }
            }
            Vector3 pos = transform.position + (Vector3)(Velocity * dt);
            transform.position = pos;

            if (FaceByFlip)
            {
                if (sr != null) sr.flipX = Velocity.x < 0f;
            }
            else if (Spin)
            {
                transform.Rotate(0f, 0f, 720f * dt);
            }
            else
            {
                float angle = Mathf.Atan2(Velocity.y, Velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle + SpriteAngleOffset);
            }

            Combat.SetHitDirection(gameObject, Velocity);
            Combat.HitBreakables(pos, 0.35f, brokeHit, gameObject, Velocity.x >= 0f ? 0f : 180f);
            if (!IgnoreTerrain && Physics2D.OverlapPoint(pos, Combat.TerrainMask) != null)
            {
                if (Bounce > 0 && TryBounce())
                {
                    Bounce--;
                    return;
                }
                Stop();
                return;
            }
            prevPos = pos;

            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.35f, Combat.EnemyMask);
            foreach (Collider2D c in hits)
            {
                if (alreadyHit.Contains(c)) continue;
                float dir = Velocity.x >= 0f ? 0f : 180f;
                if (Combat.Damage(c, Damage, gameObject, dir))
                {
                    alreadyHit.Add(c);
                    HealthManager victim = c.GetComponent<HealthManager>();
                    if (victim == null) victim = c.GetComponentInParent<HealthManager>();
                    if (Burn > 0f) global::TerrariaArsenal.Burn.Apply(victim, Burn, 1);
                    if (BeesOnHit > 0) Effects.SpawnBees(transform.position, BeesOnHit, Mathf.Max(1, Damage / 2));
                    if (Pierce <= 0)
                    {
                        Stop();
                        return;
                    }
                    Pierce--;
                    Life *= 0.5f;
                }
            }
        }
    }

    // Minion przywolywacza: lata przy glowie Rycerza i nurkuje na najblizszego wroga.
    public class Minion : MonoBehaviour
    {
        public int Damage = 4;
        public float Cooldown = 0.9f;
        public Sprite[] Frames;               // klatki animacji (null albo 1 = bez animacji)
        public float FrameTime = 0.09f;

        private float animTime;
        private const float Range = 9f;
        private const float Speed = 14f;
        private float cooldownLeft;
        private HealthManager target;
        private SpriteRenderer sr;

        private readonly List<Collider2D> brokeHit = new List<Collider2D>();

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            Combat.MakeHitter(gameObject, 0.4f);
        }

        private void Update()
        {
            HeroController hero = HeroController.instance;
            if (hero == null) return;
            if (brokeHit.Count > 20) brokeHit.Clear();
            Combat.HitBreakables(transform.position, 0.4f, brokeHit, gameObject, 0f);

            float dt = Time.deltaTime;
            cooldownLeft -= dt;

            if (sr != null && Frames != null && Frames.Length > 1)
            {
                animTime += dt;
                sr.sprite = Frames[(int)(animTime / FrameTime) % Frames.Length];
            }
            Vector3 heroPos = hero.transform.position;
            float side = hero.cState.facingRight ? -0.8f : 0.8f;
            Vector3 home = heroPos + new Vector3(side, 1.4f + Mathf.Sin(Time.time * 4f) * 0.12f, -0.01f);

            // Po zmianie lokacji minion od razu wraca do Rycerza.
            if (Vector3.Distance(transform.position, heroPos) > 25f)
            {
                transform.position = home;
            }

            if (target != null && (target.GetIsDead() || !target.gameObject.activeInHierarchy ||
                Vector2.Distance(target.transform.position, heroPos) > Range * 1.5f))
            {
                target = null;
            }
            // Wrog trafiony biczem ma pierwszenstwo.
            HealthManager tagged = Effects.CurrentTag();
            if (tagged != null && cooldownLeft <= 0f && Vector2.Distance(tagged.transform.position, heroPos) < Range * 1.5f)
            {
                target = tagged;
            }
            if (target == null && cooldownLeft <= 0f)
            {
                target = Combat.NearestEnemy(heroPos, Range);
            }

            Vector3 goal = target != null && cooldownLeft <= 0f ? target.transform.position : home;
            goal.z = home.z;
            Combat.SetHitDirection(gameObject, goal - transform.position);
            float dx = goal.x - transform.position.x;
            transform.position = Vector3.MoveTowards(transform.position, goal, Speed * dt);
            // Obrot tylko przy wyraznym ruchu w bok; przy Rycerzu patrzy tam co on (bez migania).
            if (sr != null) sr.flipX = Mathf.Abs(dx) > 0.3f ? dx < 0f : !hero.cState.facingRight;

            if (target != null && cooldownLeft <= 0f &&
                Vector2.Distance(transform.position, target.transform.position) < 0.9f)
            {
                Collider2D col = target.GetComponent<Collider2D>();
                bool isTagged = target == Effects.CurrentTag();
                int dmg = Damage + (isTagged ? Effects.TagDamage : 0);
                Combat.Damage(col, dmg, gameObject, transform.position.x < target.transform.position.x ? 0f : 180f);
                if (isTagged) Effects.MinionHitTagged(target);
                cooldownLeft = Cooldown;
                target = null;
            }
        }
    }
}
