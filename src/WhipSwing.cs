// Zamach biczem jak w Terrarii: bicz rozwija sie lukiem znad glowy do celu i strzela koncem.
// Trafiony wrog dostaje "tag": miniony lataja do niego i zadaja mu dodatkowe obrazenia.

using System.Collections.Generic;
using UnityEngine;

namespace TerrariaArsenal
{
    public class WhipSwing : MonoBehaviour
    {
        private const int Segments = 12;
        private const float Duration = 0.42f;

        private Whip whip;
        private Vector2 aim;
        private float side;
        private float t;
        private bool cracked;
        private SpriteRenderer[] parts;
        private GameObject tip;
        private readonly List<Collider2D> hitEnemies = new List<Collider2D>();
        private readonly List<Collider2D> hitOther = new List<Collider2D>();

        public static void Spawn(Whip whip, Vector2 aim)
        {
            HeroController hero = HeroController.instance;
            if (hero == null) return;
            GameObject go = new GameObject("TA_Whip");
            WhipSwing s = go.AddComponent<WhipSwing>();
            s.whip = whip;
            s.aim = aim.sqrMagnitude > 0.001f ? aim.normalized : (hero.cState.facingRight ? Vector2.right : Vector2.left);
            s.side = Mathf.Abs(s.aim.x) > 0.15f ? Mathf.Sign(s.aim.x) : (hero.cState.facingRight ? 1f : -1f);
            Sounds.Play("Item_152", 0.7f);
        }

        private void Start()
        {
            Sprite handle = Assets.Get("Wh_" + whip.Id + "_H", 26f);
            Sprite seg = Assets.Get("Wh_" + whip.Id + "_S", 26f);
            Sprite end = Assets.Get("Wh_" + whip.Id + "_T", 26f);
            parts = new SpriteRenderer[Segments];
            for (int i = 0; i < Segments; i++)
            {
                Sprite sp = i == 0 ? handle : (i == Segments - 1 ? end : seg);
                GameObject g = Combat.MakeSprite("TA_WhipPart", sp, transform.position, Color.white);
                g.transform.SetParent(transform, true);
                parts[i] = g.GetComponent<SpriteRenderer>();
                parts[i].sortingOrder = 101;
            }
            tip = new GameObject("TA_WhipTip");
            tip.transform.SetParent(transform, false);
            Combat.MakeHitter(tip, 0.45f);
        }

        private static Vector2 Dir(float deg)
        {
            return new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad));
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

            // Zamach: znad glowy (za plecami) do kierunku celu, potem bicz wraca.
            float target = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            float start = target + side * 125f;
            float k = Mathf.Clamp01(t / 0.6f);
            float ease = 1f - (1f - k) * (1f - k);
            float ang = Mathf.LerpAngle(start, target, ease);
            float len = t < 0.6f ? Mathf.Lerp(0.25f, 1f, ease) : Mathf.Lerp(1f, 0.2f, (t - 0.6f) / 0.4f);
            len *= whip.Range;

            // Bicz wychodzi z dloni postaci (klatka animacji z TerrariaPlayer).
            Vector2 hand = TerrariaPlayer.Instance != null
                ? TerrariaPlayer.Instance.HandWorld(hero)
                : (Vector2)hero.transform.position + new Vector2(side * 0.2f, -0.1f);
            Vector2 tipPos = hand + Dir(ang) * len;
            Vector2 ctrl = hand + Dir(ang + side * 40f * (1f - k)) * len * 0.55f;   // srodek bicza sie spoznia

            float z = hero.transform.position.z - 0.003f;
            for (int i = 0; i < Segments; i++)
            {
                float u0 = i / (float)Segments, u1 = (i + 1) / (float)Segments;
                Vector2 a = Bezier(hand, ctrl, tipPos, u0);
                Vector2 b = Bezier(hand, ctrl, tipPos, u1);
                Vector2 d = b - a;
                Transform tr = parts[i].transform;
                tr.position = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, z);
                // Czesci bicza w Terrarii sa narysowane pionowo, koncem w dol.
                tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f);
            }
            Combat.SetHitDirection(tip, tipPos - (Vector2)tip.transform.position);
            tip.transform.position = new Vector3(tipPos.x, tipPos.y, z);

            if (!cracked && t > 0.55f)
            {
                cracked = true;
                Sounds.Play("Item_153", 0.5f);
            }

            // Bicz rani na calej dlugosci w trakcie zamachu.
            if (t > 0.2f && t < 0.85f)
            {
                for (int i = 1; i <= 6; i++)
                {
                    Vector2 p = Bezier(hand, ctrl, tipPos, i / 6f);
                    Collider2D[] hits = Physics2D.OverlapCircleAll(p, 0.4f, Combat.EnemyMask);
                    foreach (Collider2D c in hits)
                    {
                        if (hitEnemies.Contains(c)) continue;
                        hitEnemies.Add(c);
                        if (Combat.Damage(c, whip.Damage, gameObject, side > 0f ? 0f : 180f))
                        {
                            HealthManager hm = c.GetComponent<HealthManager>();
                            if (hm == null) hm = c.GetComponentInParent<HealthManager>();
                            Effects.Tag(hm, whip.TagDamage, whip.Id == "Firecracker" ? whip.Damage : 0);
                        }
                    }
                    Combat.HitBreakables(p, 0.35f, hitOther, gameObject, side > 0f ? 0f : 180f);
                }
            }
        }

        private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float u)
        {
            float v = 1f - u;
            return v * v * a + 2f * v * u * b + u * u * c;
        }
    }
}
