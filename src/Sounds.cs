// Dzwieki z Terrarii (pliki res/S_*.wav wbudowane w DLL).
// Glosnosc wedlug ustawien gry (Master x Dzwieki).

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TerrariaArsenal
{
    public static class Sounds
    {
        private static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        private static AudioSource[] sources;
        private static int next;

        public static void Reset()
        {
            Cache.Clear();
        }

        public static void Play(string name, float volume)
        {
            AudioClip clip = Load(name);
            if (clip == null) return;
            if (sources == null)
            {
                GameObject go = new GameObject("TA_Sounds");
                Object.DontDestroyOnLoad(go);
                sources = new AudioSource[6];
                for (int i = 0; i < sources.Length; i++)
                {
                    sources[i] = go.AddComponent<AudioSource>();
                    sources[i].playOnAwake = false;
                    sources[i].spatialBlend = 0f;
                }
            }
            AudioSource src = sources[next];
            next = (next + 1) % sources.Length;
            src.pitch = Random.Range(0.92f, 1.08f); // Terraria tez lekko zmienia wysokosc
            src.volume = volume * GameVolume();
            src.clip = clip;
            src.Play();
        }

        // Losowy z kilku wariantow, np. Player_Hit_0..2.
        public static void PlayRandom(string prefix, int count, float volume)
        {
            Play(prefix + Random.Range(0, count), volume);
        }

        private static float GameVolume()
        {
            GameManager gm = GameManager.instance;
            if (gm == null || gm.gameSettings == null) return 0.6f;
            return Mathf.Clamp01(gm.gameSettings.masterVolume / 10f) * Mathf.Clamp01(gm.gameSettings.soundVolume / 10f);
        }

        private static AudioClip Load(string name)
        {
            AudioClip clip;
            if (Cache.TryGetValue(name, out clip)) return clip;
            Cache[name] = null;
            clip = TerrariaContent.Sound(name);
            if (clip != null)
            {
                Cache[name] = clip;
                return clip;
            }
            Stream s = typeof(Sounds).Assembly.GetManifestResourceStream("TerrariaArsenal.S_" + name + ".wav");
            if (s == null) return null;
            byte[] d = new byte[s.Length];
            s.Read(d, 0, d.Length);
            s.Close();

            // Prosty parser WAV: szukamy chunkow "fmt " i "data" (PCM 16 bit).
            int channels = 2, rate = 44100, dataPos = -1, dataLen = 0;
            int p = 12;
            while (p + 8 <= d.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(d, p, 4);
                int len = System.BitConverter.ToInt32(d, p + 4);
                if (id == "fmt ")
                {
                    channels = System.BitConverter.ToInt16(d, p + 10);
                    rate = System.BitConverter.ToInt32(d, p + 12);
                }
                else if (id == "data")
                {
                    dataPos = p + 8;
                    dataLen = Mathf.Min(len, d.Length - dataPos);
                    break;
                }
                p += 8 + len + (len & 1);
            }
            if (dataPos < 0) return null;

            int samples = dataLen / 2;
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                data[i] = System.BitConverter.ToInt16(d, dataPos + i * 2) / 32768f;
            }
            clip = AudioClip.Create("TA_" + name, samples / channels, channels, rate, false);
            clip.SetData(data, 0);
            Cache[name] = clip;
            return clip;
        }
    }
}
