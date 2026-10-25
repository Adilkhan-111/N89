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
    // Часть 1 — калибровка в лаборатории: голос оператора учит ходить, стрелять, перезаряжаться и взламывать.
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
        [SerializeField] private AmmoHud _ammoHud;
        [SerializeField] private ActiveReload _reload;
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

        [Header("Finale")]
        [SerializeField] private Vector2 _flyCenter = new Vector2(32f, 8f);
        [SerializeField] private float _flySize = 34f;
        [SerializeField] private float _flyDuration = 4.5f;
        [SerializeField] private string _menuScene = "MainMenu";

        [Header("Катсцена: выстрел в спину")]
        [SerializeField] private RectTransform _letterboxTop;
        [SerializeField] private RectTransform _letterboxBottom;
        [SerializeField] private float _letterboxHeight = 120f;
        [Tooltip("Трассер пули: растягивается от ствола до 89")]
        [SerializeField] private SpriteRenderer _tracer;
        [Tooltip("Вспышка в момент попадания, вытянута по направлению пули")]
        [SerializeField] private SpriteRenderer _impactLine;
        [SerializeField] private float _impactLength = 4f;
        [Tooltip("Спрайт 89, лежащего лицом вниз (голова смотрит вправо, +x)")]
        [SerializeField] private Sprite _deadSprite;
        [SerializeField] private GameObject _muzzleFxPrefab;
        [SerializeField] private GameObject _bloodPoolPrefab;
        [Tooltip("Сколько шагов охранник делает к 89, прежде чем выстрелить")]
        [SerializeField] private float _guardWalk = 3f;
        [SerializeField] private float _hitZoom = 4.2f;

        private bool _finale;
        private bool _dying;
        private bool _restarting;

        private IEnumerator Start()
        {
            _player.CanMove = false;
            _player.CanShoot = false;
            _hacking.HackingEnabled = false;
            _hackingHud.RamVisible = false;
            _ammoHud.Visible = false;
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

            // Счёт появится на шаге с дронами, где оператор объясняет серии
            if (ScoreCounter.Instance != null) ScoreCounter.Instance.Visible = false;
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
            _ammoHud.Visible = true;
            yield return Say(Operator, "Патроны — справа внизу. Магазин опустеет — над тобой появится шкала перезарядки.");
            yield return Say(Operator, "Бегунок ходит туда-обратно. Нажми левую кнопку мыши, когда он в зелёной зоне.");
            _player.CanShoot = true;
            SayAsync(Operator, "Уничтожь учебные мишени.");
            yield return WaitAllDead(_targets, "Уничтожь мишени");
            yield return Say(Operator, "Точность приемлемая.");
            yield return ReloadStep();
        }

        // Досрочная перезарядка: R, не дожидаясь пустого магазина
        private IEnumerator ReloadStep()
        {
            yield return Say(Operator, "Не жди, пока магазин опустеет. Клавиша R — перезарядка в любой момент.");
            yield return Say(Operator, "Промах по зелёной зоне стоит времени. В бою это смерть.");
            int reloads = 0;
            void Count() => reloads++;
            _reload.Reloaded += Count;
            SayAsync(Operator, "Выстрели и перезарядись досрочно.");
            _objective.Set("Перезарядись:  выстрели, [R], затем [ЛКМ] в зелёной зоне");
            yield return new WaitUntil(() => reloads > 0);
            _reload.Reloaded -= Count;
            _objective.Set(string.Empty);
            yield return Say(Operator, "Перезарядка в норме.");
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
            if (ScoreCounter.Instance != null) ScoreCounter.Instance.Visible = true;
            yield return Say(Operator, "Эффективность оценивается. Счёт — справа вверху.");
            yield return Say(Operator, "Уничтожай цели серией: каждая следующая в серии стоит больше.");
            SayAsync(Operator, "Пауза дольше трёх секунд обрывает серию. Ранг выставим в конце.");
            yield return WaitAllDead(_drones, "Уничтожь дронов одной серией");
            ScoreCounter score = ScoreCounter.Instance;
            if (score != null && score.MaxCombo >= _drones.Length)
                yield return Say(Operator, "Чистая серия. Ты учишься быстрее предыдущих образцов.");
            else
                yield return Say(Operator, "Серия прервалась. Приемлемо. Дальше — быстрее.");
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
            yield return Say(Operator, "При сканировании оружие блокируется. Попытка выстрела сорвёт взлом.");
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
            _ammoHud.Visible = true;

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

        // Катсцена у выхода: камера разворачивается к охраннику за спиной 89, он подходит и стреляет.
        // Склейка на 89 — стоп-кадр с вертикальной вспышкой попадания, 89 падает.
        private IEnumerator ShotInTheBack()
        {
            _finale = true;
            _player.CanMove = false;
            _player.CanShoot = false;
            _player.CanAim = false;
            _hacking.HackingEnabled = false;
            _hackingHud.RamVisible = false;
            _ammoHud.Visible = false;
            _objective.Set(string.Empty);
            FreezeOtherEnemies();
            StartCoroutine(Letterbox(_letterboxHeight, 0.5f));

            yield return new WaitForSeconds(0.8f);

            // Охранник появляется у входа в зал — за спиной 89. ИИ выключен: всё по сценарию
            _assassin.enabled = false;
            _assassin.gameObject.SetActive(true);
            Transform guard = _assassin.transform;
            Transform player = _player.transform;
            _cameraFollow.Focus(guard, 3f);
            SayAsync(Operator, "Не дай ему выйти.");
            yield return new WaitForSeconds(1f);

            Vector2 toPlayer = (Vector2)(player.position - guard.position);
            Vector2 walkTo = (Vector2)guard.position + toPlayer.normalized * Mathf.Clamp(toPlayer.magnitude - 2f, 0f, _guardWalk);
            while (Vector2.Distance(guard.position, walkTo) > 0.05f)
            {
                guard.position = Vector2.MoveTowards(guard.position, walkTo, 2.2f * Time.deltaTime);
                AimGuard(player.position, 720f);
                yield return null;
            }

            // Вскидывает пистолет и замирает
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                AimGuard(player.position, 360f);
                yield return null;
            }

            // Выстрел
            Transform muzzle = _assassin.Body.Find("Muzzle");
            Vector2 from = muzzle != null ? muzzle.position : guard.position;
            Vector2 hitPoint = player.position;
            Vector2 shotDir = (hitPoint - from).normalized;
            if (_muzzleFxPrefab != null)
            {
                Instantiate(_muzzleFxPrefab, from, _assassin.Body.rotation);
            }
            _cameraFollow.Shake(0.15f, 0.1f);
            StartCoroutine(Tracer(from, hitPoint));
            yield return new WaitForSecondsRealtime(0.12f);

            // Склейка на 89: попадание, стоп-кадр, вспышка
            _cameraFollow.CutTo(player);
            StartCoroutine(_cameraFollow.Zoom(_hitZoom, 0.2f));
            // Тело 89 укладывает сама катсцена (PlayerFall)
            _player.Health.SpawnCorpse = false;
            _player.Health.Kill();
            Time.timeScale = 0f;
            StartCoroutine(ImpactLine(hitPoint, shotDir));
            yield return new WaitForSecondsRealtime(0.18f);

            // Падает в замедлении
            Time.timeScale = 0.35f;
            yield return PlayerFall(shotDir);
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(1f);

            yield return FinaleSequence();
        }

        private void AimGuard(Vector2 target, float turnSpeed)
        {
            Transform body = _assassin.Body;
            Vector2 dir = target - (Vector2)body.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float next = Mathf.MoveTowardsAngle(body.eulerAngles.z, angle, turnSpeed * Time.deltaTime);
            body.rotation = Quaternion.Euler(0f, 0f, next);
        }

        // На время катсцены остальные враги замирают, а пули в полёте исчезают
        private void FreezeOtherEnemies()
        {
            foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>())
            {
                if (enemy != _assassin) enemy.enabled = false;
            }
            foreach (Bullet bullet in FindObjectsByType<Bullet>())
            {
                Destroy(bullet.gameObject);
            }
        }

        private IEnumerator Letterbox(float height, float duration)
        {
            if (_letterboxTop == null || _letterboxBottom == null)
            {
                yield break;
            }

            float from = _letterboxTop.sizeDelta.y;
            for (float t = 0f; t <= duration; t += Time.unscaledDeltaTime)
            {
                float h = Mathf.Lerp(from, height, Mathf.SmoothStep(0f, 1f, t / duration));
                _letterboxTop.sizeDelta = new Vector2(0f, h);
                _letterboxBottom.sizeDelta = new Vector2(0f, h);
                yield return null;
            }
            _letterboxTop.sizeDelta = new Vector2(0f, height);
            _letterboxBottom.sizeDelta = new Vector2(0f, height);
        }

        // Тонкий трассер от ствола до 89, гаснет за долю секунды
        private IEnumerator Tracer(Vector2 from, Vector2 to)
        {
            if (_tracer == null)
            {
                yield break;
            }

            Vector2 unit = _tracer.sprite.bounds.size;
            Vector2 delta = to - from;
            _tracer.transform.position = (from + to) * 0.5f;
            _tracer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _tracer.gameObject.SetActive(true);

            Color color = _tracer.color;
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.25f;
                _tracer.transform.localScale = new Vector3(delta.magnitude / unit.x, Mathf.Lerp(0.08f, 0f, k) / unit.y, 1f);
                _tracer.color = new Color(color.r, color.g, color.b, 1f - k);
                yield return null;
            }

            _tracer.gameObject.SetActive(false);
            _tracer.color = color;
        }

        // Линия попадания по направлению пули: пробивает 89 насквозь, вспыхивает и истончается
        private IEnumerator ImpactLine(Vector2 point, Vector2 dir)
        {
            if (_impactLine == null)
            {
                yield break;
            }

            Vector2 unit = _impactLine.sprite.bounds.size;
            // Чуть смещена вперёд — как будто пуля вышла с другой стороны
            _impactLine.transform.position = point + dir * (_impactLength * 0.15f);
            _impactLine.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            _impactLine.gameObject.SetActive(true);
            Color color = _impactLine.color;
            const float width = 0.12f;

            for (float t = 0f; t < 0.06f; t += Time.unscaledDeltaTime)
            {
                _impactLine.transform.localScale = new Vector3(Mathf.Lerp(0f, _impactLength, t / 0.06f) / unit.x, width / unit.y, 1f);
                yield return null;
            }

            _impactLine.transform.localScale = new Vector3(_impactLength / unit.x, width / unit.y, 1f);
            yield return new WaitForSecondsRealtime(0.12f);

            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.35f;
                _impactLine.transform.localScale = new Vector3(_impactLength / unit.x, Mathf.Lerp(width, 0f, k) / unit.y, 1f);
                _impactLine.color = new Color(color.r, color.g, color.b, 1f - k);
                yield return null;
            }

            _impactLine.gameObject.SetActive(false);
            _impactLine.color = color;
        }

        // 89 спотыкается вперёд по линии выстрела и падает лицом вниз: спрайт меняется на лежащее тело,
        // в момент удара об пол камера вздрагивает, под ним растекается синяя кровь
        private IEnumerator PlayerFall(Vector2 shotDir)
        {
            Transform root = _player.transform;
            Transform body = root.Find("Body");
            SpriteRenderer sprite = body.GetComponent<SpriteRenderer>();
            float shotAngle = Mathf.Atan2(shotDir.y, shotDir.x) * Mathf.Rad2Deg;

            // Шаг вперёд от удара пули, корпус разворачивает по выстрелу
            Vector3 start = root.position;
            Vector3 stagger = start + (Vector3)(shotDir * 0.3f);
            float fromAngle = body.eulerAngles.z;
            for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.15f;
                root.position = Vector3.Lerp(start, stagger, k);
                body.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(fromAngle, shotAngle, k));
                yield return null;
            }

            // Падение: лежащее тело головой по направлению выстрела
            if (_deadSprite != null)
            {
                sprite.sprite = _deadSprite;
            }
            sprite.color = Color.white;
            float lieAngle = shotAngle + Random.Range(-15f, 15f);
            body.rotation = Quaternion.Euler(0f, 0f, lieAngle);
            _cameraFollow.Shake(0.25f, 0.15f);

            Vector3 end = stagger + (Vector3)(shotDir * 0.6f);
            if (_bloodPoolPrefab != null)
            {
                Transform pool = Instantiate(_bloodPoolPrefab, end, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))).transform;
                StartCoroutine(Grow(pool, 1.6f, 2.2f));
            }

            const float duration = 0.35f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / duration, 3f);
                root.position = Vector3.Lerp(stagger, end, k);
                // Удар об пол: тело сплющивается и распрямляется
                body.localScale = new Vector3(Mathf.Lerp(1.15f, 1f, k), Mathf.Lerp(0.8f, 1f, k), 1f);
                yield return null;
            }
            body.localScale = Vector3.one;

            // Тело темнеет
            Color dead = new Color(0.72f, 0.74f, 0.86f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
            {
                sprite.color = Color.Lerp(Color.white, dead, t);
                yield return null;
            }
        }

        private static IEnumerator Grow(Transform target, float scale, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(0f, scale, Mathf.Sqrt(t / duration));
                yield return null;
            }
            target.localScale = Vector3.one * scale;
        }

        private void OnPlayerDied(Health health)
        {
            if (_dying)
            {
                return;
            }

            _dying = true;
            // В финале смертью управляет катсцена
            if (!_finale)
            {
                _restarting = true;
                StartCoroutine(RestartSequence());
            }
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
            yield return _fader.ShowTitle("ЕЩЁ РАЗ", "Перемотка...    [R]");
            yield return new WaitForSecondsRealtime(0.4f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Финал: камера улетает вверх над всем комплексом, итоговый экран
        private IEnumerator FinaleSequence()
        {
            Checkpoint.Clear();

            SayAsync(Operator, "Субъект 89 ликвидирован.");
            Coroutine fly = StartCoroutine(_cameraFollow.FlyAway(_flyCenter, _flySize, _flyDuration));
            yield return new WaitForSecondsRealtime(_flyDuration * 0.55f);
            SayAsync(Operator, "Данные сохранены. Подготовить субъект 90.");
            yield return fly;

            yield return Letterbox(0f, 0.4f);
            yield return _endScreen.Show();
        }

        private void RaiseAlarm()
        {
            foreach (Light2D light in _alarmLights) light.enabled = true;
            if (_globalLight != null) _globalLight.color = new Color(1f, 0.45f, 0.5f);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            // Быстрый рестарт: после смерти R сразу перезапускает с чекпоинта
            if (_restarting && kb != null && kb.rKey.wasPressedThisFrame)
            {
                _restarting = false;
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }
            // Esc — пауза (PauseMenu)
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
