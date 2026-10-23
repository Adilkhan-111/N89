using System.Collections;
using System.Linq;
using N89.Combat;
using N89.Enemies;
using N89.Hacking;
using N89.Player;
using N89.UI;
using N89.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace N89.Tutorial
{
    // Сценарий обучения Номера 89 в лаборатории: голос оператора ведёт по шагам
    // (ходьба → стрельба → дроны → взлом), а в конце субъекта ликвидируют за попытку побега.
    public class TutorialDirector : MonoBehaviour
    {
        private const string Speaker = "ОПЕРАТОР";

        [Header("Player")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private HackingController _hacking;
        [SerializeField] private CameraFollow _cameraFollow;

        [Header("UI")]
        [SerializeField] private SubtitleBox _subtitles;
        [SerializeField] private ObjectivePanel _objective;
        [SerializeField] private HackingHud _hackingHud;
        [SerializeField] private ScreenFader _fader;

        [Header("Steps")]
        [SerializeField] private TriggerZone[] _moveMarkers;
        [SerializeField] private Health[] _targets;
        [SerializeField] private Health[] _drones;
        [SerializeField] private Door _hackDoor;
        [SerializeField] private Health _glassDrone;

        [Header("Ending")]
        [SerializeField] private Door _exitGate;
        [SerializeField] private TriggerZone _escapeZone;
        [SerializeField] private Turret[] _turrets;
        [SerializeField] private Light2D[] _alarmLights;
        [SerializeField] private Light2D _globalLight;
        [SerializeField] private float _forcedDeathDelay = 2.5f;
        [SerializeField] private string _nextScene = "MainMenu";

        private bool _ending;

        private IEnumerator Start()
        {
            _player.CanMove = false;
            _player.CanShoot = false;
            _hacking.HackingEnabled = false;
            _hackingHud.RamVisible = false;
            _objective.Set(string.Empty);

            foreach (TriggerZone marker in _moveMarkers) marker.gameObject.SetActive(false);
            foreach (Health drone in _drones) drone.gameObject.SetActive(false);
            foreach (Light2D light in _alarmLights) light.enabled = false;

            _player.Health.Died += OnPlayerDied;

            _fader.SetBlack(1f);
            yield return new WaitForSeconds(0.6f);
            yield return _fader.Fade(1f, 0f, 2f);

            yield return Say("Субъект 89. Пробуждение подтверждено.");
            yield return Say("Пульс в норме. Нейроимплант активен. Начинаем калибровку.");

            yield return MovementStep();
            yield return ShootingStep();
            yield return DronesStep();
            yield return HackingStep();
            yield return EndingStep();
        }

        private IEnumerator MovementStep()
        {
            _player.CanMove = true;
            SayAsync("Двигайся. Клавиши W, A, S, D. Встань на отметку.");

            for (int i = 0; i < _moveMarkers.Length; i++)
            {
                _objective.Set($"Встань на отметки  {i}/{_moveMarkers.Length}");
                TriggerZone marker = _moveMarkers[i];
                marker.gameObject.SetActive(true);
                yield return new WaitUntil(() => marker.WasEntered);
                marker.gameObject.SetActive(false);
            }

            _objective.Set(string.Empty);
            yield return Say("Моторика в норме.");
        }

        private IEnumerator ShootingStep()
        {
            yield return Say("Мышь — прицел. Левая кнопка мыши — выстрел.");
            _player.CanShoot = true;
            SayAsync("Уничтожь учебные мишени.");
            yield return WaitAllDead(_targets, "Уничтожь мишени");
            yield return Say("Точность приемлемая.");
        }

        private IEnumerator DronesStep()
        {
            foreach (Health drone in _drones) drone.gameObject.SetActive(true);
            yield return Say("Подвижные цели. Учебные дроны. Они не стреляют.");
            SayAsync("Пока что.");
            yield return WaitAllDead(_drones, "Уничтожь дронов");
            yield return Say("Хорошо. Ты учишься быстрее предыдущих образцов.");
        }

        private IEnumerator HackingStep()
        {
            _hacking.Ram.Refill();
            _hacking.HackingEnabled = true;
            _hackingHud.RamVisible = true;

            yield return Say("Твой имплант умеет больше, чем стрелять.");
            yield return Say("Зажми TAB — режим сканирования. Время замедлится, а уязвимая техника подсветится.");
            SayAsync("Наведи курсор на дверь справа и нажми 1, чтобы взломать замок.");
            _objective.Set("Взломай дверь:  [TAB] + курсор + [1]");
            yield return new WaitUntil(() => _hackDoor.IsOpen);

            _objective.Set(string.Empty);
            yield return Say("Каждый взлом расходует оперативную память. Шкала RAM — слева внизу.");
            yield return Say("Слева за стеклом — дрон. Пули стекло не пробивают.");
            SayAsync("Используй «Короткое замыкание».");
            _objective.Set("Уничтожь дрона за стеклом взломом");
            yield return new WaitUntil(() => _glassDrone == null || _glassDrone.IsDead);

            _objective.Set(string.Empty);
            yield return Say("Память восстанавливается сама. Не трать её впустую.");
        }

        private IEnumerator EndingStep()
        {
            yield return Say("Калибровка завершена. Субъект 89 признан пригодным.");
            _exitGate.Open();
            SayAsync("Технический шлюз открыт для обслуживания. Оставайся на месте.");

            // Ждём, пока 89 попробует сбежать. Если стоит на месте — голос напоминает о себе.
            float nagAt = Time.time + 14f;
            int nag = 0;
            string[] nags =
            {
                "Субъект 89. Вернись в капсулу.",
                "Субъект 89. Это не просьба.",
            };
            while (!_escapeZone.WasEntered)
            {
                if (Time.time > nagAt && nag < nags.Length)
                {
                    SayAsync(nags[nag++]);
                    nagAt = Time.time + 14f;
                }
                yield return null;
            }

            yield return EscapeSequence();
        }

        private IEnumerator EscapeSequence()
        {
            _ending = true;
            _hacking.HackingEnabled = false;
            _exitGate.Close();

            SayAsync("Попытка побега. Протокол ликвидации.");
            foreach (Light2D light in _alarmLights) light.enabled = true;
            if (_globalLight != null) _globalLight.color = new Color(1f, 0.35f, 0.4f);

            yield return new WaitForSeconds(0.6f);
            foreach (Turret turret in _turrets)
            {
                turret.Target = _player.transform;
                turret.Active = true;
            }

            // Сбежать нельзя: если турели промахнулись, всё равно добиваем
            float deadline = Time.time + _forcedDeathDelay;
            while (!_player.Health.IsDead && Time.time < deadline)
            {
                yield return null;
            }

            if (!_player.Health.IsDead)
            {
                _player.Health.Kill();
            }
        }

        private void OnPlayerDied(Health health)
        {
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            _objective.Set(string.Empty);
            _fader.Flash(new Color(1f, 0.1f, 0.2f), 0.6f);
            Time.timeScale = 0.2f;
            yield return new WaitForSecondsRealtime(1.2f);
            Time.timeScale = 1f;

            foreach (Turret turret in _turrets) turret.Active = false;
            yield return _fader.Fade(0f, 1f, 1.2f);
            yield return _fader.ShowTitle("СУБЪЕКТ 89 — ЛИКВИДИРОВАН", "Данные калибровки сохранены. Подготовить субъект 90.");
            yield return new WaitForSecondsRealtime(3f);

            SceneManager.LoadScene(_nextScene);
        }

        private void Update()
        {
            // Esc — выйти в меню (удобно при тестах)
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && !_ending)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(_nextScene);
            }
        }

        private IEnumerator WaitAllDead(Health[] group, string objective)
        {
            while (true)
            {
                int dead = group.Count(h => h == null || h.IsDead);
                _objective.Set($"{objective}  {dead}/{group.Length}");
                if (dead == group.Length)
                {
                    break;
                }
                yield return null;
            }

            _objective.Set(string.Empty);
        }

        private Coroutine Say(string line) => _subtitles.Say(Speaker, line);

        private void SayAsync(string line) => _subtitles.Say(Speaker, line);
    }
}
