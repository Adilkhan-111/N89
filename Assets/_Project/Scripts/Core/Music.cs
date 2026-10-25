using UnityEngine;
using UnityEngine.SceneManagement;

namespace N89.Core
{
    // Музыка: трек из Resources/Music/<сцена в нижнем регистре>.mp3 (tutorial, mission01–03); главное меню и все интерлюдии — interlude.mp3.
    // Зацикливается с плавным переходом: за _crossfade секунд до конца запускается та же дорожка
    // с нуля на втором источнике, старая гаснет. Рестарт после смерти музыку не перезапускает;
    // в сцене без трека музыка затухает. Объект создаётся сам, на сцены ничего вешать не нужно.
    public class Music : MonoBehaviour
    {
        private const float Crossfade = 3f;
        private const float Volume = 0.6f;

        private static Music s_instance;
        private readonly AudioSource[] _sources = new AudioSource[2];
        private int _current;
        private AudioClip _clip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (s_instance != null) return;
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<Music>();
            SceneManager.sceneLoaded += (scene, _) => s_instance.OnScene(scene.name);
            s_instance.OnScene(SceneManager.GetActiveScene().name);
        }

        private void Awake()
        {
            for (int i = 0; i < 2; i++)
            {
                _sources[i] = gameObject.AddComponent<AudioSource>();
                _sources[i].playOnAwake = false;
                _sources[i].ignoreListenerPause = true;
            }
        }

        private void OnScene(string scene)
        {
            // Все интерлюдии — один трек (interlude.mp3): между ними музыка не перезапускается
            string track = scene.ToLowerInvariant();
            if (track.StartsWith("interlude") || track == "mainmenu") track = "interlude";
            AudioClip clip = Resources.Load<AudioClip>("Music/" + track);
            if (clip == _clip) return;
            _clip = clip;
            if (clip == null)
            {
                return;
            }
            foreach (AudioSource s in _sources) s.Stop();
            PlayFromStart(0);
        }

        private void PlayFromStart(int index)
        {
            _current = index;
            AudioSource s = _sources[index];
            s.clip = _clip;
            s.volume = 0f;
            s.time = 0f;
            s.Play();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_clip == null)
            {
                foreach (AudioSource s in _sources) s.volume = Mathf.MoveTowards(s.volume, 0f, dt / 1.5f);
                return;
            }

            AudioSource cur = _sources[_current];
            AudioSource other = _sources[1 - _current];
            float volume = Volume * SoundSettings.Music;
            cur.volume = Mathf.MoveTowards(cur.volume, volume, Volume * dt / Crossfade);
            other.volume = Mathf.MoveTowards(other.volume, 0f, Volume * dt / Crossfade);
            if (other.volume <= 0f && other.isPlaying) other.Stop();

            // Конец трека близко — запускаем его заново на втором источнике, текущий уйдёт в затухание
            if (cur.isPlaying && _clip.length > Crossfade * 2f && cur.time >= _clip.length - Crossfade) PlayFromStart(1 - _current);
        }
    }
}
