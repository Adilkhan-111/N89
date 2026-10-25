using System.Collections.Generic;
using UnityEngine;

namespace N89.Core
{
    // Временные звуковые эффекты без ассетов: клипы синтезируются кодом при первом обращении.
    // Sfx.Play(Sfx.Shot, позиция) — звук в мире: громкость падает с расстоянием от камеры, в замедлении (скан,
    // E.N.A) звук ниже. Sfx.Ui(Sfx.UiMove) — интерфейс: без расстояния и слышен в паузе.
    // У каждого звука до трёх вариантов (свой случайный шум) — подряд они не звучат одинаково.
    // Настоящие звуки — заменить синтез в Make() на AudioClip из ресурсов с тем же id.
    public static class Sfx
    {
        public const string
            // Оружие
            Shot = "shot", ShotSmg = "shot_smg", ShotRifle = "shot_rifle", ShotShotgun = "shot_shotgun", DryFire = "dry_fire",
            Impact = "impact", Reload = "reload", ReloadOk = "reload_ok", ReloadFail = "reload_fail",
            // Взлом и импланты
            ScanOn = "scan_on", ScanOff = "scan_off", Hack = "hack", HackDone = "hack_done", HackFail = "hack_fail",
            Dash = "dash", Punch = "punch", Block = "block", Swing = "swing", Slash = "slash", Deflect = "deflect",
            SandevistanOn = "sandevistan_on", SandevistanOff = "sandevistan_off",
            // Тела, мир
            Hit = "hit", Death = "death", BodyFall = "body_fall", Explosion = "explosion", Glass = "glass", Door = "door",
            Alert = "alert", Alarm = "alarm", Pickup = "pickup", Clang = "clang",
            CarStart = "car_start", CarFly = "car_fly", Elevator = "elevator", ElevatorDing = "elevator_ding",
            // Бой с Боссом
            LampFlicker = "lamp_flicker", LampOn = "lamp_on", Rumble = "rumble", Crash = "crash",
            RocketLaunch = "rocket_launch", MinigunSpin = "minigun_spin",
            // Текст и интерфейс
            Type = "type", Decode = "decode", MsgIn = "msg_in", MsgOut = "msg_out", Vibrate = "vibrate",
            UiMove = "ui_move", UiSelect = "ui_select", UiDeny = "ui_deny", Pause = "pause",
            Banner = "banner", Objective = "objective", Stinger = "stinger",
            ScoreTick = "score_tick", RankStamp = "rank_stamp", Combo = "combo",
            // Финал на свалке
            Heartbeat = "heartbeat", Crawl = "crawl";

        private const int Rate = 44100;
        private const int Voices = 16;
        private const int UiVoices = 4;
        private const float Hearing = 18f;

        private static readonly Dictionary<string, AudioClip> s_clips = new();
        private static readonly Dictionary<string, float> s_lastPlayed = new();
        private static AudioSource[] s_sources;
        private static AudioSource[] s_ui;
        private static int s_next;
        private static int s_nextUi;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Перезагрузка домена выключена: клипы прошлого запуска Play Mode уже уничтожены
            s_sources = null;
            s_ui = null;
            s_clips.Clear();
            s_lastPlayed.Clear();
        }

        public static void Play(string id, Vector2? at = null, float volume = 1f, float pitch = 1f)
        {
            float v = volume * SoundSettings.Sfx;
            Camera cam = Camera.main;
            if (at.HasValue && cam != null)
            {
                float d = Vector2.Distance(at.Value, cam.transform.position);
                v *= Mathf.Clamp01(1f - d / Hearing);
            }
            // В замедлении мир звучит ниже и глуше
            float scale = Time.timeScale;
            if (scale > 0f && scale < 1f) pitch *= Mathf.Lerp(0.6f, 1f, scale);
            Emit(false, id, v, pitch);
        }

        // Интерфейс и титры: без затухания по расстоянию, слышно и в паузе
        public static void Ui(string id, float volume = 1f, float pitch = 1f) => Emit(true, id, volume * SoundSettings.Sfx, pitch);

        // Печать текста по буквам: тихий щелчок на новую букву (пробелы молчат)
        public static void Typing(char c, float volume = 1f)
        {
            if (!char.IsWhiteSpace(c)) Ui(Type, 0.5f * volume);
        }

        private static void Emit(bool ui, string id, float volume, float pitch)
        {
            if (volume <= 0.01f) return;
            // Один и тот же звук не чаще раза в MinGap: дробовик, залп, очередь не складываются в рёв
            float now = Time.unscaledTime;
            if (s_lastPlayed.TryGetValue(id, out float last) && now - last < MinGap(id) && now >= last) return;
            s_lastPlayed[id] = now;

            if (s_sources == null || s_sources[0] == null) CreateSources();
            AudioSource src;
            if (ui)
            {
                src = s_ui[s_nextUi];
                s_nextUi = (s_nextUi + 1) % UiVoices;
            }
            else
            {
                src = s_sources[s_next];
                s_next = (s_next + 1) % Voices;
            }
            float jitter = Jitter(id);
            src.pitch = pitch * (1f + Random.Range(-jitter, jitter));
            src.PlayOneShot(Clip(id), volume);
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
            s_ui = new AudioSource[UiVoices];
            for (int i = 0; i < UiVoices; i++)
            {
                s_ui[i] = go.AddComponent<AudioSource>();
                s_ui[i].playOnAwake = false;
                s_ui[i].ignoreListenerPause = true;
            }
        }

        private static AudioClip Clip(string id)
        {
            string key = $"{id}#{Random.Range(0, Variants(id))}";
            if (s_clips.TryGetValue(key, out AudioClip clip) && clip != null) return clip;
            float[] data = Make(id);
            Finish(data, Level(id));
            clip = AudioClip.Create(key, data.Length, 1, Rate, false);
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            clip.SetData(data, 0);
            s_clips[key] = clip;
            return clip;
        }

        // ------------------------------------------------------------ Настройки звуков

        private static int Variants(string id) => id switch
        {
            Elevator or CarStart or CarFly or Stinger or Rumble or MinigunSpin or Banner or Alarm or Vibrate
                or MsgIn or UiSelect or UiDeny or Pause or Objective or ElevatorDing or SandevistanOn or SandevistanOff => 1,
            _ => 3,
        };

        // Пиковая громкость клипа после нормализации: тихие щелчки интерфейса — тише выстрелов
        private static float Level(string id) => id switch
        {
            Type or ScoreTick => 0.3f,
            Decode or UiMove or Crawl => 0.4f,
            Impact or DryFire or Objective or Combo or MsgOut => 0.5f,
            Heartbeat or Alert or LampFlicker or Pickup or MsgIn or Vibrate or UiDeny or Pause => 0.6f,
            Door or Swing or Dash or Reload or ReloadOk or ReloadFail => 0.7f,
            _ => 0.85f,
        };

        // Разброс высоты при каждом проигрывании
        private static float Jitter(string id) => id switch
        {
            Type => 0.12f,
            UiMove or UiSelect or UiDeny or Pause or Objective or MsgIn or MsgOut or Banner or Stinger
                or ScoreTick or Combo or ElevatorDing or Alarm or Heartbeat => 0f,
            Impact or Glass or Hit => 0.1f,
            _ => 0.05f,
        };

        private static float MinGap(string id) => id switch
        {
            Type => 0.035f,
            ScoreTick => 0.04f,
            Shot or ShotRifle or Impact or Hit => 0.03f,
            ShotSmg => 0.02f,
            Alert => 0.25f,
            LampFlicker => 0.05f,
            Vibrate or Banner or Stinger => 0.3f,
            _ => 0.02f,
        };

        // ------------------------------------------------------------ Синтез

        private static float[] Make(string id) => id switch
        {
            // Оружие: щелчок капсюля (высокий шум) + «тело» (фильтрованный шум) + басовый удар + отражения от стен
            Shot => Room(Mix(Crack(1f, 2000f), Filter(Noise(0.2f, 22f, 1f), F.Low, 2200f, 0.9f),
                Sweep(0.16f, 150f, 50f, 22f, 0.9f)), 0.6f, 0.18f),
            ShotSmg => Room(Mix(Crack(0.9f, 2600f), Filter(Noise(0.1f, 42f, 0.9f), F.Low, 3200f, 0.8f),
                Sweep(0.08f, 190f, 75f, 40f, 0.6f)), 0.5f, 0.12f),
            ShotRifle => Room(Mix(Crack(1.2f, 1800f), Filter(Noise(0.24f, 19f, 1f), F.Low, 2600f, 1.1f),
                Sweep(0.18f, 130f, 45f, 18f, 1f), Filter(Noise(0.06f, 50f, 0.4f), F.Band, 4500f, 1.5f)), 0.7f, 0.2f),
            ShotShotgun => Room(Mix(Crack(1.3f, 1200f), Filter(Noise(0.45f, 9f, 1.2f), F.Low, 1200f, 0.9f),
                Sweep(0.3f, 120f, 36f, 10f, 1.2f), Crackle(0.15f, 300f, 15f, 0.4f)), 0.9f, 0.25f),
            DryFire => Mix(Clack(0f, 2600f), Clack(0.035f, 1700f)),
            Impact => Mix(Filter(Noise(0.05f, 70f, 0.8f), F.Band, 2500f, 1.2f), Filter(Noise(0.12f, 30f, 0.5f), F.Low, 600f),
                Random.value < 0.35f ? Sweep(0.16f, Random.Range(2600f, 3600f), 1500f, 14f, 0.18f) : Crackle(0.06f, 300f, 40f, 0.3f)),
            // Магазин вынут — вставлен; «ок» — затвор с металлическим звоном; промах — заело
            Reload => Mix(Clack(0f, 1500f), Clack(0.11f, 1050f)),
            ReloadOk => Mix(Clack(0f, 1300f), Clack(0.07f, 2200f), Fm(0.25f, 1760f, 2f, 1.5f, 18f, 0.2f, 0.08f)),
            ReloadFail => Mix(Clack(0f, 700f), Sweep(0.2f, 240f, 110f, 10f, 0.35f, square: true), Filter(Noise(0.15f, 15f, 0.4f), F.Low, 500f)),

            // Взлом — электроника, а не чиптюн: помехи, гул трансформатора, треск данных, разряды
            ScanOn => Room(Mix(Glitch(0.18f, 0.35f), Buzz(0.4f, 60f, 0.25f, 5f), Sweep(0.35f, 300f, 1200f, 6f, 0.1f),
                Filter(Noise(0.4f, 6f, 0.3f), F.Low, 400f)), 0.6f, 0.2f),
            ScanOff => Mix(Glitch(0.12f, 0.3f), Buzz(0.25f, 60f, 0.2f, 12f), Sweep(0.2f, 1100f, 300f, 12f, 0.08f)),
            Hack => Mix(Glitch(0.45f, 0.45f), Buzz(0.5f, 120f, 0.18f, 3f), Sweep(0.5f, 200f, 900f, 3f, 0.08f)),
            HackDone => Room(Mix(Filter(Noise(0.04f, 80f, 0.9f), F.High, 1500f), Glitch(0.2f, 0.4f, 0.04f),
                Buzz(0.35f, 50f, 0.35f, 8f, 0.04f), Crackle(0.3f, 150f, 8f, 0.3f)), 0.6f, 0.25f),
            HackFail => Mix(Glitch(0.3f, 0.5f), Buzz(0.35f, 45f, 0.45f, 6f)),
            // Импланты
            Dash => Env(Filter(Noise(0.35f, 0f, 1f), F.Band, 900f, 0.7f), 0.05f, 8f),
            Punch => Room(Mix(Filter(Noise(0.12f, 30f, 1.1f), F.Low, 700f, 1.1f), Sweep(0.2f, 110f, 38f, 14f, 1.1f),
                Crack(0.7f, 1500f)), 0.5f, 0.15f),
            Block => Room(Mix(Fm(0.4f, 520f, 1.41f, 4f, 9f, 0.4f), Fm(0.3f, 780f, 2.1f, 2f, 12f, 0.2f),
                Filter(Noise(0.04f, 80f, 0.7f), F.High, 2000f)), 0.5f, 0.2f),
            Swing => Env(Filter(Noise(0.22f, 0f, 1f), F.Band, 1400f, 0.9f), 0.06f, 14f),
            Slash => Mix(Env(Filter(Noise(0.14f, 0f, 1f), F.High, 3000f), 0.01f, 25f), Sweep(0.12f, 2600f, 700f, 18f, 0.25f),
                Filter(Noise(0.06f, 50f, 0.6f), F.Low, 800f)),
            Deflect => Room(Mix(Fm(0.45f, 1800f, 1.41f, 3f, 8f, 0.35f), Fm(0.4f, 2700f, 1.5f, 2f, 10f, 0.2f),
                Filter(Noise(0.03f, 120f, 0.6f), F.High, 3000f), Sweep(0.25f, 3000f, 1600f, 10f, 0.12f)), 0.5f, 0.2f),
            // E.N.A: гулкий спад вниз (мир тормозит) и подъём обратно
            SandevistanOn => Room(Mix(Sweep(0.6f, 900f, 90f, 3f, 0.35f), Filter(Noise(0.5f, 5f, 0.4f), F.Low, 500f),
                Glitch(0.15f, 0.25f), Fm(0.8f, 220f, 1.5f, 3f, 3f, 0.15f)), 1.2f, 0.35f),
            SandevistanOff => Room(Mix(Sweep(0.4f, 90f, 700f, 5f, 0.3f), Glitch(0.12f, 0.25f)), 0.8f, 0.2f),

            // Тела
            Hit => Mix(Filter(Noise(0.09f, 40f, 1f), F.Low, 900f, 1.2f), Sweep(0.1f, 140f, 55f, 28f, 0.8f),
                Filter(Noise(0.04f, 90f, 0.5f), F.Band, 2500f, 1.5f)),
            Death => Mix(Filter(Noise(0.3f, 10f, 0.8f), F.Low, 700f), Sweep(0.35f, 120f, 40f, 8f, 0.8f), Crackle(0.2f, 200f, 12f, 0.3f)),
            BodyFall => Mix(Filter(Noise(0.25f, 14f, 1.1f), F.Low, 350f, 1.2f), Sweep(0.25f, 80f, 35f, 12f, 0.9f),
                Filter(Noise(0.2f, 20f, 0.4f, delay: 0.08f), F.Low, 800f)),
            // Мир
            Explosion => Room(Mix(Crack(1.2f, 800f), Filter(Noise(1.4f, 3f, 1.4f), F.Low, 450f, 0.9f),
                Sweep(1f, 80f, 25f, 4f, 1.1f), Crackle(1f, 120f, 3f, 0.35f)), 1.3f, 0.3f),
            Glass => GlassBreak(),
            // Автодверь: пневматика шипит, сервопривод гудит, створки стукаются
            Door => Mix(Env(Filter(Noise(0.45f, 0f, 0.7f), F.Band, 1300f, 0.8f), 0.04f, 6f), Sweep(0.4f, 95f, 75f, 4f, 0.2f),
                Clack(0.36f, 600f)),
            // Охрана заметила: короткий щелчок рации
            Alert => Mix(Filter(Glitch(0.07f, 0.5f), F.Band, 2000f, 1f), Sweep(0.06f, 900f, 1300f, 8f, 0.25f, square: true, delay: 0.05f),
                Sweep(0.08f, 1300f, 1300f, 8f, 0.25f, square: true, delay: 0.13f)),
            // Тревога камеры: двухтональная сирена
            Alarm => Room(Mix(Sweep(0.45f, 700f, 1100f, 0.5f, 0.35f, square: true), Sweep(0.45f, 1100f, 700f, 0.5f, 0.35f, square: true, delay: 0.45f)), 0.8f, 0.2f),
            Pickup => Mix(Clack(0f, 1600f), Fm(0.25f, 1320f, 2f, 1f, 14f, 0.25f, 0.04f), Fm(0.3f, 1980f, 2f, 1f, 12f, 0.2f, 0.1f)),
            // Металл о бетон: газовый баллон упал
            Clang => Room(Mix(Fm(0.8f, 240f, 2.76f, 6f, 5f, 0.5f), Fm(0.6f, 520f, 1.41f, 3f, 7f, 0.25f),
                Filter(Noise(0.05f, 60f, 0.8f), F.Band, 2000f, 1f)), 0.8f, 0.25f),
            CarStart => Mix(Buzz(1.4f, 70f, 0.3f, 0.6f), Sweep(1.4f, 300f, 1600f, 1.2f, 0.12f), Filter(Noise(1.4f, 1.5f, 0.6f), F.Low, 600f)),
            CarFly => Room(Mix(Filter(Noise(2f, 1.4f, 1f), F.Low, 900f, 0.8f), Sweep(2f, 900f, 300f, 1.5f, 0.15f)), 1f, 0.3f),
            // Лифт: двери, гул мотора с подвыванием, стук тросов, в конце — двери
            Elevator => Mix(Buzz(2f, 55f, 0.35f, 0.3f), Env(Filter(Noise(2f, 0f, 0.5f), F.Low, 300f), 0.3f, 0.6f),
                Sweep(2f, 180f, 260f, 0.6f, 0.08f), Clack(0f, 500f), Clack(0.9f, 350f), Clack(1.8f, 500f)),
            ElevatorDing => Room(Mix(Fm(1.2f, 1318f, 3.5f, 1.2f, 3.5f, 0.4f), Fm(1.2f, 1046f, 3.5f, 1.2f, 3.5f, 0.35f, 0.28f)), 0.8f, 0.25f),

            // Подвал Босса: лампа ловит напряжение (треск) и загорается с гулким ударом
            LampFlicker => Mix(Filter(Glitch(0.08f, 0.8f), F.High, 1500f), Buzz(0.1f, 120f, 0.4f, 20f), Crackle(0.08f, 400f, 20f, 0.4f)),
            LampOn => Room(Mix(Filter(Noise(0.05f, 60f, 1.1f), F.Low, 900f), Sweep(0.3f, 90f, 40f, 10f, 1f),
                Buzz(1.4f, 100f, 0.3f, 1.2f, 0.05f), Fm(0.4f, 220f, 1.41f, 2f, 8f, 0.2f)), 1.2f, 0.3f),
            // Потолок трещит перед обрушением; удар обломков
            Rumble => Mix(Env(Filter(Noise(3.5f, 0f, 1.2f), F.Low, 160f, 1f), 1.5f, 0.3f), Crackle(3.5f, 25f, 0.2f, 0.25f),
                Fm(3.5f, 45f, 1.01f, 2f, 0.3f, 0.2f)),
            Crash => Room(Mix(Filter(Noise(1f, 4f, 1.3f), F.Low, 700f), Sweep(0.6f, 70f, 28f, 5f, 1.1f), Crackle(0.9f, 90f, 4f, 0.6f),
                Crack(1f, 1000f)), 1.2f, 0.3f),
            RocketLaunch => Mix(Crack(0.9f, 1000f), Env(Filter(Noise(0.9f, 0f, 1f), F.Band, 700f, 0.6f), 0.02f, 3f), Sweep(0.6f, 200f, 60f, 5f, 0.5f)),
            MinigunSpin => Mix(Sweep(0.8f, 80f, 700f, 0f, 0.2f, square: true), Env(Filter(Noise(0.8f, 0f, 0.6f), F.Band, 1500f, 1f), 0.6f, 0f)),

            // Текст и интерфейс
            Type => Mix(Filter(Noise(0.012f, 300f, 1f), F.Band, 3500f, 2f), Tone(0.02f, Random.Range(1700f, 2300f), 150f, 0.25f)),
            Decode => Filter(Glitch(0.22f, 0.5f), F.Band, 2500f, 0.8f),
            MsgIn => Mix(Fm(0.25f, 1568f, 2f, 1f, 14f, 0.35f), Fm(0.3f, 2093f, 2f, 1f, 12f, 0.3f, 0.09f)),
            MsgOut => Mix(Env(Filter(Noise(0.16f, 0f, 0.6f), F.Band, 2500f, 1f), 0.02f, 20f), Sweep(0.12f, 900f, 1700f, 15f, 0.2f)),
            Vibrate => Mix(Pulse(Buzz(0.28f, 150f, 0.9f, 0f), 0.02f), Pulse(Buzz(0.28f, 150f, 0.9f, 0f, 0.42f), 0.02f)),
            UiMove => Mix(Tone(0.04f, 2200f, 80f, 0.3f), Filter(Noise(0.015f, 250f, 0.6f), F.High, 3000f)),
            UiSelect => Room(Mix(Tone(0.12f, 1320f, 20f, 0.35f), Tone(0.15f, 1980f, 18f, 0.3f, 0.05f), Filter(Glitch(0.06f, 0.4f), F.High, 2000f)), 0.5f, 0.2f),
            UiDeny => Mix(Sweep(0.18f, 300f, 180f, 10f, 0.4f, square: true), Sweep(0.18f, 310f, 190f, 10f, 0.3f, square: true)),
            Pause => Mix(Sweep(0.2f, 1200f, 400f, 12f, 0.25f), Filter(Noise(0.1f, 30f, 0.4f), F.Low, 800f)),
            // Баннер: втягивающийся шум и удар с помехами
            Banner => Room(Mix(Env(Filter(Noise(0.14f, 0f, 0.8f), F.Band, 1500f, 0.7f), 0.12f, 0f), Sweep(0.4f, 60f, 40f, 7f, 1f, delay: 0.12f),
                Filter(Noise(0.03f, 80f, 0.9f, delay: 0.12f), F.Low, 1200f), Filter(Glitch(0.15f, 0.3f, 0.12f), F.High, 2500f)), 1f, 0.3f),
            Objective => Mix(Tone(0.12f, 880f, 18f, 0.3f), Tone(0.18f, 1320f, 14f, 0.3f, 0.08f)),
            // Титр (смерть, концовки): низкий гулкий удар
            Stinger => Room(Mix(Sweep(1.6f, 55f, 40f, 2.2f, 1f), Filter(Noise(1.2f, 3f, 0.6f), F.Low, 300f), Buzz(1.5f, 41f, 0.3f, 2f),
                Fm(1.5f, 110f, 1.01f, 1.5f, 2f, 0.2f)), 1.5f, 0.4f),
            ScoreTick => Tone(0.025f, 2600f, 120f, 0.3f),
            RankStamp => Room(Mix(Filter(Noise(0.08f, 30f, 1.2f), F.Low, 900f), Sweep(0.25f, 120f, 40f, 12f, 1.1f), Fm(0.5f, 330f, 1.41f, 3f, 6f, 0.25f)), 1f, 0.3f),
            Combo => Mix(Tone(0.08f, 1046f, 30f, 0.3f), Tone(0.1f, 1568f, 25f, 0.25f, 0.05f)),

            // Финал: глухое сердцебиение и тело, которое волочится по асфальту
            Heartbeat => Mix(Filter(Sweep(0.15f, 60f, 40f, 20f, 1f), F.Low, 200f), Filter(Sweep(0.12f, 55f, 38f, 24f, 0.8f, delay: 0.22f), F.Low, 200f)),
            Crawl => Mix(Env(Filter(Noise(0.35f, 0f, 0.8f), F.Band, 700f, 0.8f), 0.08f, 6f), Filter(Noise(0.3f, 8f, 0.3f), F.High, 2500f)),
            _ => new float[1],
        };

        private static int N(float seconds) => Mathf.Max(1, (int)(seconds * Rate));

        // Белый шум с экспоненциальным затуханием (decay 0 — без затухания); lowpass < 1 — мягче
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

        // Резкий щелчок: несколько миллисекунд шума выше cutoff — атака выстрела, удара
        private static float[] Crack(float gain, float cutoff) => Filter(Noise(0.008f, 300f, gain), F.High, cutoff);

        // Металлический щелчок механизма (магазин, затвор, створки) на частоте freq
        private static float[] Clack(float delay, float freq) =>
            Mix(Filter(Noise(0.035f, 110f, 1f, delay: delay), F.Band, freq, 1.8f), Fm(0.05f, freq * 0.5f, 1.41f, 2f, 60f, 0.25f, delay));

        // FM-синтез: металл, колокольчики, звон. ratio — отношение модулятора к несущей, index — яркость (спадает)
        private static float[] Fm(float seconds, float carrier, float ratio, float index, float decay, float gain, float delay = 0f)
        {
            int off = N(delay);
            var d = new float[N(seconds) + off];
            for (int i = off; i < d.Length; i++)
            {
                float t = (i - off) / (float)Rate;
                float env = Mathf.Exp(-t * decay);
                float mod = Mathf.Sin(2f * Mathf.PI * carrier * ratio * t) * index * env;
                float attack = Mathf.Clamp01(t / 0.002f);
                d[i] = Mathf.Sin(2f * Mathf.PI * carrier * t + mod) * gain * env * attack;
            }
            return d;
        }

        // Редкие щелчки-трески (density — щелчков в секунду): искры, огонь, осыпающиеся обломки
        private static float[] Crackle(float seconds, float density, float decay, float gain)
        {
            var d = new float[N(seconds)];
            for (int i = 0; i < d.Length; i++)
            {
                if (Random.value * Rate >= density) continue;
                float amp = Random.Range(0.3f, 1f) * gain * Mathf.Exp(-(i / (float)Rate) * decay);
                int len = Random.Range(20, 120);
                for (int j = 0; j < len && i + j < d.Length; j++) d[i + j] += Random.Range(-1f, 1f) * amp * (1f - j / (float)len);
            }
            return d;
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

        // Стекло: хруст, звон осколков вразнобой (FM-колокольчики) и шорох
        private static float[] GlassBreak()
        {
            var parts = new List<float[]>
            {
                Crack(1f, 2500f),
                Filter(Noise(0.3f, 12f, 0.6f), F.High, 3000f),
                Filter(Noise(0.05f, 60f, 0.6f), F.Low, 1500f),
            };
            for (int i = 0; i < 8; i++)
            {
                parts.Add(Fm(0.18f, Random.Range(2800f, 6500f), 1.7f, 2f, 25f, 0.12f, Random.Range(0f, 0.3f)));
            }
            return Room(Mix(parts.ToArray()), 0.6f, 0.2f);
        }

        // ------------------------------------------------------------ Обработка

        private enum F { Low, High, Band }

        // Биквадратный фильтр (RBJ): срез/полоса около freq, q — резонанс (ширина полосы)
        private static float[] Filter(float[] x, F type, float freq, float q = 0.707f)
        {
            float w = 2f * Mathf.PI * Mathf.Min(freq, Rate * 0.45f) / Rate;
            float cos = Mathf.Cos(w), alpha = Mathf.Sin(w) / (2f * q);
            float b0, b1, b2;
            switch (type)
            {
                case F.Low: b0 = (1f - cos) / 2f; b1 = 1f - cos; b2 = b0; break;
                case F.High: b0 = (1f + cos) / 2f; b1 = -(1f + cos); b2 = b0; break;
                default: b0 = alpha; b1 = 0f; b2 = -alpha; break;
            }
            float a0 = 1f + alpha, a1 = -2f * cos, a2 = 1f - alpha;
            b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;

            var y = new float[x.Length];
            float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                float v = b0 * x[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x[i];
                y2 = y1; y1 = v;
                y[i] = v;
            }
            return y;
        }

        // Огибающая поверх звука: плавное нарастание за attack секунд, потом спад (decay — в секунду)
        private static float[] Env(float[] x, float attack, float decay)
        {
            for (int i = 0; i < x.Length; i++)
            {
                float t = i / (float)Rate;
                float a = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
                x[i] *= a * a * Mathf.Exp(-Mathf.Max(0f, t - attack) * decay);
            }
            return x;
        }

        // Пульс вибромотора: мягкие края, чтобы не щёлкало
        private static float[] Pulse(float[] x, float edge)
        {
            int e = N(edge);
            int start = 0;
            while (start < x.Length && x[start] == 0f) start++;
            for (int i = start; i < x.Length; i++)
            {
                float k = Mathf.Min(1f, Mathf.Min(i - start, x.Length - 1 - i) / (float)e);
                x[i] *= k;
            }
            return x;
        }

        // «Комната» (ревербератор Шрёдера): 4 гребёнчатых фильтра параллельно и 2 всепропускающих подряд.
        // size — размер помещения (длина хвоста), mix — доля отражений
        private static float[] Room(float[] dry, float size, float mix)
        {
            int tail = N(0.35f * size);
            var input = new float[dry.Length + tail];
            System.Array.Copy(dry, input, dry.Length);

            int[] combs = { 1557, 1617, 1491, 1422 };
            float feedback = Mathf.Clamp(0.7f + 0.1f * size, 0.6f, 0.88f);
            var wet = new float[input.Length];
            foreach (int baseDelay in combs)
            {
                int delay = Mathf.Max(1, (int)(baseDelay * Mathf.Lerp(0.6f, 1.2f, Mathf.Clamp01(size / 1.5f))));
                var buffer = new float[delay];
                int idx = 0;
                float damp = 0f;
                for (int i = 0; i < input.Length; i++)
                {
                    float o = buffer[idx];
                    damp += (o - damp) * 0.6f;
                    buffer[idx] = input[i] + damp * feedback;
                    idx = (idx + 1) % delay;
                    wet[i] += o * 0.25f;
                }
            }
            foreach (int delay in new[] { 225, 556 })
            {
                var buffer = new float[delay];
                int idx = 0;
                for (int i = 0; i < wet.Length; i++)
                {
                    float b = buffer[idx];
                    float o = -wet[i] + b;
                    buffer[idx] = wet[i] + b * 0.5f;
                    idx = (idx + 1) % delay;
                    wet[i] = o;
                }
            }
            for (int i = 0; i < input.Length; i++) input[i] = input[i] + wet[i] * mix;
            return input;
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

        // Нормализация пика до level и короткое затухание в конце — без щелчка обрыва
        private static void Finish(float[] d, float level)
        {
            float peak = 0f;
            foreach (float s in d) peak = Mathf.Max(peak, Mathf.Abs(s));
            float k = peak > 0.0001f ? level / peak : 0f;
            int fade = Mathf.Min(d.Length, N(0.005f));
            for (int i = 0; i < d.Length; i++)
            {
                float edge = i >= d.Length - fade ? (d.Length - 1 - i) / (float)fade : 1f;
                d[i] *= k * edge;
            }
        }
    }
}
