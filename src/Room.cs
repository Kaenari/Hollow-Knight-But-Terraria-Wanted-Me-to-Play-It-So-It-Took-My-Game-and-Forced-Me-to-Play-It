// Pokoj wyboru: 4 skrzynie (po jednej na klase) + klucz, ktory otwiera tylko jedna.

using System.Collections.Generic;
using UnityEngine;

namespace TerrariaArsenal
{
    public class Chest : MonoBehaviour
    {
        public int Room;
        public Weapon Weapon;
        public bool PlayerNear;
    }

    public class KeyPickup : MonoBehaviour
    {
        public int Room;
    }

    public static class ChestRoom
    {
        public static readonly List<Chest> Chests = new List<Chest>();
        public static KeyPickup Key;
        public static int SpawnedRoom = -1;

        private const float ChestPpu = 26f;
        private const float Spacing = 2.4f;

        public static void Spawn(int room, bool needsKey)
        {
            Clear();
            HeroController hero = HeroController.instance;
            Vector3 h = hero.transform.position;
            float z = h.z - 0.01f;
            Vector2 eye = new Vector2(h.x, h.y + 0.4f);

            // Szukamy scian po lewej i prawej, zeby skrzynie nie weszly w teren.
            RaycastHit2D left = Physics2D.Raycast(eye, Vector2.left, 8f, Combat.TerrainMask);
            RaycastHit2D right = Physics2D.Raycast(eye, Vector2.right, 8f, Combat.TerrainMask);
            float minX = left.collider != null ? left.point.x + 0.9f : h.x - 8f;
            float maxX = right.collider != null ? right.point.x - 0.9f : h.x + 8f;

            float spacing = Mathf.Min(Spacing, Mathf.Max(1.1f, (maxX - minX) / 3f));
            float half = spacing * 1.5f;
            float centre = Mathf.Clamp(h.x, minX + half, Mathf.Max(minX + half, maxX - half));

            for (int i = 0; i < 4; i++)
            {
                WeaponClass cls = (WeaponClass)i;
                Weapon w = Weapons.Get(room, cls);
                float x = centre - half + i * spacing;
                float y = GroundY(x, h.y) + 0.55f;
                Color tint = Color.Lerp(Color.white, Weapons.ClassColors[i], 0.45f);
                GameObject go = Combat.MakeSprite("TA_Chest_" + cls, Assets.Get(room == 8 ? "GoldChest" : "Chest", ChestPpu),
                    new Vector3(x, y, z), tint);
                Chest c = go.AddComponent<Chest>();
                c.Room = room;
                c.Weapon = w;
                Chests.Add(c);

                // Ikona broni unoszaca sie nad skrzynia.
                GameObject icon = Combat.MakeSprite("TA_Icon", Assets.WeaponIcon(w, 40f), new Vector3(x, y + 1.3f, z), Color.white);
                icon.transform.parent = go.transform;
                icon.AddComponent<Bob>();
            }

            if (needsKey)
            {
                float kx = Mathf.Clamp(h.x + (hero.cState.facingRight ? 1.2f : -1.2f), minX, maxX);
                float ky = GroundY(kx, h.y) + 0.6f;
                GameObject k = Combat.MakeSprite("TA_Key", Assets.Get("Key", 30f), new Vector3(kx, ky, z), Color.white);
                k.AddComponent<Bob>();
                Key = k.AddComponent<KeyPickup>();
                Key.Room = room;
            }
            SpawnedRoom = room;
        }

        public static void Clear()
        {
            foreach (Chest c in Chests)
            {
                if (c != null) Object.Destroy(c.gameObject);
            }
            Chests.Clear();
            if (Key != null) Object.Destroy(Key.gameObject);
            Key = null;
            SpawnedRoom = -1;
        }

        private static float GroundY(float x, float heroY)
        {
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, heroY + 1.5f), Vector2.down, 10f, Combat.TerrainMask);
            if (hit.collider != null) return hit.point.y;
            return heroY - 0.6f;
        }
    }

    // Lekkie unoszenie sie ikon i klucza.
    public class Bob : MonoBehaviour
    {
        private Vector3 start;
        private float phase;

        private void Start()
        {
            start = transform.localPosition;
            phase = Random.value * 6f;
        }

        private void Update()
        {
            transform.localPosition = start + new Vector3(0f, Mathf.Sin(Time.time * 2.5f + phase) * 0.1f, 0f);
        }
    }
}
