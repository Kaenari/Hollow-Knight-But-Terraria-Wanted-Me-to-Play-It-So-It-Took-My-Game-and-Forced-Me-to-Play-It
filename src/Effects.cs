// Efekty jak w Terrarii: liczby obrazen nad wrogami, trafienia krytyczne,
// promienie mieczy (Enchanted Sword, Terra Blade), pszczoly Bee Keepera i latajace miecze Zenith.

using System.Collections.Generic;
using GlobalEnums;
using UnityEngine;

namespace TerrariaArsenal
{
    // Wyskakujace liczby obrazen (pomaranczowe, krytyki wieksze i czerwonawe).
    public static class DamageNumbers
    {
        private class Entry
        {
            public Vector3 Pos;
            public string Text;
            public bool Crit;
            public float Age;
            public float Drift;
            public bool Heal;
        }

        private const float LifeTime = 1f;
        private static readonly List<Entry> Entries = new List<Entry>();
        private static readonly Color Normal = new Color(1f, 0.63f, 0.31f);
        private static readonly Color CritColor = new Color(1f, 0.39f, 0.12f);

        public static void Add(Vector3 worldPos, int damage, bool crit)
        {
            if (damage <= 0) return;
            Entry e = new Entry();
            e.Pos = worldPos + new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f);
            e.Text = damage.ToString();
            e.Crit = crit;
            e.Drift = Random.Range(-0.3f, 0.3f);
            Entries.Add(e);
            if (Entries.Count > 40) Entries.RemoveAt(0);
        }

        // Zielona liczba leczenia (jak w Terrarii przy odzyskaniu zycia).
        public static void AddHeal(Vector3 worldPos, int amount)
        {
            if (amount <= 0) return;
            Entry e = new Entry();
            e.Pos = worldPos;
            e.Text = amount.ToString();
            e.Heal = true;
            Entries.Add(e);
            if (Entries.Count > 40) Entries.RemoveAt(0);
        }

        public static void Tick(float dt)
        {
            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                Entries[i].Age += dt;
                if (Entries[i].Age >= LifeTime) Entries.RemoveAt(i);
            }
        }

        public static void Clear()
        {
            Entries.Clear();
        }

        public static void Draw(float ui)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            foreach (Entry e in Entries)
            {
                float k = e.Age / LifeTime;
                // Szybko w gore, potem zwalnia (jak CombatText w Terrarii).
                float rise = 1.3f * (1f - (1f - k) * (1f - k));
                Vector3 sp = cam.WorldToScreenPoint(e.Pos + new Vector3(e.Drift * k, rise, 0f));
                if (sp.z < 0f) continue;
                float pop = k < 0.12f ? Mathf.Lerp(1.5f, 1f, k / 0.12f) : 1f;
                float sc = (e.Crit ? 1.35f : 1.05f) * pop * ui;
                Color c = e.Heal ? new Color(0.39f, 1f, 0.39f) : (e.Crit ? CritColor : Normal);
                c.a = k > 0.65f ? Mathf.InverseLerp(1f, 0.65f, k) : 1f;
                TerrariaFont.DrawCentered(sp.x, Screen.height - sp.y - TerrariaFont.LineHeight(sc) * 0.5f, e.Text, c, sc);
            }
        }
    }

    public static class Effects
    {
        public const float CritChance = 0.08f;

        // Tag bicza (jak w Terrarii): ostatnio trafiony biczem wrog.
        public static HealthManager TaggedEnemy;
        public static int TagDamage;
        public static float TagUntil;

        public static int TagExplosion;   // Firecracker: pierwszy cios miniona w oznaczonego wroga wybucha

        public static void Tag(HealthManager hm, int tagDamage, int explosion)
        {
            if (hm == null) return;
            TaggedEnemy = hm;
            TagDamage = tagDamage;
            TagExplosion = explosion;
            TagUntil = Time.time + 4f;
        }

        // Wybuch Firecrackera (w Terrarii 275% obrazen bicza; tu 1.5x, zeby nie rozbijac balansu HK).
        public static void MinionHitTagged(HealthManager hm)
        {
            if (hm == null || TagExplosion <= 0 || hm.GetIsDead()) return;
            int dmg = Mathf.RoundToInt(TagExplosion * 1.5f);
            TagExplosion = 0;
            Vector3 p = hm.transform.position;
            for (int i = 0; i < 14; i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Dust.Spawn(p, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2f, 5f),
                    new Color(1f, Random.Range(0.3f, 0.8f), 0.1f, 1f), 0.45f, 0.3f);
            }
            Sounds.Play("Item_14", 0.7f);
            hm.ApplyExtraDamage(dmg);
            DamageNumbers.Add(p + new Vector3(0f, 1f, 0f), dmg, true);
        }

        public static HealthManager CurrentTag()
        {
            if (TaggedEnemy == null || Time.time > TagUntil || TaggedEnemy.GetIsDead() || !TaggedEnemy.gameObject.activeInHierarchy)
                return null;
            return TaggedEnemy;
        }

        private static readonly string[] ZenithSwords =
        {
            "CopperBroadsword", "GoldBroadsword", "EnchantedSword", "Muramasa",
            "BeeKeeper", "NightsEdge", "Excalibur", "TerraBlade"
        };

        private static Weapon BestMelee()
        {
            return Weapons.ActiveMelee();
        }

        // Czy to trafienie pochodzi od broni z moda (pociski, minion, gwozdz wzmocniony mieczem)?
        public static bool IsOurs(HitInstance hit)
        {
            if (hit.AttackType == AttackTypes.Nail) return BestMelee() != null;
            GameObject src = hit.Source;
            if (src == null) return false;
            return src.GetComponent<Projectile>() != null || src.GetComponent<Minion>() != null ||
                   src.GetComponent<ZenithSword>() != null || src.GetComponent<WhipSwing>() != null ||
                   src.GetComponent<SwordArc>() != null;
        }

        // Wywolywane po kazdym zamachu gwozdziem.
        public static void OnAttack(AttackDirection direction)
        {
            if (TerrariaPlayer.Instance != null)
                TerrariaPlayer.Instance.OnAttack(direction == AttackDirection.upward ? 1 : direction == AttackDirection.downward ? 2 : 0);
            if (TerrariaPlayer.Enabled) Sounds.Play("Item_1", 0.8f);
            HeroController hero = HeroController.instance;
            if (hero == null) return;

            // Odbicie w dol, gdy w rece jest luk/magia/bicz: cios gwozdzia zostaje (to on odbija od kolcow),
            // ale jest niewidoczny, a widac uderzenie bronia z reki.
            Weapon held = null;
            string icon = Runner.Instance != null ? Runner.Instance.InHandIcon(out held) : null;
            if (icon != null)
            {
                if (direction == AttackDirection.downward && TerrariaPlayer.Enabled)
                {
                    HideNailSlash(hero);
                    if (TerrariaPlayer.Instance != null) TerrariaPlayer.Instance.OnBonk(icon, held);
                }
                return;
            }

            Weapon w = BestMelee();
            if (w == null) return;

            bool right = hero.cState.facingRight;
            Vector2 dir;
            if (direction == AttackDirection.upward) dir = Vector2.up;
            else if (direction == AttackDirection.downward) dir = Vector2.down;
            else dir = right ? Vector2.right : Vector2.left;

            switch (w.Id)
            {
                case "EnchantedSword":
                    Beam(hero, dir, "P_EnchantedBeam", 28f, 20f, w.Damage, 0, 0.55f);
                    break;
                case "TerraBlade":
                    Beam(hero, dir, "P_TerraBeam", 30f, 24f, w.Damage, 2, 0.7f);
                    break;
                case "NightsEdge":
                    SwordArc.Spawn(hero, dir, new Color(0.65f, 0.35f, 1f, 0.9f), w.Damage);
                    break;
                case "Excalibur":
                    SwordArc.Spawn(hero, dir, new Color(1f, 0.85f, 0.35f, 0.9f), w.Damage);
                    break;
                case "Zenith":
                    for (int i = 0; i < 2; i++) SpawnZenithSword(hero, dir, w.Damage);
                    break;
            }
        }

        // Ukrywa biale ciecie gwozdzia HK, ktore wlasnie sie zaczelo (NailSlash sam je wylaczy na koncu,
        // a przy nastepnym cieciu wlaczy z powrotem).
        private static void HideNailSlash(HeroController hero)
        {
            foreach (NailSlash slash in hero.GetComponentsInChildren<NailSlash>(true))
            {
                MeshRenderer mr = slash.GetComponent<MeshRenderer>();
                if (mr != null && mr.enabled) mr.enabled = false;
            }
        }

        // Wywolywane, gdy gwozdz trafi wroga.
        public static void OnNailHit(HealthManager enemy)
        {
            Weapon w = BestMelee();
            if (w != null && w.Id == "Muramasa" && enemy != null)
            {
                MuramasaSlash.Spawn(enemy, Mathf.Max(1, w.Damage / 2));
                return;
            }
            if (w == null || w.Id != "BeeKeeper" || enemy == null) return;
            SpawnBees(enemy.transform.position, Random.Range(1, 4), Mathf.Max(1, w.Damage / 3));
        }

        // Pszczoly jak z Bee Keepera / The Bee's Knees: lataja i same szukaja wrogow.
        public static void SpawnBees(Vector3 at, int count, int damage)
        {
            Sprite bee = Assets.Get("P_Bee", 20f);
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = at + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 0.8f), -0.01f);
                GameObject go = Combat.MakeSprite("TA_Bee", bee, pos, Color.white);
                Projectile p = go.AddComponent<Projectile>();
                float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                p.Velocity = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 7f;
                p.Life = 1.6f;
                p.Damage = damage;
                p.Homing = 600f;
                p.FaceByFlip = true;
                p.IgnoreTerrain = true;
            }
        }

        private static void Beam(HeroController hero, Vector2 dir, string sprite, float ppu, float speed, int damage, int pierce, float life)
        {
            Vector3 origin = hero.transform.position + new Vector3(dir.x * 0.8f, dir.y * 0.8f, -0.01f);
            GameObject go = Combat.MakeSprite("TA_Beam", Assets.Get(sprite, ppu), origin, Color.white);
            Projectile p = go.AddComponent<Projectile>();
            p.Velocity = dir * speed;
            p.Life = life;
            p.Damage = damage;
            p.Pierce = pierce;
            p.SpriteAngleOffset = -45f; // miecze w Terrarii sa narysowane po skosie (czubek w prawy gorny rog)
        }

        private static void SpawnZenithSword(HeroController hero, Vector2 dir, int damage)
        {
            Vector3 heroPos = hero.transform.position;
            HealthManager target = Combat.NearestEnemy(heroPos, 10f);
            Vector3 goal = target != null
                ? target.transform.position
                : heroPos + (Vector3)(dir * Random.Range(5f, 7f)) + new Vector3(0f, Random.Range(-1f, 1f), 0f);
            goal += new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.6f, 0.6f), 0f);

            string id = ZenithSwords[Random.Range(0, ZenithSwords.Length)];
            GameObject go = Combat.MakeSprite("TA_Zenith", Assets.Get(id, 26f), heroPos, Color.white);
            ZenithSword z = go.AddComponent<ZenithSword>();
            z.Goal = goal;
            z.Side = Random.Range(-2.2f, 2.2f);
            z.Damage = damage;
            z.Duration = Random.Range(0.4f, 0.55f);
        }
    }

    // Luk ciecia z Terrarii 1.4 (Night's Edge fioletowy, Excalibur zloty): rozlega sie przed postacia
    // i rani wszystko w zasiegu, wiec miecz siega dalej niz sam gwozdz.
    public class SwordArc : MonoBehaviour
    {
        private const float Duration = 0.26f;
        private Sprite[] frames;
        private SpriteRenderer sr;
        private Vector2 dir;
        private Color color;
        private int damage;
        private float t;
        private readonly List<Collider2D> hit = new List<Collider2D>();
        private readonly List<Collider2D> broke = new List<Collider2D>();

        public static void Spawn(HeroController hero, Vector2 dir, Color color, int damage)
        {
            GameObject go = Combat.MakeSprite("TA_SwordArc", null, hero.transform.position, color);
            SwordArc a = go.AddComponent<SwordArc>();
            a.dir = dir;
            a.color = color;
            a.damage = damage;
        }

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            frames = Assets.GetFrames("P_SlashArc", 60f, 4);
            if (frames != null) sr.sprite = frames[0];
            // Tekstura: luk otwarty w prawo. Obracamy w kierunku ataku.
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        private void Update()
        {
            HeroController hero = HeroController.instance;
            t += Time.deltaTime;
            if (hero == null || t >= Duration)
            {
                Destroy(gameObject);
                return;
            }
            float k = t / Duration;
            Vector3 center = hero.transform.position + (Vector3)(dir * 0.9f);
            center.z = hero.transform.position.z - 0.004f;
            transform.position = center;
            if (frames != null) sr.sprite = frames[Mathf.Min(3, (int)(k * 4f))];
            Color c = color;
            c.a = color.a * (1f - k * 0.6f);
            sr.color = c;

            if (k > 0.65f) return;
            Vector2 hitCenter = (Vector2)hero.transform.position + dir * 1.4f;
            foreach (Collider2D col in Physics2D.OverlapCircleAll(hitCenter, 1.6f, Combat.EnemyMask))
            {
                if (hit.Contains(col)) continue;
                hit.Add(col);
                Combat.Damage(col, damage, gameObject, dir.x >= 0f ? 0f : 180f);
            }
            Combat.HitBreakables(hitCenter, 1.2f, broke, gameObject, dir.x >= 0f ? 0f : 180f);
        }
    }

    // Muramasa (Terraria 1.4.4): po trafieniu wroga przecinaja go dwie niebieskie smugi.
    public class MuramasaSlash : MonoBehaviour
    {
        private const float Duration = 0.18f;
        private HealthManager target;
        private int damage;
        private float t;
        private bool dealt;
        private SpriteRenderer sr;
        private float baseLen;

        public static void Spawn(HealthManager enemy, int damage)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 p = enemy.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.4f), -0.02f);
                GameObject go = Combat.MakeSprite("TA_Muramasa", Assets.Get("P_Muramasa", 24f), p, new Color(0.6f, 0.85f, 1f, 1f));
                go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 180f));
                MuramasaSlash m = go.AddComponent<MuramasaSlash>();
                m.target = enemy;
                m.damage = i == 0 ? damage : 0;   // obrazenia raz, druga smuga tylko wyglad
            }
        }

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            baseLen = 1f;
        }

        private void Update()
        {
            t += Time.deltaTime;
            if (t >= Duration)
            {
                Destroy(gameObject);
                return;
            }
            float k = t / Duration;
            transform.localScale = new Vector3(1f, baseLen * Mathf.Lerp(0.3f, 1.6f, k), 1f);
            Color c = sr.color;
            c.a = 1f - k;
            sr.color = c;
            if (!dealt && k > 0.4f)
            {
                dealt = true;
                if (damage > 0 && target != null && !target.GetIsDead())
                {
                    target.ApplyExtraDamage(damage);
                    DamageNumbers.Add(target.transform.position + new Vector3(0f, 0.9f, 0f), damage, false);
                }
            }
        }
    }

    // Podpalenie (On Fire! z Terrarii): obrazenia co 0.5 s bez odrzutu, plomyki nad wrogiem.
    public class Burn : MonoBehaviour
    {
        private HealthManager hm;
        private float until;
        private float tick;
        private int damage;

        public static void Apply(HealthManager target, float seconds, int damagePerTick)
        {
            if (target == null || target.GetIsDead() || seconds <= 0f) return;
            Burn b = target.GetComponent<Burn>();
            if (b == null) b = target.gameObject.AddComponent<Burn>();
            b.hm = target;
            b.until = Mathf.Max(b.until, Time.time + seconds);
            b.damage = Mathf.Max(b.damage, damagePerTick);
            if (b.tick <= 0f) b.tick = 0.5f;
        }

        private void Update()
        {
            if (hm == null || hm.GetIsDead() || Time.time > until)
            {
                Destroy(this);
                return;
            }
            if (Random.value < Time.deltaTime * 14f)
            {
                Vector3 p = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.6f), -0.02f);
                Dust.Spawn(p, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1f, 2f)),
                    new Color(1f, Random.Range(0.4f, 0.7f), 0.1f, 0.9f), 0.5f, 0.16f);
            }
            tick -= Time.deltaTime;
            if (tick > 0f) return;
            tick = 0.5f;
            Vector3 top = transform.position + new Vector3(0f, 0.8f, 0f);
            hm.ApplyExtraDamage(damage);
            DamageNumbers.Add(top, damage, false);
        }
    }

    // Miecz Zenitha: leci lukiem od Rycerza do celu i wraca, raniac wszystko po drodze.
    public class ZenithSword : MonoBehaviour
    {
        public Vector3 Goal;
        public float Side;
        public int Damage;
        public float Duration = 0.5f;

        private float t;
        private Vector3 offset;  // cel wzgledem Rycerza w chwili rzutu
        private SpriteRenderer sr;
        private readonly List<Collider2D> alreadyHit = new List<Collider2D>();

        private readonly List<Collider2D> brokeHit = new List<Collider2D>();

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            Combat.MakeHitter(gameObject, 0.5f);
            HeroController hero = HeroController.instance;
            offset = hero != null ? Goal - hero.transform.position : Vector3.zero;
            offset.z = 0f;
        }

        private void Update()
        {
            HeroController hero = HeroController.instance;
            if (hero == null)
            {
                Destroy(gameObject);
                return;
            }
            t += Time.deltaTime / Duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 start = hero.transform.position;
            Vector3 perp = new Vector3(-offset.y, offset.x, 0f).normalized * Side;
            float a = t * Mathf.PI * 2f;
            Vector3 pos = start + offset * (1f - Mathf.Cos(a)) * 0.5f + perp * Mathf.Sin(a) * 0.5f;
            pos.z = start.z - 0.01f;

            Vector3 vel = pos - transform.position;
            if (vel.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle - 45f);
            }
            transform.position = pos;
            if (sr != null)
            {
                Color c = sr.color;
                c.a = t > 0.8f ? Mathf.InverseLerp(1f, 0.8f, t) : 1f;
                sr.color = c;
            }

            Combat.SetHitDirection(gameObject, vel);
            Combat.HitBreakables(pos, 0.5f, brokeHit, gameObject, vel.x >= 0f ? 0f : 180f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.6f, Combat.EnemyMask);
            foreach (Collider2D c in hits)
            {
                if (alreadyHit.Contains(c)) continue;
                if (Combat.Damage(c, Damage, gameObject, vel.x >= 0f ? 0f : 180f)) alreadyHit.Add(c);
            }
        }
    }
}
