using System.Collections.Generic;
using UnityEngine;

namespace N89.Core
{
    // Временные звуковые эффекты без ассетов: клипы синтезируются кодом при первом обращении
    // (шум, свипы, писк). Sfx.Play(Sfx.Shot, позиция) — громкость падает с расстоянием от камеры.
    // Настоящие звуки — заменить синтез в Make() на AudioClip из ресурсов с тем же id.
    public static class Sfx
    {
        public const string Shot = "shot", Reload = "reload", ReloadOk = "reload_ok", ReloadFail = "reload_fail",
            ScanOn = "scan_on", ScanOff = "scan_off", Hack = "hack", HackDone = "hack_done", HackFail = "hack_fail",
            Hit = "hit", Death = "death", Explosion = "explosion", Glass = "glass", Door = "door",
            Dash = "dash", Punch = "punch", Block = "block", Swing = "swing", Slash = "slash", Deflect = "deflect",
            CarStart = "car_start", CarFly = "car_fly", Elevator = "elevator",
            SandevistanOn = "sandevistan_on", SandevistanOff = "sandevistan_off";

        private const int Rate = 44100;
        private const int Voices = 16;
        private const float Hearing = 18f;

        private static readonly Dictionary<string, AudioClip> s_clips = new();
        private static AudioSource[] s_sources;
        private static int s_next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Перезагрузка домена выключена: клипы прошлого запуска Play Mode уже уничтожены
            s_sources = null;
            s_clips.Clear();
        }

        public static void Play(string id, Vector2? at = null, float volume = 1f)
        {
            float v = volume;
            Camera cam = Camera.main;
            if (at.HasValue && cam != null)
            {
                float d = Vector2.Distance(at.Value, cam.transform.position);
                v *= Mathf.Clamp01(1f - d / Hearing);
                if (v <= 0.01f) return;
            }
            if (s_sources == null || s_sources[0] == null) CreateSources();
            AudioSource src = s_sources[s_next];
            s_next = (s_next + 1) % Voices;
            src.pitch = Random.Range(0.93f, 1.07f);
            src.PlayOneShot(Clip(id), v);
        }

        private static void CreateSources()
        {
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            s_sources = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                s_sources[i] = go.AddComponent<AudioSource>();
                s_sources[i].playOnAwake = false;
            }
        }

        private static AudioClip Clip(string id)
        {
            if (s_clips.TryGetValue(id, out AudioClip clip) && clip != null) return clip;
            float[] data = Make(id);
            clip = AudioClip.Create(id, data.Length, 1, Rate, false);
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            clip.SetData(data, 0);
            s_clips[id] = clip;
            return clip;
        }

        // ------------------------------------------------------------ Синтез

        private static float[] Make(string id) => id switch
        {
            // Выстрел: резкий щелчок + плотное «тело» из фильтрованного шума + басовый удар + хвост эха
            Shot => Echo(Mix(Noise(0.03f, 90f, 0.8f), Noise(0.22f, 16f, 1.1f, lowpass: 0.25f),
                Sweep(0.18f, 140f, 45f, 18f, 0.9f)), 0.07f, 0.3f),
            Reload => Mix(Click(0f), Click(0.09f)),
            ReloadOk => Mix(Click(0f), Tone(0.1f, 1200f, 30f, 0.35f, 0.03f)),
            ReloadFail => Sweep(0.18f, 220f, 120f, 12f, 0.4f, square: true),
            // Взлом — электроника, а не чиптюн: помехи, гул трансформатора, треск данных, разряды
            ScanOn => Mix(Glitch(0.18f, 0.35f), Buzz(0.4f, 60f, 0.25f, 5f), Noise(0.4f, 6f, 0.15f, lowpass: 0.04f)),
            ScanOff => Mix(Glitch(0.12f, 0.3f), Buzz(0.25f, 60f, 0.2f, 12f)),
            Hack => Mix(Glitch(0.45f, 0.45f), Buzz(0.5f, 120f, 0.18f, 3f), Sweep(0.5f, 200f, 900f, 3f, 0.08f)),
            HackDone => Echo(Mix(Noise(0.04f, 80f, 0.9f), Glitch(0.2f, 0.4f, 0.04f), Buzz(0.35f, 50f, 0.35f, 8f, 0.04f)), 0.05f, 0.3f),
            HackFail => Mix(Glitch(0.3f, 0.5f), Buzz(0.35f, 45f, 0.45f, 6f)),
            CarStart => Mix(Buzz(1.4f, 70f, 0.3f, 0.6f), Sweep(1.4f, 300f, 1600f, 1.2f, 0.12f), Noise(1.4f, 1.5f, 0.35f, lowpass: 0.05f)),
            CarFly => Echo(Mix(Noise(2f, 1.4f, 0.9f, lowpass: 0.1f), Sweep(2f, 900f, 300f, 1.5f, 0.15f)), 0.15f, 0.3f),
            // Сандевистан: гулкий спад вниз (мир тормозит) и подъём обратно
            SandevistanOn => Echo(Mix(Sweep(0.6f, 900f, 90f, 3f, 0.35f), Noise(0.5f, 5f, 0.3f, lowpass: 0.05f), Glitch(0.15f, 0.25f)), 0.12f, 0.35f),
            SandevistanOff => Mix(Sweep(0.4f, 90f, 700f, 5f, 0.3f), Glitch(0.12f, 0.25f)),
            Elevator => Mix(Buzz(1.6f, 55f, 0.3f, 0.4f), Noise(1.6f, 0.8f, 0.2f, lowpass: 0.03f), Click(0f), Click(1.4f)),
            Explosion => Echo(Mix(Noise(0.05f, 60f, 0.9f), Noise(1.2f, 3.5f, 1.3f, lowpass: 0.06f),
                Sweep(0.9f, 70f, 25f, 4f, 1f)), 0.12f, 0.35f),
            Death => Mix(Noise(0.25f, 12f, 0.7f, lowpass: 0.12f), Sweep(0.3f, 110f, 40f, 9f, 0.8f)),
            Hit => Mix(Noise(0.08f, 35f, 0.7f, lowpass: 0.3f), Sweep(0.1f, 130f, 60f, 25f, 0.6f)),
            Punch => Echo(Mix(Noise(0.12f, 25f, 0.9f, lowpass: 0.2f), Sweep(0.18f, 100f, 38f, 16f, 1f)), 0.06f, 0.2f),
            Glass => Mix(Noise(0.25f, 12f, 0.5f), Arp(0.2f, new[] { 3200f, 2600f, 3800f, 2900f }, 0.15f)),
            Door => Mix(Noise(0.35f, 6f, 0.12f), Sweep(0.35f, 110f, 90f, 5f, 0.2f)),
            Dash => Mix(Noise(0.3f, 7f, 0.7f, lowpass: 0.08f), Noise(0.3f, 9f, 0.25f, lowpass: 0.5f)),
            Block => Mix(Tone(0.25f, 700f, 14f, 0.35f), Tone(0.25f, 1060f, 14f, 0.2f), Noise(0.05f, 50f, 0.4f)),
            Swing => Mix(Noise(0.18f, 12f, 0.6f, lowpass: 0.12f), Sweep(0.18f, 900f, 300f, 14f, 0.12f)),
            Slash => Mix(Noise(0.12f, 25f, 0.6f), Sweep(0.12f, 2400f, 600f, 20f, 0.25f)),
            Deflect => Mix(Tone(0.35f, 1800f, 10f, 0.35f), Tone(0.35f, 2700f, 12f, 0.2f), Noise(0.05f, 60f, 0.4f)),
            _ => new float[1],
        };

        private static int N(float seconds) => Mathf.Max(1, (int)(seconds * Rate));

        // Белый шум с экспоненциальным затуханием; lowpass < 1 — мягче (простой однополюсный фильтр)
        private static float[] Noise(float seconds, float decay, float gain, float lowpass = 1f, float delay = 0f)
        {
            int off = N(delay);
            var d = new float[N(seconds) + off];
            float prev = 0f;
            for (int i = off; i < d.Length; i++)
            {
                float t = (i - off) / (float)Rate;
                prev += (Random.Range(-1f, 1f) - prev) * lowpass;
                d[i] = prev * gain * Mathf.Exp(-t * decay);
            }
            return d;
        }

        private static float[] Tone(float seconds, float freq, float decay, float gain, float delay = 0f) =>
            Sweep(seconds, freq, freq, decay, gain, delay: delay);

        private static float[] Sweep(float seconds, float from, float to, float decay, float gain, bool square = false, float delay = 0f)
        {
            int off = N(delay);
            var d = new float[N(seconds) + off];
            float phase = 0f;
            for (int i = off; i < d.Length; i++)
            {
                float k = (i - off) / (float)N(seconds);
                phase += Mathf.Lerp(from, to, k) / Rate;
                float s = Mathf.Sin(phase * Mathf.PI * 2f);
                if (square) s = (s + 0.3f * Mathf.Sin(phase * Mathf.PI * 6f)) * 0.7f;
                float attack = Mathf.Clamp01((i - off) / (Rate * 0.004f));
                d[i] = s * gain * attack * Mathf.Exp(-k * seconds * decay);
            }
            return d;
        }

        private static float[] Click(float delay) => Mix(Noise(0.02f, 150f, 0.6f, delay: delay), Tone(0.03f, 2500f, 90f, 0.3f, delay));

        // Короткие ноты подряд — «цифровой» писк взлома
        private static float[] Arp(float seconds, float[] notes, float gain)
        {
            var parts = new float[notes.Length][];
            float step = seconds / notes.Length;
            for (int i = 0; i < notes.Length; i++) parts[i] = Sweep(step, notes[i], notes[i], 12f, gain, square: true, delay: step * i);
            return Mix(parts);
        }

        // Цифровые помехи: шум рубится на куски по 4–30 мс, каждый со своей «битностью» (sample-and-hold)
        // и громкостью, часть кусков — тишина. Звучит как сбоящая электроника и поток данных
        private static float[] Glitch(float seconds, float gain, float delay = 0f)
        {
            int off = N(delay);
            var d = new float[N(seconds) + off];
            int i = off;
            while (i < d.Length)
            {
                int len = N(Random.Range(0.004f, 0.03f));
                int hold = Random.Range(1, 24);
                float amp = Random.value < 0.3f ? 0f : Random.Range(0.3f, 1f);
                float tone = Random.value < 0.4f ? Random.Range(800f, 4000f) : 0f;
                float held = 0f;
                for (int j = 0; j < len && i < d.Length; j++, i++)
                {
                    if (j % hold == 0) held = Random.Range(-1f, 1f);
                    float s = tone > 0f ? Mathf.Sign(Mathf.Sin(i * tone * 2f * Mathf.PI / Rate)) * 0.5f + held * 0.3f : held;
                    float fade = 1f - (float)(i - off) / (d.Length - off);
                    d[i] = s * amp * gain * (0.4f + 0.6f * fade);
                }
            }
            return d;
        }

        // Электрический гул: основная частота с гармониками (как трансформатор) и дрожанием громкости
        private static float[] Buzz(float seconds, float freq, float gain, float decay, float delay = 0f)
        {
            int off = N(delay);
            var d = new float[N(seconds) + off];
            for (int i = off; i < d.Length; i++)
            {
                float t = (i - off) / (float)Rate;
                float w = 2f * Mathf.PI * freq * t;
                float s = Mathf.Sin(w) + 0.5f * Mathf.Sin(2f * w) + 0.35f * Mathf.Sin(3f * w) + 0.2f * Mathf.Sin(5f * w);
                float flutter = 0.75f + 0.25f * Mathf.Sin(t * 37f) + Random.Range(-0.1f, 0.1f);
                float attack = Mathf.Clamp01(t / 0.02f);
                d[i] = s * 0.5f * gain * flutter * attack * Mathf.Exp(-t * decay);
            }
            return d;
        }

        // Хвост из трёх затухающих повторов — звук «в помещении», а не сухой писк
        private static float[] Echo(float[] dry, float delay, float feedback)
        {
            int step = N(delay);
            var d = new float[dry.Length + step * 3];
            for (int k = 0; k < 4; k++)
            {
                float g = Mathf.Pow(feedback, k);
                for (int i = 0; i < dry.Length; i++) d[i + step * k] += dry[i] * g;
            }
            return d;
        }

        private static float[] Mix(params float[][] parts)
        {
            int len = 0;
            foreach (float[] p in parts) len = Mathf.Max(len, p.Length);
            var d = new float[len];
            foreach (float[] p in parts)
                for (int i = 0; i < p.Length; i++) d[i] += p[i];
            for (int i = 0; i < len; i++) d[i] = (float)System.Math.Tanh(d[i] * 1.2f);
            return d;
        }
    }
}
