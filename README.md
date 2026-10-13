# Hollow Knight: But Terraria Wanted Me to Play It, So It Took My Game and Forced Me to Play It

*Polski poniżej · English below*

---

## 🇵🇱 Polski

Mod do **Hollow Knight**, w którym grasz postacią z Terrarii z bronią z Terrarii. Do wyboru masz miecze, łuki, magię oraz laski przywoływacza z biczami. Pod spodem działa fizyka HK. Umiejętności ruchu HK zamieniają się w akcesoria z Terrarii, a bossowie i lokacje dostają muzykę z Terrarii.

### Wymagania
- Hollow Knight **1.5.78** z **Modding API** (np. przez Scarab albo Lumafly).
- **Terraria zainstalowana na tym samym komputerze** (Steam). Mod nie zawiera żadnych grafik, dźwięków ani muzyki z Terrarii, tylko czyta je z Twojej instalacji. Bez Terrarii mod nie działa.

### Instalacja
**Najprościej:** w [Lumafly](https://themulhima.github.io/Lumafly/) wyszukaj ten mod i kliknij **Install**.

**Ręcznie:**
1. Pobierz `TerrariaArsenal_release.zip` z zakładki *Releases* i rozpakuj go tak, żeby powstał folder `...\Hollow Knight\hollow_knight_Data\Managed\Mods\TerrariaArsenal\` z plikiem `TerrariaArsenal.dll`.
2. Uruchom grę. Mod sam znajdzie Terrarię w bibliotekach Steam. Jeśli jej nie znajdzie, w menu głównym pojawi się okno, w którym wklejasz folder Terrarii i klikasz **Zapisz**. Ścieżkę możesz też wpisać ręcznie w `TerrariaPath` w pliku `TerrariaArsenal.GlobalSettings.json` (w folderze zapisów gry: `%AppData%\..\LocalLow\Team Cherry\Hollow Knight\`).

### Wygląd postaci
Na ekranie wyboru zapisu obok każdego slotu stoi Twoja postać. Na podświetlonym slocie naciśnij **C** (albo kliknij postać), żeby otworzyć kreator. Możesz tam ustawić typ ciała, fryzurę oraz kolory włosów, skóry, oczu, koszuli, podkoszulka, spodni i butów. Jest też przycisk **Losuj**. Każdy zapis ma swój wygląd.

### Sterowanie
| Klawisz | Działanie |
|---|---|
| **LPM / X** (atak HK) | atak bronią z ręki: cięcie mieczem, smagnięcie biczem, strzał albo czar |
| **F** | strzał wybraną bronią strzelającą bez zmiany przedmiotu w ręce |
| **R / kółko myszy** | zmiana przedmiotu w ręce (każdy miecz, każdy bicz z laską, każda broń strzelająca) |
| **E** | Amethyst Hook (gdy masz go zamiast Mantis Claw); skok odczepia |
| **Dół + atak w powietrzu** | odbicie od kolców i wrogów (pogo) każdą bronią |
| **W górę** przy skrzyni | otwarcie skrzyni kluczem |
| **F9** | przełączenie wyglądu: postać z Terrarii / Rycerz |

Jeśli używasz myszy, strzały i czary lecą w stronę kursora. Na samej klawiaturze lecą przed siebie, a z wciśniętym **W** w górę. Na padzie celujesz prawą gałką.

### Jak zdobywasz bronie
W King's Pass i po każdym z 8 bossów pojawiają się **4 skrzynie**, po jednej z każdej klasy, oraz **klucz**. Klucz otwiera jedną skrzynię, więc za każdym razem wybierasz jedną broń, z dowolnej klasy. Możesz zbierać kilka broni z tej samej klasy i przełączać się między nimi.

Pokoje ze skrzyniami: Start (King's Pass), False Knight, Hornet (Greenpath), Mantis Lords, Soul Master, Broken Vessel, Watcher Knights, Uumuu, The Hollow Knight.

| Klasa | Bronie |
|---|---|
| Wojownik | Copper Broadsword → Gold Broadsword → Enchanted Sword → Muramasa → Bee Keeper → Night's Edge → Excalibur → Terra Blade → Zenith |
| Strzelec | Wooden Bow → Gold Bow → Demon Bow → The Bee's Knees → Handgun → Molten Fury → Daedalus Stormbow → Tsunami → S.D.M.G. |
| Mag | Wand of Sparking → Ruby Staff → Diamond Staff → Aqua Scepter → Book of Skulls → Flower of Fire → Meteor Staff → Razorblade Typhoon → Last Prism |
| Przywoływacz | Finch → Flinx → Vampire Frog → Hornet → Imp → Spider → Pirate → Tempest Staff → Terraprisma, każda laska z biczem z Terrarii |

- Obrażenia pochodzą z wiki Terrarii i są przeliczone na skalę HK.
- Mana (gwiazdki w prawym górnym rogu) odnawia się sama, szybciej gdy stoisz.
- Miniony atakują same, a trafienie biczem oznacza cel, na którym się skupiają.
- Pociski, bicze i miniony niszczą to samo, co gwóźdź: trawę, skały z geo i ściany.

### Umiejętności ruchu jako akcesoria
| HK | Terraria |
|---|---|
| (zawsze) | Hermes Boots: biegnij dłużej, a przyspieszysz do sprintu |
| Mothwing Cloak | Tabi |
| Mantis Claw | Amethyst Hook pod **E** (zamiast wspinania po ścianach) |
| Monarch Wings | Cloud in a Bottle |
| Crystal Heart | Shield of Cthulhu |
| Isma's Tear | Flipper |
| Shade Cloak | Master Ninja Gear |
| Lumafly Lantern | Mining Helmet |

### Ustawienia (`TerrariaArsenal.GlobalSettings.json`)
- `TerrariaPath`: folder Terrarii. Puste oznacza automatyczne wyszukiwanie.
- `TerrariaAreaMusic`: `true` włącza muzykę biomów Terrarii w lokacjach, `false` zostawia ją tylko na bossach.
- `KeepWallJump`: `true` zostawia skok od ściany z Mantis Claw oprócz haka (awaryjnie).

Ulubione tematy bossów HK (Hornet, Mantis Lords, Grimm, The Hollow Knight, Radiance, Pure Vessel) zostają oryginalne.

---

## 🇬🇧 English

A **Hollow Knight** mod that lets you play as a Terraria character with Terraria weapons: swords, bows, magic, and summoner staffs with whips. It runs on HK physics underneath. HK movement abilities become Terraria accessories, and bosses and areas get Terraria music.

### Requirements
- Hollow Knight **1.5.78** with the **Modding API** (e.g. via Scarab or Lumafly).
- **Terraria installed on the same PC** (Steam). The mod ships **no Terraria graphics, sounds or music**. It reads them from your own installation. The mod does not work without Terraria.

### Installation
**Easiest:** search for this mod in [Lumafly](https://themulhima.github.io/Lumafly/) and click **Install**.

**Manually:**
1. Download `TerrariaArsenal_release.zip` from *Releases* and unzip it so that you get `...\Hollow Knight\hollow_knight_Data\Managed\Mods\TerrariaArsenal\TerrariaArsenal.dll`.
2. Start the game. Terraria is found automatically in your Steam libraries. If it is not found, a window in the main menu lets you paste the Terraria folder and click **Save**. You can also set `TerrariaPath` in `TerrariaArsenal.GlobalSettings.json` (in the HK save folder: `%AppData%\..\LocalLow\Team Cherry\Hollow Knight\`).

### Character appearance
On the save-select screen your character stands next to each slot. Press **C** on the highlighted slot (or click the character) to open the creator. There you can set body type, hairstyle, and the colours of hair, skin, eyes, shirt, undershirt, pants and shoes. There is also a **Random** button. Each save has its own look.

### Controls
| Key | Action |
|---|---|
| **LMB / X** (HK attack) | use the item in hand: sword swing, whip lash, shot or spell |
| **F** | fire the selected ranged/magic weapon without switching |
| **R / mouse wheel** | switch item in hand (every sword, every whip with its staff, every ranged/magic weapon) |
| **E** | Amethyst Hook (replaces Mantis Claw); jump to let go |
| **Down + attack in the air** | pogo off spikes and enemies with any weapon |
| **Up** at a chest | open it with the key |
| **F9** | toggle look: Terraria character / the Knight |

With a mouse, shots and spells fly towards the cursor. On keyboard only they go forward, or upward while **W** is held. On a gamepad you aim with the right stick.

### Getting weapons
In King's Pass and after each of 8 bosses, **4 chests** appear (one per class) together with a **key**. The key opens one chest, so each time you pick one weapon from any class. You can own several weapons of the same class and switch between them.

Chest rooms: Start (King's Pass), False Knight, Hornet (Greenpath), Mantis Lords, Soul Master, Broken Vessel, Watcher Knights, Uumuu, The Hollow Knight.

| Class | Weapons |
|---|---|
| Melee | Copper Broadsword → Gold Broadsword → Enchanted Sword → Muramasa → Bee Keeper → Night's Edge → Excalibur → Terra Blade → Zenith |
| Ranged | Wooden Bow → Gold Bow → Demon Bow → The Bee's Knees → Handgun → Molten Fury → Daedalus Stormbow → Tsunami → S.D.M.G. |
| Magic | Wand of Sparking → Ruby Staff → Diamond Staff → Aqua Scepter → Book of Skulls → Flower of Fire → Meteor Staff → Razorblade Typhoon → Last Prism |
| Summoner | Finch → Flinx → Vampire Frog → Hornet → Imp → Spider → Pirate → Tempest Staff → Terraprisma, each staff paired with a Terraria whip |

- Damage comes from the Terraria wiki, scaled to HK.
- Mana (stars, top right) regenerates on its own, faster while standing still.
- Minions attack on their own, and whip hits mark the target they focus.
- Projectiles, whips and minions break whatever the nail breaks: grass, geo rocks and walls.

### Movement abilities as accessories
| HK | Terraria |
|---|---|
| (always) | Hermes Boots: keep running to break into a sprint |
| Mothwing Cloak | Tabi |
| Mantis Claw | Amethyst Hook on **E** (instead of wall climbing) |
| Monarch Wings | Cloud in a Bottle |
| Crystal Heart | Shield of Cthulhu |
| Isma's Tear | Flipper |
| Shade Cloak | Master Ninja Gear |
| Lumafly Lantern | Mining Helmet |

### Settings (`TerrariaArsenal.GlobalSettings.json`)
- `TerrariaPath`: Terraria folder. Empty means auto-detect.
- `TerrariaAreaMusic`: `true` plays Terraria biome music in areas too, `false` keeps it on bosses only.
- `KeepWallJump`: `true` keeps the Mantis Claw wall jump alongside the hook (fallback).

Fan-favourite HK boss themes (Hornet, Mantis Lords, Grimm, The Hollow Knight, Radiance, Pure Vessel) are kept.

---

*Fan mod. Not affiliated with Re-Logic or Team Cherry. Terraria content is loaded at runtime from your own copy of Terraria. Hollow Knight content belongs to Team Cherry.*
