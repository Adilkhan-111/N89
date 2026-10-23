using System.Collections;
using System.Linq;
using N89.Combat;
using N89.Core;
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
    // Сценарий туториала за Номер 89.
    // Часть 1 — калибровка в лаборатории: голос оператора учит ходить, стрелять и взламывать.
    // Часть 2 — побег: 89 взламывает шлюз и бежит через коридоры охраны корпорации Е.
    // У самого выхода его убивают в спину, камера улетает вверх, показывается итоговый экран.
    public class TutorialDirector : MonoBehaviour
    {
        private const string Operator = "ОПЕРАТОР";
        private const string Subject = "89";
        private const string EscapeCheckpoint = "tutorial_escape";

        [Header("Player")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private HackingController _hacking;
        [SerializeField] private CameraFollow _cameraFollow;

        [Header("UI")]
        [SerializeField] private SubtitleBox _subtitles;
        [SerializeField] private ObjectivePanel _objective;
        [SerializeField] private HackingHud _hackingHud;
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private TutorialEndScreen _endScreen;

        [Header("Part 1: калибровка")]
        [SerializeField] private TriggerZone[] _moveMarkers;
        [SerializeField] private Health[] _targets;
        [SerializeField] private Health[] _drones;
        [SerializeField] private Door _hackDoor;
        [SerializeField] private Health _glassDrone;

        [Header("Part 2: побег")]
        [SerializeField] private Door _exitGate;
        [SerializeField] private HackableDevice _exitGateHack;
        [SerializeField] private Transform _escapeStart;
        [SerializeField] private Door _firstEscapeDoor;
        [SerializeField] private TriggerZone _corridorZone;
        [SerializeField] private TriggerZone _guardRoomZone;
        [SerializeField] private TriggerZone _droneCorridorZone;
        [SerializeField] private TriggerZone _hallZone;
        [SerializeField] private TriggerZone _exitZone;
        [SerializeField] private EnemyAI _assassin;
        [SerializeField] private Light2D[] _alarmLights;
        [SerializeField] private Light2D _globalLight;
        [SerializeField] private float _forcedDeathDelay = 0.8f;

        [Header("Finale")]
        [SerializeField] private Vector2 _flyCenter = new Vector2(30f, 8f);
        [SerializeField] private float _flySize = 30f;
        [SerializeField] private float _flyDuration = 4.5f;
        [SerializeField] private string _menuScene = "MainMenu";

        private bool _finale;
        private bool _dying;

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
            _exitGateHack.enabled = false;
            _assassin.gameObject.SetActive(false);

            _player.Health.Died += OnPlayerDied;
            _fader.SetBlack(1f);

            // После смерти во второй части сцена перезагружается и стартует сразу с побега
            if (Checkpoint.Is(EscapeCheckpoint))
            {
                PrepareEscapeCheckpoint();
                yield return _fader.Fade(1f, 0f, 0.6f);
                yield return EscapePart();
                yield break;
            }

            yield return new WaitForSeconds(0.6f);
            yield return _fader.Fade(1f, 0f, 2f);

            yield return Say(Operator, "Субъект 89. Пробуждение подтверждено.");
            yield return Say(Operator, "Пульс в норме. Нейроимплант активен. Начинаем калибровку.");

            yield return MovementStep();
            yield return ShootingStep();
            yield return LookStep();
            yield return DronesStep();
            yield return HackingStep();
            yield return CalibrationEnd();
            yield return EscapePart();
        }

        // ------------------------------------------------------------ Часть 1

        private IEnumerator MovementStep()
        {
            _player.CanMove = true;
            SayAsync(Operator, "Двигайся. Клавиши W, A, S, D. Встань на отметку.");

            for (int i = 0; i < _moveMarkers.Length; i++)
            {
                _objective.Set($"Встань на отметки  {i}/{_moveMarkers.Length}");
                TriggerZone marker = _moveMarkers[i];
                marker.gameObject.SetActive(true);
                yield return new WaitUntil(() => marker.WasEntered);
                marker.gameObject.SetActive(false);
            }

            _objective.Set(string.Empty);
            yield return Say(Operator, "Моторика в норме.");
        }

        private IEnumerator ShootingStep()
        {
            yield return Say(Operator, "Мышь — прицел. Левая кнопка мыши — выстрел.");
            _player.CanShoot = true;
            SayAsync(Operator, "Уничтожь учебные мишени.");
            yield return WaitAllDead(_targets, "Уничтожь мишени");
            yield return Say(Operator, "Точность приемлемая.");
        }

        private IEnumerator LookStep()
        {
            yield return Say(Operator, "Обзор оптики расширен. Смотри дальше, чем видишь.");
            SayAsync(Operator, "Зажми Shift и веди мышью — взгляд уйдёт к прицелу.");

            // Засчитываем, когда Shift суммарно продержали пару секунд
            _objective.Set("Осмотрись:  зажми [SHIFT] + мышь");
            float held = 0f;
            while (held < 2f)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.shiftKey.isPressed)
                {
                    held += Time.unscaledDeltaTime;
                }
                yield return null;
            }

            _objective.Set(string.Empty);
            yield return Say(Operator, "Враг, которого ты увидел первым, — уже мёртвый враг.");
        }

        private IEnumerator DronesStep()
        {
            foreach (Health drone in _drones) drone.gameObject.SetActive(true);
            yield return Say(Operator, "Подвижные цели. Учебные дроны. Они не стреляют.");
            SayAsync(Operator, "Пока что.");
            yield return WaitAllDead(_drones, "Уничтожь дронов");
            yield return Say(Operator, "Хорошо. Ты учишься быстрее предыдущих образцов.");
        }

        private IEnumerator HackingStep()
        {
            _hacking.Ram.Refill();
            _hacking.HackingEnabled = true;
            _hackingHud.RamVisible = true;

            yield return Say(Operator, "Твой имплант умеет больше, чем стрелять.");
            yield return Say(Operator, "Зажми TAB — режим сканирования. Время замедлится, а уязвимая техника подсветится.");
            SayAsync(Operator, "Наведи курсор на дверь справа и нажми 1, чтобы взломать замок.");
            _objective.Set("Взломай дверь:  [TAB] + курсор + [1]");
            yield return new WaitUntil(() => _hackDoor.IsOpen);

            _objective.Set(string.Empty);
            yield return Say(Operator, "Каждый взлом расходует оперативную память. Шкала RAM — слева внизу.");
            yield return Say(Operator, "Слева за стеклом — дрон. Пули стекло не пробивают.");
            SayAsync(Operator, "Используй «Короткое замыкание».");
            _objective.Set("Уничтожь дрона за стеклом взломом");
            yield return new WaitUntil(() => _glassDrone == null || _glassDrone.IsDead);

            _objective.Set(string.Empty);
            yield return Say(Operator, "Память восстанавливается сама. Не трать её впустую.");
        }

        private IEnumerator CalibrationEnd()
        {
            yield return Say(Operator, "Калибровка завершена. Субъект 89 признан пригодным.");
            yield return Say(Operator, "Вернись в капсулу. Начинаем подготовку к очистке памяти.");
            yield return Say(Subject, "...Нет.");

            _exitGateHack.enabled = true;
            SayAsync(Subject, "Шлюз в восточной комнате. Если взломать его — я выберусь.");
            _objective.Set("Сбеги: взломай шлюз в восточной комнате");

            // Пока 89 не решился — оператор напоминает о капсуле
            float nagAt = Time.time + 15f;
            int nag = 0;
            string[] nags =
            {
                "Субъект 89. В капсулу.",
                "Субъект 89. Это не просьба.",
            };
            while (!_exitGate.IsOpen)
            {
                if (Time.time > nagAt && nag < nags.Length)
                {
                    SayAsync(Operator, nags[nag++]);
                    nagAt = Time.time + 15f;
                }
                yield return null;
            }

            Checkpoint.Set(EscapeCheckpoint);
            RaiseAlarm();
            yield return Say(Operator, "Попытка побега. Охрана — на уровень Б.");
            SayAsync(Subject, "Бежать. Просто бежать.");
        }

        // ------------------------------------------------------------ Часть 2

        private void PrepareEscapeCheckpoint()
        {
            Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
            rb.position = _escapeStart.position;
            _player.transform.position = _escapeStart.position;
            _cameraFollow.SnapToTarget();

            _player.CanMove = true;
            _player.CanShoot = true;
            _hacking.HackingEnabled = true;
            _hackingHud.RamVisible = true;

            _hackDoor.Open();
            _exitGate.Open();
            RaiseAlarm();
        }

        private IEnumerator EscapePart()
        {
            _objective.Set("Беги");

            yield return new WaitUntil(() => _corridorZone.WasEntered);
            if (!_firstEscapeDoor.IsOpen)
            {
                SayAsync(Subject, "Заперто... Значит, взломать.");
                _objective.Set("Взломай дверь:  [TAB] + курсор + [1]");
                yield return new WaitUntil(() => _firstEscapeDoor.IsOpen);
            }
            _objective.Set("Беги к выходу");

            yield return new WaitUntil(() => _guardRoomZone.WasEntered);
            SayAsync(Operator, "Субъект 89, остановись. Охране разрешён огонь на поражение.");
            _objective.Set("Прорвись через охрану. Охрана тоже взламывается");

            yield return new WaitUntil(() => _droneCorridorZone.WasEntered);
            SayAsync(Subject, "Дрон... Как на тренировке. Только этот стреляет.");
            _objective.Set("Беги к выходу");

            yield return new WaitUntil(() => _hallZone.WasEntered);
            SayAsync(Subject, "Выход... Я вижу выход!");
            _objective.Set("Добеги до выхода");

            yield return new WaitUntil(() => _exitZone.WasEntered);
            yield return ShotInTheBack();
        }

        private IEnumerator ShotInTheBack()
        {
            _finale = true;
            _hacking.HackingEnabled = false;
            _objective.Set(string.Empty);

            // Охранник появляется у входа в зал — за спиной 89 — и сразу стреляет
            _assassin.gameObject.SetActive(true);
            _assassin.Engage(_player, 0.1f);

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
            if (_dying)
            {
                return;
            }

            _dying = true;
            StartCoroutine(_finale ? FinaleSequence() : RestartSequence());
        }

        // Смерть по ходу побега: короткая вспышка и перезапуск с чекпоинта
        private IEnumerator RestartSequence()
        {
            _objective.Set(string.Empty);
            _fader.Flash(new Color(1f, 0.1f, 0.2f), 0.5f);
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(0.8f);
            Time.timeScale = 1f;

            yield return _fader.Fade(0f, 1f, 0.5f);
            yield return _fader.ShowTitle("ЕЩЁ РАЗ", "Перемотка...");
            yield return new WaitForSecondsRealtime(0.4f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Финал: замедление, камера улетает вверх над всем комплексом, итоговый экран
        private IEnumerator FinaleSequence()
        {
            _fader.Flash(new Color(1f, 0.1f, 0.2f), 0.6f);
            Time.timeScale = 0.15f;
            yield return new WaitForSecondsRealtime(1.6f);
            Time.timeScale = 1f;

            _hackingHud.RamVisible = false;
            Checkpoint.Clear();

            SayAsync(Operator, "Субъект 89 ликвидирован.");
            Coroutine fly = StartCoroutine(_cameraFollow.FlyAway(_flyCenter, _flySize, _flyDuration));
            yield return new WaitForSecondsRealtime(_flyDuration * 0.55f);
            SayAsync(Operator, "Данные сохранены. Подготовить субъект 90.");
            yield return fly;

            yield return _endScreen.Show();
        }

        private void RaiseAlarm()
        {
            foreach (Light2D light in _alarmLights) light.enabled = true;
            if (_globalLight != null) _globalLight.color = new Color(1f, 0.45f, 0.5f);
        }

        private void Update()
        {
            // Esc — выйти в меню (удобно при тестах)
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && !_finale)
            {
                Time.timeScale = 1f;
                Checkpoint.Clear();
                SceneManager.LoadScene(_menuScene);
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

        private Coroutine Say(string speaker, string line) => _subtitles.Say(speaker, line);

        private void SayAsync(string speaker, string line) => _subtitles.Say(speaker, line);
    }
}
