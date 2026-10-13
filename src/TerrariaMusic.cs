// Muzyka bossow i lokacji z Terrarii, czytana prosto z zainstalowanej Terrarii gracza (Content\Wave Bank.xwb),
// wiec mod nie zawiera zadnej muzyki Terrarii. Bez Terrarii zostaje zwykla muzyka HK.
// Format: XACT 3 (Sound Bank.xsb mapuje "Music_N" na numer utworu w Wave Bank.xwb), utwory MS-ADPCM albo PCM.
// Ulubione tematy bossow HK (Mantis Lords, Hornet, The Hollow Knight, Radiance, Grimm, Pure Vessel) zostaja.
// W lokacjach gra muzyka biomu Terrarii o podobnym klimacie (wylaczane: TerrariaAreaMusic w ustawieniach).

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

namespace TerrariaArsenal
{
    public class TerrariaMusic : MonoBehaviour
    {
        public static TerrariaMusic Instance;

        // Nazwa bossa z BossBar -> numer muzyki Terrarii (MusicID, jak na wiki Terrarii).
        private static readonly Dictionary<string, int> BossMusic = new Dictionary<string, int>
        {
            { "False Knight", 5 }, { "Failed Champion", 5 },                 // Boss 1 (Eye of Cthulhu)
            { "Gruz Mother", 93 },                                           // King Slime
            { "Brooding Mawlek", 13 },                                       // Boss 3 (Brain of Cthulhu)
            { "Dung Defender", 17 }, { "White Defender", 17 },               // Boss 4 (Golem)
            { "Soul Master", 94 }, { "Soul Tyrant", 94 },                    // Lunatic Cultist
            { "Crystal Guardian", 56 }, { "Enraged Guardian", 56 },          // Queen Slime
            { "Broken Vessel", 12 }, { "Lost Kin", 12 },                     // Boss 2 (Wall of Flesh)
            { "Nosk", 90 },                                                  // Deerclops
            { "Flukemarm", 99 },                                             // Eater of Worlds
            { "Hive Knight", 96 },                                           // Queen Bee
            { "Watcher Knights", 104 },                                      // Skeletron
            { "The Collector", 98 },                                         // Skeletron Prime
            { "Uumuu", 58 },                                                 // Duke Fishron
            { "Traitor Lord", 24 },                                          // Plantera
            { "Brothers Oro & Mato", 97 },                                   // The Twins
            { "Paintmaster Sheo", 57 },                                      // Empress of Light
            { "Great Nailsage Sly", 38 },                                    // Lunar Boss (Moon Lord)
            { "God Tamer", 25 },                                             // Boss 5
            { "Grey Prince Zote", 101 },                                     // Torch God
            { "Xero", 13 }, { "Gorb", 13 }, { "Marmu", 13 }, { "No Eyes", 13 },
            { "Elder Hu", 13 }, { "Galien", 13 }, { "Markoth", 13 }          // Warrior Dreams -> Boss 3
        };

        // Obszar HK (MapZone) -> muzyka biomu Terrarii o podobnym klimacie.
        private static readonly Dictionary<string, int> AreaMusic = new Dictionary<string, int>
        {
            { "KINGS_PASS", 4 },          // Underground
            { "TOWN", 47 },               // Dirtmouth: Town Night
            { "CLIFFS", 44 },             // Howling Cliffs: Windy Day
            { "CROSSROADS", 31 },         // Alt Underground
            { "SHAMAN_TEMPLE", 31 },
            { "TRAM_UPPER", 31 }, { "TRAM_LOWER", 31 },
            { "GREEN_PATH", 7 },          // Jungle
            { "OVERGROWN_MOUND", 7 }, { "ISMAS_GROVE", 7 },
            { "ROYAL_GARDENS", 54 },      // Queen's Gardens: Underground Jungle
            { "HIVE", 54 },
            { "FOG_CANYON", 43 },         // Ocean Night
            { "MONOMON_ARCHIVE", 43 },
            { "BLUE_LAKE", 22 },          // Lake of Unn: Ocean
            { "WASTES", 29 },             // Fungal Wastes: Mushrooms
            { "QUEENS_STATION", 29 }, { "MANTIS_VILLAGE", 29 },
            { "DEEPNEST", 33 },           // Underground Crimson
            { "BEASTS_DEN", 33 }, { "DISTANT_VILLAGE", 33 }, { "RUINED_TRAMWAY", 33 },
            { "MINES", 11 },              // Crystal Peak: Underground Hallow
            { "PEAK", 11 }, { "CRYSTAL_MOUND", 11 },
            { "RESTING_GROUNDS", 53 },    // Graveyard
            { "GLADE", 53 }, { "JONI_GRAVE", 53 }, { "NOEYES_TEMPLE", 53 },
            { "CITY", 19 },               // City of Tears: Rain
            { "SOUL_SOCIETY", 19 }, { "MAGE_TOWER", 19 }, { "LURIENS_TOWER", 19 },
            { "LOVE_TOWER", 19 }, { "KINGS_STATION", 19 }, { "ROYAL_QUARTER", 19 },
            { "WATERWAYS", 10 },          // Underground Corruption
            { "ACID_LAKE", 10 },
            { "PALACE_GROUNDS", 23 },     // Ancient Basin: Dungeon
            { "ABYSS", 2 },               // Eerie
            { "ABYSS_DEEP", 2 },
            { "OUTSKIRTS", 21 },          // Kingdom's Edge: Desert
            { "WYRMSKIN", 21 },
            { "COLOSSEUM", 41 },          // Old One's Army
            { "WHITE_PALACE", 9 },        // The Hallow
            { "GODS_GLORY", 91 },         // Godhome: Aether
            { "GODSEEKER_WASTE", 91 },
            { "BONE_FOREST", 4 }
        };

        private WaveBank bank;
        private bool triedLoad;
        private AudioSource source;
        private int playingId = -1;
        private readonly List<AudioSource> mutedHk = new List<AudioSource>();
        private float scanTimer;
        private string zone = "";
        private float zoneTimer;
        private static System.Reflection.FieldInfo musicSourcesField;

        private void Awake()
        {
            Instance = this;
        }

        // Po ustawieniu nowej sciezki Terrarii: wczytaj Wave Bank od nowa.
        public void ResetBank()
        {
            Stop();
            bank = null;
            triedLoad = false;
        }

        // Co powinno grac teraz: muzyka bossa, muzyka obszaru albo nic (-1 = zostaje muzyka HK).
        private int Wanted(out string what)
        {
            what = null;
            GameManager gm = GameManager.instance;
            if (gm == null || !gm.IsGameplayScene()) return -1;
            string boss = BossBar.Current;
            if (boss != null)
            {
                int bm;
                what = boss;
                // Boss spoza listy (ulubione tematy fanow HK): zostaje oryginalna muzyka.
                return BossMusic.TryGetValue(boss, out bm) ? bm : -1;
            }
            if (!TerrariaArsenal.Instance.Settings.TerrariaAreaMusic) return -1;
            zoneTimer -= Time.unscaledDeltaTime;
            if (zoneTimer <= 0f)
            {
                zoneTimer = 0.5f;
                try
                {
                    zone = gm.GetCurrentMapZone() ?? "";
                }
                catch (Exception)
                {
                    zone = "";
                }
            }
            int am;
            what = zone;
            return AreaMusic.TryGetValue(zone, out am) ? am : -1;
        }

        private void Update()
        {
            string what;
            int want = Wanted(out what);
            if (want < 0)
            {
                if (playingId >= 0) Stop();
            }
            else if (want != playingId)
            {
                Play(what, want);
            }

            if (source != null && source.isPlaying)
            {
                source.volume = Volume();
                MuteHkMusic(false);
            }
        }

        private static float Volume()
        {
            GameManager gm = GameManager.instance;
            if (gm == null || gm.gameSettings == null) return 0.6f;
            return Mathf.Clamp01(gm.gameSettings.masterVolume / 10f) * Mathf.Clamp01(gm.gameSettings.musicVolume / 10f) * 0.85f;
        }

        private void Play(string what, int musicId)
        {
            playingId = musicId;   // takze przy bledzie, zeby nie probowac co klatke
            if (!triedLoad)
            {
                triedLoad = true;
                string dir = TerrariaFiles.ContentDir();
                if (dir == null)
                {
                    TerrariaArsenal.Instance.Log("Muzyka: nie znaleziono Terrarii, zostaje muzyka HK.");
                    return;
                }
                try
                {
                    bank = WaveBank.Open(Path.Combine(dir, "Wave Bank.xwb"), Path.Combine(dir, "Sound Bank.xsb"));
                    TerrariaArsenal.Instance.Log("Muzyka: wczytano Wave Bank (" + bank.Count + " utworow).");
                }
                catch (Exception e)
                {
                    TerrariaArsenal.Instance.LogError("Muzyka: nie udalo sie odczytac Wave Bank: " + e.Message);
                    bank = null;
                }
            }
            if (bank == null) return;

            AudioClip clip = null;
            try
            {
                clip = bank.CreateClip(musicId);
            }
            catch (Exception e)
            {
                TerrariaArsenal.Instance.LogError("Muzyka: utwor " + musicId + ": " + e.Message);
            }
            if (clip == null) return;

            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.spatialBlend = 0f;
                source.loop = true;
                source.playOnAwake = false;
                source.ignoreListenerPause = true;
            }
            source.Stop();
            AudioClip old = source.clip;
            source.clip = clip;
            if (old != null && old != clip) Destroy(old);
            source.volume = Volume();
            source.Play();
            MuteHkMusic(true);
            TerrariaArsenal.Instance.Log("Muzyka: " + what + " -> Terraria Music_" + musicId);
        }

        private void Stop()
        {
            if (source != null) source.Stop();
            playingId = -1;
            foreach (AudioSource a in mutedHk)
            {
                if (a != null) a.mute = false;
            }
            mutedHk.Clear();
        }

        private void Mute(AudioSource a)
        {
            if (a == null || a == source || a.mute) return;
            a.mute = true;
            mutedHk.Add(a);
        }

        // Wycisza muzyke HK: zrodla AudioManagera co klatke, reszte zrodel z mikserem muzyki co pol sekundy.
        private void MuteHkMusic(bool full)
        {
            GameManager gm = GameManager.instance;
            if (gm != null && gm.AudioManager != null)
            {
                if (musicSourcesField == null)
                {
                    musicSourcesField = typeof(AudioManager).GetField("musicSources",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                }
                AudioSource[] ms = musicSourcesField != null ? musicSourcesField.GetValue(gm.AudioManager) as AudioSource[] : null;
                if (ms != null) foreach (AudioSource a in ms) Mute(a);
            }

            scanTimer -= Time.unscaledDeltaTime;
            if (!full && scanTimer > 0f) return;
            scanTimer = 0.5f;
            foreach (AudioSource a in FindObjectsOfType<AudioSource>())
            {
                if (a == null || a == source || a.mute) continue;
                AudioMixerGroup g = a.outputAudioMixerGroup;
                if (g == null) continue;
                string mixer = g.audioMixer != null ? g.audioMixer.name : "";
                if (mixer.IndexOf("Music", StringComparison.OrdinalIgnoreCase) < 0 &&
                    g.name.IndexOf("Music", StringComparison.OrdinalIgnoreCase) < 0) continue;
                Mute(a);
            }
        }
    }

    // Szukanie zainstalowanej Terrarii (biblioteki Steam albo sciezka z ustawien moda).
    public static class TerrariaFiles
    {
        private static string cached;
        private static bool searched;

        // Zapomina wynik szukania (po wpisaniu nowej sciezki w menu).
        public static void Reset()
        {
            searched = false;
            cached = null;
        }

        // Sprawdza sciezke wpisana przez gracza: folder Terrarii, jej folder Content albo plik Terraria.exe.
        // Zwraca folder Terrarii (ten, w ktorym jest Content) albo null.
        public static string Validate(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            string p = input.Trim().Trim('"').Trim();
            if (p.Length == 0) return null;
            try
            {
                if (File.Exists(p)) p = Path.GetDirectoryName(p);
                p = p.TrimEnd('\\', '/');
                if (File.Exists(Path.Combine(Path.Combine(p, "Content"), "Wave Bank.xwb"))) return p;
                if (File.Exists(Path.Combine(p, "Wave Bank.xwb"))) return Path.GetDirectoryName(p);
            }
            catch (Exception)
            {
            }
            return null;
        }

        // Tylko do testow (F8 w menu glownym w wersji testowej): udaje, ze Terrarii nie ma.
        public static bool ForceMissing;

        public static string ContentDir()
        {
            if (ForceMissing) return null;
            if (searched) return cached;
            searched = true;
            List<string> candidates = new List<string>();
            string custom = TerrariaArsenal.Instance.Settings.TerrariaPath;
            if (!string.IsNullOrEmpty(custom)) candidates.Add(custom);

            string steam = null;
            try
            {
                steam = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            }
            catch (Exception)
            {
            }
            if (string.IsNullOrEmpty(steam)) steam = @"C:\Program Files (x86)\Steam";
            candidates.Add(Path.Combine(steam, @"steamapps\common\Terraria"));
            try
            {
                string vdf = Path.Combine(steam, @"steamapps\libraryfolders.vdf");
                if (File.Exists(vdf))
                {
                    foreach (string line in File.ReadAllLines(vdf))
                    {
                        string t = line.Trim();
                        if (!t.StartsWith("\"path\"")) continue;
                        int a = t.IndexOf('"', 6);
                        int b = t.LastIndexOf('"');
                        if (a < 0 || b <= a) continue;
                        string lib = t.Substring(a + 1, b - a - 1).Replace(@"\\", @"\");
                        candidates.Add(Path.Combine(lib, @"steamapps\common\Terraria"));
                    }
                }
            }
            catch (Exception)
            {
            }

            foreach (string c in candidates)
            {
                string content = Path.Combine(c, "Content");
                if (File.Exists(Path.Combine(content, "Wave Bank.xwb")))
                {
                    cached = content;
                    TerrariaArsenal.Instance.Log("Znaleziono Terrarie: " + c);
                    break;
                }
            }
            return cached;
        }
    }

    // Czytnik XACT Wave Bank (+ Sound Bank do mapowania nazw) z dekoderem MS-ADPCM, strumieniowo.
    public class WaveBank
    {
        private class Entry
        {
            public int Format;      // 0 PCM, 1 XMA, 2 ADPCM, 3 WMA
            public int Channels;
            public int Rate;
            public int BlockAlign;  // dla ADPCM: pelny rozmiar bloku w bajtach
            public int Bits;
            public long Offset;
            public int Length;
        }

        private string path;
        private readonly List<Entry> entries = new List<Entry>();
        private readonly Dictionary<int, int> cueToWave = new Dictionary<int, int>();

        public int Count
        {
            get { return entries.Count; }
        }

        public static WaveBank Open(string xwb, string xsb)
        {
            WaveBank w = new WaveBank();
            w.path = xwb;
            w.ReadXwb(xwb);
            if (File.Exists(xsb)) w.ReadXsb(xsb);
            return w;
        }

        private void ReadXwb(string file)
        {
            using (BinaryReader r = new BinaryReader(File.OpenRead(file)))
            {
                if (new string(r.ReadChars(4)) != "WBND") throw new Exception("to nie jest Wave Bank");
                uint version = r.ReadUInt32();
                uint headerVersion = r.ReadUInt32();
                uint[] segOff = new uint[5], segLen = new uint[5];
                for (int i = 0; i < 5; i++)
                {
                    segOff[i] = r.ReadUInt32();
                    segLen[i] = r.ReadUInt32();
                }
                r.BaseStream.Position = segOff[0];
                uint flags = r.ReadUInt32();
                int count = (int)r.ReadUInt32();
                r.ReadBytes(64); // nazwa
                int metaSize = (int)r.ReadUInt32();
                r.ReadUInt32(); // rozmiar nazwy
                uint alignment = r.ReadUInt32();
                uint compactFormat = r.ReadUInt32();
                bool compact = (flags & 0x00020000) != 0;
                TerrariaArsenal.Instance.Log("Wave Bank: wersja " + version + "/" + headerVersion + ", utworow " + count +
                                            ", meta " + metaSize + (compact ? " (compact)" : ""));

                for (int i = 0; i < count; i++)
                {
                    r.BaseStream.Position = segOff[1] + (long)i * metaSize;
                    Entry e = new Entry();
                    uint fmt;
                    if (compact)
                    {
                        uint v = r.ReadUInt32();
                        fmt = compactFormat;
                        e.Offset = segOff[4] + (long)(v & 0x1FFFFF) * alignment;
                        e.Length = 0;
                    }
                    else
                    {
                        r.ReadUInt32(); // flagi + dlugosc
                        fmt = r.ReadUInt32();
                        e.Offset = segOff[4] + r.ReadUInt32();
                        e.Length = (int)r.ReadUInt32();
                    }
                    e.Format = (int)(fmt & 3);
                    e.Channels = (int)((fmt >> 2) & 7);
                    e.Rate = (int)((fmt >> 5) & 0x3FFFF);
                    int ba = (int)((fmt >> 23) & 0xFF);
                    e.Bits = ((fmt >> 31) & 1) != 0 ? 16 : 8;
                    e.BlockAlign = e.Format == 2 ? (ba + 22) * e.Channels : ba;
                    entries.Add(e);
                }
                if (entries.Count > 0)
                {
                    Entry f = entries[0];
                    TerrariaArsenal.Instance.Log("Wave Bank: pierwszy utwor format " + f.Format + ", kanaly " + f.Channels +
                                                ", " + f.Rate + " Hz, blok " + f.BlockAlign + ", bajtow " + f.Length);
                }
            }
        }

        // Sound Bank: cue "Music_N" -> numer utworu w Wave Banku (kolejnosc nie jest prosta).
        private void ReadXsb(string file)
        {
            byte[] d = File.ReadAllBytes(file);
            int p = 4 + 6 + 8 + 1;
            int nSimple = BitConverter.ToUInt16(d, p);
            int nTotal = BitConverter.ToUInt16(d, p + 6);
            p += 8 + 1 + 6;
            uint simpleOff = BitConverter.ToUInt32(d, p);
            uint namesOff = BitConverter.ToUInt32(d, p + 8);
            List<string> names = new List<string>();
            int q = (int)namesOff;
            for (int i = 0; i < nTotal && q < d.Length; i++)
            {
                int e = q;
                while (e < d.Length && d[e] != 0) e++;
                names.Add(System.Text.Encoding.ASCII.GetString(d, q, e - q));
                q = e + 1;
            }
            for (int i = 0; i < nSimple && i < names.Count; i++)
            {
                int o = (int)simpleOff + i * 5;
                int soundOff = (int)BitConverter.ToUInt32(d, o + 1);
                if ((d[soundOff] & 1) != 0) continue; // dzwiek zlozony - pomijamy
                int track = BitConverter.ToUInt16(d, soundOff + 9);
                string n = names[i];
                int id;
                if (n.StartsWith("Music_") && int.TryParse(n.Substring(6), out id)) cueToWave[id] = track;
            }
        }

        public AudioClip CreateClip(int musicId)
        {
            int wave;
            if (!cueToWave.TryGetValue(musicId, out wave)) wave = musicId - 1;
            if (wave < 0 || wave >= entries.Count) throw new Exception("brak utworu");
            Entry e = entries[wave];
            if (e.Format != 0 && e.Format != 2) throw new Exception("nieobslugiwany format " + e.Format);
            WaveStream ws = new WaveStream(path, e.Format, e.Channels, e.BlockAlign, e.Bits, e.Offset, e.Length);
            return AudioClip.Create("Terraria_Music_" + musicId, ws.TotalFrames, e.Channels, e.Rate, true, ws.OnRead, ws.OnSetPosition);
        }

        // Strumien jednego utworu: dekoduje blok po bloku, gdy Unity prosi o kolejne probki.
        private class WaveStream
        {
            private static readonly int[] AdaptTable = { 230, 230, 230, 230, 307, 409, 512, 614, 768, 614, 512, 409, 307, 230, 230, 230 };
            private static readonly int[] Coef1 = { 256, 512, 0, 192, 240, 460, 392 };
            private static readonly int[] Coef2 = { 0, -256, 0, 64, 0, -208, -232 };

            private readonly FileStream file;
            private readonly int format, channels, blockAlign, bits;
            private readonly long offset;
            private readonly int length;
            private readonly int framesPerBlock;
            public readonly int TotalFrames;

            private readonly byte[] raw;
            private readonly short[] decoded;   // przeplot kanalow
            private int decodedFrames;
            private int readFrame;              // pozycja w zdekodowanym bloku
            private int blockIndex;
            private readonly object sync = new object();

            public WaveStream(string path, int format, int channels, int blockAlign, int bits, long offset, int length)
            {
                this.format = format;
                this.channels = Math.Max(1, channels);
                this.blockAlign = blockAlign;
                this.bits = bits;
                this.offset = offset;
                this.length = length;
                file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (format == 2)
                {
                    framesPerBlock = (blockAlign - 7 * this.channels) * 2 / this.channels + 2;
                    TotalFrames = (length / blockAlign) * framesPerBlock;
                    raw = new byte[blockAlign];
                }
                else
                {
                    framesPerBlock = 4096;
                    int bytesPerFrame = this.channels * (bits / 8);
                    TotalFrames = length / bytesPerFrame;
                    raw = new byte[framesPerBlock * bytesPerFrame];
                }
                decoded = new short[framesPerBlock * this.channels];
                blockIndex = 0;
                readFrame = 0;
                decodedFrames = 0;
            }

            public void OnSetPosition(int frame)
            {
                lock (sync)
                {
                    if (TotalFrames <= 0) return;
                    frame %= TotalFrames;
                    blockIndex = frame / framesPerBlock;
                    decodedFrames = 0;
                    DecodeBlock();
                    readFrame = Math.Min(decodedFrames, frame % framesPerBlock);
                }
            }

            public void OnRead(float[] data)
            {
                lock (sync)
                {
                    int i = 0;
                    while (i < data.Length)
                    {
                        if (readFrame >= decodedFrames)
                        {
                            if (decodedFrames > 0) blockIndex++;
                            if ((long)blockIndex * framesPerBlock >= TotalFrames) blockIndex = 0; // petla
                            DecodeBlock();
                            readFrame = 0;
                            if (decodedFrames == 0)
                            {
                                while (i < data.Length) data[i++] = 0f;
                                return;
                            }
                        }
                        int idx = readFrame * channels;
                        for (int c = 0; c < channels && i < data.Length; c++) data[i++] = decoded[idx + c] / 32768f;
                        readFrame++;
                    }
                }
            }

            private void DecodeBlock()
            {
                decodedFrames = 0;
                int blockBytes = format == 2 ? blockAlign : raw.Length;
                long pos = offset + (long)blockIndex * blockBytes;
                long end = offset + length;
                if (pos >= end) return;
                int n = (int)Math.Min(blockBytes, end - pos);
                file.Position = pos;
                int got = 0;
                while (got < n)
                {
                    int r = file.Read(raw, got, n - got);
                    if (r <= 0) break;
                    got += r;
                }
                if (format == 2)
                {
                    if (got < 7 * channels) return;
                    DecodeAdpcm(got);
                }
                else
                {
                    int bpf = channels * (bits / 8);
                    int frames = got / bpf;
                    for (int f = 0; f < frames; f++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            int o = f * bpf + c * (bits / 8);
                            decoded[f * channels + c] = bits == 16 ? BitConverter.ToInt16(raw, o) : (short)((raw[o] - 128) << 8);
                        }
                    }
                    decodedFrames = frames;
                }
            }

            private void DecodeAdpcm(int got)
            {
                int ch = channels;
                int[] pred = new int[ch], delta = new int[ch], s1 = new int[ch], s2 = new int[ch];
                int p = 0;
                for (int c = 0; c < ch; c++) pred[c] = Math.Min(6, (int)raw[p++]);
                for (int c = 0; c < ch; c++) { delta[c] = BitConverter.ToInt16(raw, p); p += 2; }
                for (int c = 0; c < ch; c++) { s1[c] = BitConverter.ToInt16(raw, p); p += 2; }
                for (int c = 0; c < ch; c++) { s2[c] = BitConverter.ToInt16(raw, p); p += 2; }
                for (int c = 0; c < ch; c++)
                {
                    decoded[c] = (short)s2[c];
                    decoded[ch + c] = (short)s1[c];
                }
                int frame = 2;
                int nib = 0;
                while (p < got && frame < framesPerBlock)
                {
                    int b = raw[p++];
                    for (int h = 0; h < 2; h++)
                    {
                        int c = nib % ch;
                        int code = h == 0 ? (b >> 4) & 0xF : b & 0xF;
                        int signed = code >= 8 ? code - 16 : code;
                        int predicted = (s1[c] * Coef1[pred[c]] + s2[c] * Coef2[pred[c]]) >> 8;
                        int sample = predicted + signed * delta[c];
                        if (sample > 32767) sample = 32767;
                        if (sample < -32768) sample = -32768;
                        s2[c] = s1[c];
                        s1[c] = sample;
                        delta[c] = (AdaptTable[code] * delta[c]) >> 8;
                        if (delta[c] < 16) delta[c] = 16;
                        decoded[frame * ch + c] = (short)sample;
                        nib++;
                        if (nib % ch == 0) frame++;
                        if (frame >= framesPerBlock) break;
                    }
                }
                decodedFrames = frame;
            }
        }
    }
}
