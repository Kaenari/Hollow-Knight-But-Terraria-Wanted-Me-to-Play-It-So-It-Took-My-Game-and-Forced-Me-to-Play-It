// Tabela broni: 9 pokoi x 4 klasy. Obrazenia juz przeliczone ze skali Terrarii
// wzorem round(1.41 * D^0.55). Cooldown = use time z wiki / 60 s.

using System.Collections.Generic;
using UnityEngine;

namespace TerrariaArsenal
{
    public enum WeaponClass { Melee = 0, Ranged = 1, Magic = 2, Summon = 3 }

    public class Weapon
    {
        public string Id;
        public string Name;
        public WeaponClass Class;
        public int Room;
        public int TerrariaDamage;
        public int Damage;
        public float Cooldown;

        // Wyglad i zachowanie pocisku (Strzelec, Mag) albo miniona (Przywolywacz).
        public string Sprite = "P_Arrow";
        public float Ppu = 32f;           // pikseli na jednostke swiata
        public float AngleOffset = -90f;  // sprite'y pociskow Terrarii sa zwrocone w gore
        public float Speed = 24f;
        public float Gravity = 9f;
        public int Pierce;
        public float Life = 2f;
        public int Count = 1;             // ile pociskow na strzal
        public float Spread;              // rozrzut w stopniach
        public int ManaCost;            // koszt many (tylko Mag)
        public bool Spin;
        public int Frames = 1;            // klatki animacji miniona (pasek pionowy w PNG)

        // Mechaniki z Terrarii
        public float Burn;                // podpalenie (On Fire!) na tyle sekund
        public int Bounce;                // odbicia od scian (Flower of Fire)
        public float Homing;              // samonaprowadzanie, stopnie/s (Razorblade Typhoon)
        public bool FromSky;              // pociski spadaja z nieba na cel (Daedalus Stormbow, Meteor Staff)
        public int BeesOnHit;             // pszczoly po trafieniu (The Bee's Knees)
        public Color Tint = Color.white;

        public Weapon(string id, string name, WeaponClass cls, int room, int terrariaDamage, int damage, float cooldown)
        {
            Id = id;
            Name = name;
            Class = cls;
            Room = room;
            TerrariaDamage = terrariaDamage;
            Damage = damage;
            Cooldown = cooldown;
        }
    }

    // Bicz przywolywacza (dostajesz go razem z laska z tego samego pokoju).
    // Tag: po trafieniu biczem miniony atakuja tego wroga i zadaja mu dodatkowe obrazenia.
    public class Whip
    {
        public string Id;
        public string Name;
        public string Icon;
        public int Room;
        public int TerrariaDamage;
        public int Damage;
        public float Cooldown;
        public float Range;      // w jednostkach HK
        public int TagDamage;

        public Whip(string id, string name, string icon, int room, int terrariaDamage, int useTime, int rangeTiles, int tag)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Room = room;
            TerrariaDamage = terrariaDamage;
            Damage = Weapons.Convert(terrariaDamage);
            Cooldown = useTime / 60f;
            Range = 2.5f + rangeTiles * 0.2f;
            TagDamage = tag > 0 ? Weapons.Convert(tag) : 0;
        }
    }

    public static class Weapons
    {
        // Przeliczenie obrazen z Terrarii na skale HK: round(1.41 * D^0.55).
        public static int Convert(int terrariaDamage)
        {
            return Mathf.Max(1, Mathf.RoundToInt(1.41f * Mathf.Pow(terrariaDamage, 0.55f)));
        }

        // Dane z terraria.wiki.gg/wiki/Whips (obrazenia, use time, zasieg w kafelkach, tag).
        public static readonly List<Whip> Whips = new List<Whip>
        {
            new Whip("LeatherWhip", "Leather Whip", "LeatherWhip", 0, 14, 30, 11, 4),
            new Whip("Snapthorn", "Snapthorn", "Snapthorn", 1, 18, 30, 15, 6),
            new Whip("SpinalTapWhip", "Spinal Tap", "SpinalTap", 2, 29, 30, 18, 7),
            new Whip("Firecracker", "Firecracker", "Firecracker", 3, 37, 30, 21, 0),
            new Whip("CoolWhip", "Cool Whip", "CoolWhip", 4, 45, 30, 23, 6),
            new Whip("Durendal", "Durendal", "Durendal", 5, 55, 28, 24, 9),
            new Whip("DarkHarvest", "Dark Harvest", "DarkHarvest", 6, 110, 27, 28, 15),
            new Whip("MorningStar", "Morning Star", "MorningStar", 7, 160, 35, 28, 8),
            new Whip("Kaleidoscope", "Kaleidoscope", "Kaleidoscope", 8, 170, 30, 33, 20)
        };

        // Bicz z pary z wybrana laska przywolywacza (albo null).
        public static Whip WhipFor(List<string> owned)
        {
            return WhipFor(ActiveSummon());
        }

        public static Whip WhipFor(Weapon staff)
        {
            if (staff == null) return null;
            foreach (Whip w in Whips)
            {
                if (w.Room == staff.Room) return w;
            }
            return null;
        }

        public static readonly string[] ClassNames = { "WOJOWNIK", "STRZELEC", "MAG", "PRZYWOŁYWACZ" };
        public static readonly string[] ClassNamesPl = { "Wojownik", "Strzelec", "Mag", "Przywoływacz" };

        public static readonly Color[] ClassColors =
        {
            new Color(1f, 0.35f, 0.3f),
            new Color(0.4f, 0.95f, 0.45f),
            new Color(0.4f, 0.65f, 1f),
            new Color(0.8f, 0.45f, 1f)
        };

        public static readonly string[] RoomNames =
        {
            "Start (King's Pass)", "False Knight", "Hornet (Greenpath)", "Mantis Lords",
            "Soul Master", "Broken Vessel", "Watcher Knights", "Uumuu", "The Hollow Knight"
        };

        public static readonly List<Weapon> All = new List<Weapon>
        {
            // Pokoj 0 - start
            new Weapon("CopperBroadsword", "Copper Broadsword", WeaponClass.Melee, 0, 9, 5, 0.35f),
            new Weapon("WoodenBow", "Wooden Bow", WeaponClass.Ranged, 0, 4, 3, 0.5f),
            new Weapon("WandOfSparking", "Wand of Sparking", WeaponClass.Magic, 0, 14, 6, 0.43f),
            new Weapon("FinchStaff", "Finch Staff", WeaponClass.Summon, 0, 7, 4, 0.9f),
            // Pokoj 1 - False Knight
            new Weapon("GoldBroadsword", "Gold Broadsword", WeaponClass.Melee, 1, 15, 6, 0.35f),
            new Weapon("GoldBow", "Gold Bow", WeaponClass.Ranged, 1, 11, 5, 0.45f),
            new Weapon("RubyStaff", "Ruby Staff", WeaponClass.Magic, 1, 18, 7, 0.4f),
            new Weapon("FlinxStaff", "Flinx Staff", WeaponClass.Summon, 1, 8, 4, 0.8f),
            // Pokoj 2 - Hornet
            new Weapon("EnchantedSword", "Enchanted Sword", WeaponClass.Melee, 2, 23, 8, 0.3f),
            new Weapon("DemonBow", "Demon Bow", WeaponClass.Ranged, 2, 14, 6, 0.4f),
            new Weapon("DiamondStaff", "Diamond Staff", WeaponClass.Magic, 2, 23, 8, 0.4f),
            new Weapon("VampireFrogStaff", "Vampire Frog Staff", WeaponClass.Summon, 2, 11, 5, 0.75f),
            // Pokoj 3 - Mantis Lords
            new Weapon("Muramasa", "Muramasa", WeaponClass.Melee, 3, 24, 8, 0.25f),
            new Weapon("BeesKnees", "The Bee's Knees", WeaponClass.Ranged, 3, 23, 8, 0.4f),
            new Weapon("AquaScepter", "Aqua Scepter", WeaponClass.Magic, 3, 27, 9, 0.3f),
            new Weapon("HornetStaff", "Hornet Staff", WeaponClass.Summon, 3, 12, 6, 0.7f),
            // Pokoj 4 - Soul Master
            new Weapon("BeeKeeper", "Bee Keeper", WeaponClass.Melee, 4, 30, 9, 0.3f),
            new Weapon("Handgun", "Handgun", WeaponClass.Ranged, 4, 26, 8, 0.3f),
            new Weapon("BookOfSkulls", "Book of Skulls", WeaponClass.Magic, 4, 29, 9, 0.45f),
            new Weapon("ImpStaff", "Imp Staff", WeaponClass.Summon, 4, 17, 7, 0.65f),
            // Pokoj 5 - Broken Vessel
            new Weapon("NightsEdge", "Night's Edge", WeaponClass.Melee, 5, 40, 11, 0.3f),
            new Weapon("MoltenFury", "Molten Fury", WeaponClass.Ranged, 5, 31, 9, 0.3f),
            new Weapon("FlowerOfFire", "Flower of Fire", WeaponClass.Magic, 5, 48, 12, 0.4f),
            new Weapon("SpiderStaff", "Spider Staff", WeaponClass.Summon, 5, 26, 8, 0.6f),
            // Pokoj 6 - Watcher Knights
            new Weapon("Excalibur", "Excalibur", WeaponClass.Melee, 6, 72, 15, 0.3f),
            new Weapon("DaedalusStormbow", "Daedalus Stormbow", WeaponClass.Ranged, 6, 38, 10, 0.3f),
            new Weapon("MeteorStaff", "Meteor Staff", WeaponClass.Magic, 6, 50, 12, 0.4f),
            new Weapon("PirateStaff", "Pirate Staff", WeaponClass.Summon, 6, 40, 11, 0.55f),
            // Pokoj 7 - Uumuu
            new Weapon("TerraBlade", "Terra Blade", WeaponClass.Melee, 7, 85, 16, 0.3f),
            new Weapon("Tsunami", "Tsunami", WeaponClass.Ranged, 7, 53, 13, 0.4f),
            new Weapon("RazorbladeTyphoon", "Razorblade Typhoon", WeaponClass.Magic, 7, 85, 16, 0.4f),
            new Weapon("TempestStaff", "Tempest Staff", WeaponClass.Summon, 7, 50, 12, 0.5f),
            // Pokoj 8 - The Hollow Knight
            new Weapon("Zenith", "Zenith", WeaponClass.Melee, 8, 190, 25, 0.5f),
            new Weapon("SDMG", "S.D.M.G.", WeaponClass.Ranged, 8, 85, 16, 0.1f),
            new Weapon("LastPrism", "Last Prism", WeaponClass.Magic, 8, 100, 18, 0.2f),
            new Weapon("Terraprisma", "Terraprisma", WeaponClass.Summon, 8, 90, 17, 0.4f)
        };

        static Weapons()
        {
            // Strzelec
            Shot("WoodenBow", "P_Arrow", 32f, 24f, 9f, 0, 0, 1, 0f);
            Shot("GoldBow", "P_Arrow", 32f, 26f, 9f, 0, 0, 1, 0f).Tint = new Color(1f, 0.9f, 0.5f);
            Shot("DemonBow", "P_Arrow", 32f, 28f, 8f, 0, 0, 1, 0f).Tint = new Color(0.8f, 0.6f, 1f);
            Shot("BeesKnees", "P_Arrow", 32f, 28f, 8f, 2, 0, 1, 0f).Tint = new Color(1f, 0.85f, 0.2f);
            Shot("Handgun", "P_Bullet", 28f, 42f, 0f, 0, 0, 1, 2f);
            Shot("MoltenFury", "P_FireArrow", 32f, 30f, 7f, 1, 0, 1, 0f);
            Shot("DaedalusStormbow", "P_HolyArrow", 32f, 32f, 6f, 1, 0, 3, 12f);
            Shot("Tsunami", "P_Arrow", 32f, 32f, 6f, 0, 0, 5, 18f).Tint = new Color(0.6f, 0.9f, 1f);
            Shot("SDMG", "P_Bullet", 28f, 48f, 0f, 0, 0, 1, 4f).Tint = new Color(0.6f, 1f, 0.9f);
            // Mag (ostatnie liczby: przebicie, koszt many, ilosc, rozrzut)
            Shot("WandOfSparking", "P_Spark", 16f, 16f, 4f, 1, 5, 1, 0f).Spin = true;
            Shot("RubyStaff", "P_Ruby", 44f, 20f, 0f, 1, 8, 1, 0f).Spin = true;
            Shot("DiamondStaff", "P_Diamond", 44f, 22f, 0f, 2, 8, 1, 0f).Spin = true;
            Shot("AquaScepter", "P_Water", 24f, 20f, 7f, 1, 5, 1, 4f);
            Shot("BookOfSkulls", "P_Skull", 32f, 14f, 0f, 3, 12, 1, 0f).AngleOffset = 0f;
            Shot("FlowerOfFire", "P_Fire", 24f, 18f, 10f, 1, 12, 1, 0f).Spin = true;
            Shot("MeteorStaff", "P_Meteor", 30f, 22f, 0f, 2, 15, 1, 0f).Spin = true;
            Shot("RazorbladeTyphoon", "P_Typhoon", 40f, 16f, 0f, 6, 15, 1, 0f).Spin = true;
            Shot("LastPrism", "P_Laser", 32f, 55f, 0f, 99, 4, 1, 0f);
            // Mechaniki specjalne jak w Terrarii
            Get("WandOfSparking").Burn = 2f;
            Get("MoltenFury").Burn = 3f;
            Get("FlowerOfFire").Burn = 3f;
            Get("FlowerOfFire").Bounce = 3;
            Get("BeesKnees").BeesOnHit = 2;
            Get("DaedalusStormbow").FromSky = true;
            Get("MeteorStaff").FromSky = true;
            Get("RazorbladeTyphoon").Homing = 360f;
            // Przywolywacz
            Summon("FinchStaff", "M_Finch", 4);
            Summon("FlinxStaff", "M_Flinx", 12);
            Summon("VampireFrogStaff", "M_Frog", 6);
            Summon("HornetStaff", "M_Hornet", 3);
            Summon("ImpStaff", "M_Imp", 8);
            Summon("SpiderStaff", "M_Spider", 4);
            Summon("PirateStaff", "M_Pirate", 4);
            Summon("TempestStaff", "M_Tempest", 6);
            Summon("Terraprisma", "M_Prism", 1);
        }

        private static Weapon Shot(string id, string sprite, float ppu, float speed, float gravity, int pierce, int mana, int count, float spread)
        {
            Weapon w = Get(id);
            w.Sprite = sprite;
            w.Ppu = ppu;
            w.Speed = speed;
            w.Gravity = gravity;
            w.Pierce = pierce;
            w.ManaCost = mana;
            w.Count = count;
            w.Spread = spread;
            w.Life = gravity > 0f ? 2f : 1.2f;
            return w;
        }

        private static void Summon(string id, string sprite, int frames)
        {
            Get(id).Sprite = sprite;
            Get(id).Frames = frames;
        }

        public static Weapon Get(string id)
        {
            foreach (Weapon w in All)
            {
                if (w.Id == id) return w;
            }
            return null;
        }

        public static Weapon Get(int room, WeaponClass cls)
        {
            foreach (Weapon w in All)
            {
                if (w.Room == room && w.Class == cls) return w;
            }
            return null;
        }

        // Posiadane bronie danej klasy w kolejnosci zdobycia.
        public static List<Weapon> Owned(WeaponClass cls)
        {
            List<Weapon> list = new List<Weapon>();
            foreach (string id in TerrariaArsenal.Instance.Data.Weapons)
            {
                Weapon w = Get(id);
                if (w != null && w.Class == cls && !list.Contains(w)) list.Add(w);
            }
            return list;
        }

        // Wybrany miecz (albo najmocniejszy, jesli nic nie wybrano).
        public static Weapon ActiveMelee()
        {
            return Active(TerrariaArsenal.Instance.Data.ActiveMelee, WeaponClass.Melee);
        }

        // Wybrana laska przywolywacza: jej minion lata przy postaci, a jej bicz jest pod reka.
        public static Weapon ActiveSummon()
        {
            return Active(TerrariaArsenal.Instance.Data.ActiveSummon, WeaponClass.Summon);
        }

        private static Weapon Active(string id, WeaponClass cls)
        {
            SaveData d = TerrariaArsenal.Instance.Data;
            if (!string.IsNullOrEmpty(id) && d.Weapons.Contains(id))
            {
                Weapon w = Get(id);
                if (w != null && w.Class == cls) return w;
            }
            return Best(d.Weapons, cls);
        }

        // Najmocniejsza posiadana bron danej klasy (albo null).
        public static Weapon Best(List<string> owned, WeaponClass cls)
        {
            Weapon best = null;
            foreach (string id in owned)
            {
                Weapon w = Get(id);
                if (w != null && w.Class == cls && (best == null || w.Damage > best.Damage)) best = w;
            }
            return best;
        }
    }
}
