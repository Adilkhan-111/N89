using System.Collections;
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

namespace N89.Missions
{
    // Сценарий миссии: герой стартует со снаряжением из арсенала (Loadout), цель — убить всех врагов.
    // Смерть — как в Hotline Miami: короткая фраза героя и перезапуск уровня с начала.
    // Если задан второй этаж: зачистил первый — поднимайся по лестнице (дальше смерть отматывает ко второму
    // этажу), убей всех, кроме цели, зайди в кабинет — катсцена: цель встаёт, герой стреляет, цель падает на стол.
    // Когда всё сделано — итоговый экран миссии.
    public class MissionDirector : MonoBehaviour
    {

        [Header("Hero")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private HackingController _hacking;
        [Tooltip("Миссия 3: Сандевистан вместо взлома (HackingController на герое выключен). В начале — сперва подсказка про него, потом про имплант на руки")]
        [SerializeField] private Sandevistan _sandevistan;
        [SerializeField] private string _sandevistanTitle = "САНДЕВИСТАН";
        [SerializeField] private string _sandevistanHint = "TAB — УСКОРЕНИЕ НА 3 С   ·   НЕ ВЫКЛЮЧИТЬ, ПОКА НЕ КОНЧИТСЯ ЭНЕРГИЯ";
        [SerializeField] private Weapon _weapon;
        [SerializeField] private SpriteRenderer _heroBody;
        [SerializeField] private CameraFollow _cameraFollow;

        [Header("Снаряжение")]
        [SerializeField] private int _mission = 1;
        [SerializeField] private WeaponData[] _weapons;
        [Tooltip("Если в арсенале перед этой миссией ничего не выбирали (сцену запустили напрямую)")]
        [SerializeField] private int _defaultWeapon = 1;

        [Header("Враги")]
        [SerializeField] private Health[] _enemies;

        [Header("Второй этаж (необязательно)")]
        [SerializeField] private Health[] _floor2Enemies;
        [Tooltip("Лестница наверх на первом этаже (мировые координаты)")]
        [SerializeField] private Rect _stairsZone;
        [SerializeField] private Light2D _stairsMarker;
        [SerializeField] private Transform _floor2Spawn;
        [SerializeField] private float _floor2Facing = 0f;
        [Tooltip("Сколько секунд после появления наверху охрана второго этажа ещё не реагирует")]
        [SerializeField] private float _floor2Grace = 1.5f;
        [Tooltip("Без цели (_target пуст): после зачистки второго этажа — дойти сюда (мировые координаты), и миссия окончена")]
        [SerializeField] private Rect _floor2ExitZone;
        [SerializeField] private Light2D _floor2ExitMarker;
        [SerializeField] private string _floor2ExitText = "Поднимись на лифте";

        // Этаж выше второго (миссия 3): приезд на лифте, свой чекпоинт, выход — снова в лифт
        [System.Serializable]
        public class UpperFloor
        {
            public Health[] Enemies;
            public Transform Spawn;
            public float Facing = -90f;
            public string Text = "Зачисти этаж";
            public string Checkpoint;
            [Tooltip("Кабина лифта: здесь герой появляется, сюда же возвращается после зачистки")]
            public Rect ExitZone;
            public Light2D ExitMarker;
            [Tooltip("Засада: герой вышел из кабины — эти двери открываются, враги за ними сразу идут на него")]
            public Door AmbushDoor;
            public EnemyAI[] Ambushers;
            [Tooltip("Последний этаж: противостояние, начальник, сейф с relic (поля «Финал»), выход — к машине")]
            public bool Finale;
        }

        [Header("Финал миссии 3 (этаж с Finale)")]
        [Tooltip("Стоят и целятся, пока герой не включит Сандевистан")]
        [SerializeField] private EnemyAI[] _standoffEnemies = new EnemyAI[0];
        [Tooltip("Герой вошёл сюда — противостояние (мировые координаты)")]
        [SerializeField] private Rect _standoffZone;
        [SerializeField] private float _standoffReaction = 0.7f;
        [SerializeField] private string _standoffTitle = "ОНИ ЖДУТ";
        [SerializeField] private string _standoffHint = "TAB — Сандевистан";
        [SerializeField] private string _targetSpeaker = "ГЛАВАРЬ";
        [Tooltip("Пистолет на столе цели: в катсцене она тянется к нему и разворачивается к герою (необязательно)")]
        [SerializeField] private SpriteRenderer _targetGun;
        [Tooltip("Ключ от сейфа: появляется у тела начальника")]
        [SerializeField] private SpriteRenderer _keyItem;
        [SerializeField] private Vector2 _keyOffset = new Vector2(-1f, -0.5f);
        [SerializeField] private Transform _safe;
        [SerializeField] private SpriteRenderer _safeBody;
        [SerializeField] private Sprite _safeOpen;
        [SerializeField] private SpriteRenderer _relicItem;
        [SerializeField] private float _pickupRadius = 0.9f;
        [SerializeField] private string _keyText = "Возьми ключ от сейфа";
        [SerializeField] private string _safeText = "Открой сейф";
        [SerializeField] private string _relicTitle = "RELIC";
        [SerializeField] private string _relicSubtitle = "КОНСТРУКТ У ТЕБЯ";
        [SerializeField] private string[] _relicLines = { "..." };

        [Header("Этажи выше второго (без цели)")]
        [SerializeField] private UpperFloor[] _upperFloors = new UpperFloor[0];
        [SerializeField] private float _ambushReaction = 0.35f;
        [Tooltip("Кабинет цели: герой внутри после зачистки — катсцена")]
        [SerializeField] private Rect _officeZone;
        [Tooltip("Куда указывает стрелка после зачистки второго этажа — дверь кабинета")]
        [SerializeField] private Vector2 _officeDoor;
        [SerializeField] private Banner _banner;
        [SerializeField] private WaypointArrow _waypoint;
        [Tooltip("Подсказка про имплант в начале миссии (пусто — не показывать)")]
        [SerializeField] private string _implantTitle = "РУКИ ГОРИЛЛЫ";
        [SerializeField] private string _implantHint = "ПКМ — РЫВОК С УДАРОМ   ·   ЗАЖАТЬ ПКМ — БЛОК";
        [Tooltip("Миссия 3: на герое оба импланта на руки, остаётся выбранный в арсенале (по id ImplantData), первый — по умолчанию")]
        [SerializeField] private MonoBehaviour[] _armImplants;
        [SerializeField] private string[] _armImplantIds;
        [SerializeField] private string[] _armImplantTitles;
        [SerializeField] private string[] _armImplantHints;
        [SerializeField] private string _stairsText = "Поднимись на второй этаж";
        [SerializeField] private string _floor2Text = "Убей всех, кроме цели";
        [SerializeField] private string _officeText = "Зайди в кабинет цели";
        [SerializeField] private string _checkpoint = "mission01_floor2";
        [Tooltip("Подвал с Боссом (миссия 2): вместо второго этажа лестница ведёт вниз, на бой")]
        [SerializeField] private BossFight _bossFight;

        [Header("Цель")]
        [SerializeField] private Transform _target;
        [SerializeField] private SpriteRenderer _targetBody;
        [SerializeField] private Sprite _targetStanding;
        [SerializeField] private Sprite _targetDead;
        [Tooltip("Куда цель падает — середина стола (мировые координаты)")]
        [SerializeField] private Vector2 _deskPoint;
        [SerializeField] private GameObject _bloodFxPrefab;
        [SerializeField] private GameObject _bloodPoolPrefab;
        [SerializeField] private string[] _targetLines = { "Стой... Стой! Кто тебя прислал?", "Корпорация? Я заплачу вдвое больше!" };

        [Header("Отход к машине (необязательно)")]
        [Tooltip("Летающая машина Рэя у входа: цель выполнена — вернись к ней и улетай, только тогда миссия окончена")]
        [SerializeField] private AirCar _car;
        [Tooltip("Насколько близко к центру машины подойти, чтобы сесть")]
        [SerializeField] private float _carReach = 1.9f;
        [Tooltip("Лестница обратно на первый этаж (на втором этаже / в подвале, мировые координаты); пусто — герой уже внизу")]
        [SerializeField] private Rect _returnZone;
        [SerializeField] private Transform _returnSpawn;
        [SerializeField] private float _returnFacing = -90f;
        [SerializeField] private string _returnText = "Спустись на первый этаж";
        [SerializeField] private string _carText = "Вернись к машине";

        [Header("UI")]
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private ObjectivePanel _objective;
        [SerializeField] private HackingHud _hackingHud;
        [SerializeField] private TutorialEndScreen _endScreen;
        [SerializeField] private SubtitleBox _subtitles;
        [SerializeField] private RectTransform _letterboxTop;
        [SerializeField] private RectTransform _letterboxBottom;
        [SerializeField] private string _title = "МИССИЯ 1";
        [SerializeField] private string _subtitle = "Логово банды";
        [SerializeField] private string _objectiveText = "Зачисти логово банды";
        [SerializeField] private string _clearedText = "Логово зачищено";
        [Tooltip("Фразы героя после смерти — одна случайная на экране перемотки")]
        [SerializeField] private string[] _deathLines = { "Это не то.", "Я что-то упускаю.", "Ещё раз. Тише.", "Слишком громко." };
        [SerializeField] private string _menuScene = "MainMenu";

        private bool _over;
        private bool _dead;
        // Сцена перезапущена после смерти — заставку не показываем
        private static bool s_restarted;

        // Перезагрузка домена при входе в Play Mode выключена — сбрасываем вручную
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_restarted = false;
        private bool HasFloor2 => _floor2Enemies != null && _floor2Enemies.Length > 0;
        private bool HasStairs => _stairsZone.width > 0f;
        private bool HasBasement => _bossFight != null;

        private IEnumerator Start()
        {
            ApplyLoadout();
            _player.Health.Died += OnPlayerDied;
            if (_stairsMarker != null) _stairsMarker.enabled = false;
            if (_floor2ExitMarker != null) _floor2ExitMarker.enabled = false;

            _player.CanMove = false;
            _player.CanShoot = false;
            _hacking.HackingEnabled = false;
            // Миссия 3: взлома нет — RAM прячется, вместо неё шкала энергии Сандевистана
            _hackingHud.RamVisible = _sandevistan == null;
            _objective.Set(string.Empty);
            _fader.SetBlack(1f);

            // Враги второго этажа (и выше) ждут, пока герой не поднимется
            SetFloor2Active(false);
            foreach (UpperFloor upper in _upperFloors)
            {
                SetEnemiesActive(upper.Enemies, false);
                if (upper.ExitMarker != null) upper.ExitMarker.enabled = false;
            }

            // Смерть на этаже выше второго отматывает к его лифту: нижние этажи уже зачищены
            int resumeFloor = -1;
            for (int i = 0; i < _upperFloors.Length; i++)
            {
                if (HasFloor2 && Checkpoint.Is(_upperFloors[i].Checkpoint)) resumeFloor = i;
            }
            bool fromUpper = resumeFloor >= 0;

            bool fromCheckpoint = (HasFloor2 || HasBasement) && (Checkpoint.Is(_checkpoint) || fromUpper);
            // После смерти — без заставки: сразу в бой
            bool restarted = s_restarted;
            s_restarted = false;
            if (fromUpper)
            {
                foreach (Health enemy in _enemies) enemy.gameObject.SetActive(false);
                foreach (Health enemy in _floor2Enemies) enemy.gameObject.SetActive(false);
                for (int i = 0; i < resumeFloor; i++)
                {
                    foreach (Health enemy in _upperFloors[i].Enemies) enemy.gameObject.SetActive(false);
                }
                MoveHero(_upperFloors[resumeFloor].Spawn.position, _upperFloors[resumeFloor].Facing);
            }
            else if (fromCheckpoint)
            {
                // Смерть на втором этаже (или в подвале): первый уже зачищен
                foreach (Health enemy in _enemies) enemy.gameObject.SetActive(false);
                MoveHero(_floor2Spawn.position, _floor2Facing);
                if (HasBasement) _bossFight.Prepare(true);
            }
            else if (!restarted)
            {
                yield return new WaitForSeconds(0.3f);
                yield return _fader.ShowTitle(_title, _subtitle);
                yield return new WaitForSeconds(1f);
                yield return _fader.HideTitle(0.5f);
            }

            SetControl(true);
            if (!fromCheckpoint)
            {
                UpdateObjective(_objectiveText, _enemies);
            }
            yield return _fader.Fade(1f, 0f, restarted ? 0.25f : 1f);
            if (!fromCheckpoint && !restarted && _banner != null && !string.IsNullOrEmpty(_implantTitle))
            {
                StartCoroutine(ImplantBanners());
            }
            if (fromCheckpoint && !fromUpper)
            {
                yield return Floor2Grace();
            }

            if (!fromCheckpoint)
            {
                yield return WaitCleared(_objectiveText, _enemies);
                if (_over) yield break;
            }

            if (HasFloor2)
            {
                if (!fromCheckpoint)
                {
                    yield return GoUpstairs();
                }

                if (!fromUpper)
                {
                    yield return WaitCleared(_floor2Text, _floor2Enemies);
                    if (_over) yield break;
                }

                if (_target != null && _upperFloors.Length == 0)
                {
                    _objective.Set(_officeText);
                    Announce("ЭТАЖ ЗАЧИЩЕН", _officeText.ToUpper(), _officeDoor);
                    yield return new WaitUntil(() => _over || _officeZone.Contains(_player.transform.position));
                    if (_waypoint != null) _waypoint.Hide();
                    if (_over) yield break;

                    _over = true;
                    yield return TargetCutscene();
                }
                else
                {
                    // Миссия 3: цели нет — этаж за этажом на лифте (_upperFloors); с последнего — конец
                    int floor = resumeFloor; // -1 — второй этаж
                    bool finale = false;
                    if (fromUpper)
                    {
                        yield return RunUpperFloor(_upperFloors[floor]);
                        if (_over) yield break;
                        finale = _upperFloors[floor].Finale;
                    }
                    while (!finale)
                    {
                        Rect exitZone = floor < 0 ? _floor2ExitZone : _upperFloors[floor].ExitZone;
                        Light2D exitMarker = floor < 0 ? _floor2ExitMarker : _upperFloors[floor].ExitMarker;
                        _objective.Set(_floor2ExitText);
                        if (exitMarker != null) exitMarker.enabled = true;
                        Announce("ЭТАЖ ЗАЧИЩЕН", _floor2ExitText.ToUpper(), exitZone.center);
                        // Герой мог остаться в кабине, где приехал: ждём, пока выйдет и зайдёт снова
                        yield return new WaitUntil(() => _over || !exitZone.Contains(_player.transform.position));
                        yield return new WaitUntil(() => _over || exitZone.Contains(_player.transform.position));
                        if (_waypoint != null) _waypoint.Hide();
                        if (exitMarker != null) exitMarker.enabled = false;
                        if (_over) yield break;

                        floor++;
                        if (floor >= _upperFloors.Length) break;
                        yield return RideUp(_upperFloors[floor]);
                        yield return RunUpperFloor(_upperFloors[floor]);
                        if (_over) yield break;
                        finale = _upperFloors[floor].Finale;
                    }

                    if (finale)
                    {
                        // Последний этаж: катсцена с начальником, ключ, сейф с relic — дальше к машине (Extract)
                        yield return Finale();
                        if (_over) yield break;
                        _over = true;
                    }
                    else
                    {
                        _over = true;
                        _objective.Set(_clearedText);
                        yield return new WaitForSeconds(1.5f);
                    }
                }
                Checkpoint.Clear();
            }
            else if (HasBasement)
            {
                // Вниз по лестнице — в тёмный подвал, к Боссу (BossFight ведёт бой и финальную сцену)
                if (!fromCheckpoint)
                {
                    yield return GoUpstairs();
                }

                yield return _bossFight.Run(fromCheckpoint);
                if (_over) yield break;
                _over = true;
                Checkpoint.Clear();
            }
            else
            {
                // Лестница есть, а этажа выше пока нет: дошёл до неё — миссия окончена
                if (HasStairs)
                {
                    _objective.Set(_stairsText);
                    if (_stairsMarker != null) _stairsMarker.enabled = true;
                    Announce("ЭТАЖ ЗАЧИЩЕН", _stairsText.ToUpper(), _stairsZone.center);
                    yield return new WaitUntil(() => _over || _stairsZone.Contains(_player.transform.position));
                    if (_waypoint != null) _waypoint.Hide();
                    if (_over) yield break;
                }

                _over = true;
                _objective.Set(_clearedText);
                yield return new WaitForSeconds(1.5f);
            }

            Time.timeScale = 1f;
            if (_car != null) yield return Extract();
            _player.CanShoot = false;
            _hacking.HackingEnabled = false;
            yield return _endScreen.Show();
        }

        // Подсказки в начале: про Сандевистан (если есть), потом про имплант на руки
        private IEnumerator ImplantBanners()
        {
            const float duration = 3.5f;
            if (_sandevistan != null)
            {
                _banner.Show(_sandevistanTitle, _sandevistanHint, duration);
                yield return new WaitForSecondsRealtime(duration + 0.6f);
            }
            _banner.Show(_implantTitle, _implantHint, duration);
        }

        // Оружие из арсенала: параметры стрельбы и спрайт героя с этим стволом
        private void ApplyLoadout()
        {
            WeaponData chosen = _weapons[Mathf.Clamp(_defaultWeapon, 0, _weapons.Length - 1)];
            if (Loadout.IsChosenFor(_mission))
            {
                foreach (WeaponData weapon in _weapons)
                {
                    if (weapon.name == Loadout.WeaponId) chosen = weapon;
                }
            }

            _weapon.Configure(chosen);
            if (chosen.HeroSprite != null)
            {
                _heroBody.sprite = chosen.HeroSprite;
            }

            // Имплант на руки из арсенала; остальные снимаются с героя
            if (_armImplants == null || _armImplants.Length == 0) return;
            int arm = 0;
            if (Loadout.IsChosenFor(_mission))
            {
                for (int i = 0; i < _armImplantIds.Length; i++)
                {
                    if (_armImplantIds[i] == Loadout.ImplantId) arm = i;
                }
            }
            for (int i = 0; i < _armImplants.Length; i++)
            {
                if (i != arm) Destroy(_armImplants[i]);
            }
            _implantTitle = _armImplantTitles[arm];
            _implantHint = _armImplantHints[arm];
        }

        private IEnumerator WaitCleared(string text, Health[] group)
        {
            int shown = -1;
            while (!_over)
            {
                int dead = CountDead(group);
                if (dead != shown)
                {
                    shown = dead;
                    UpdateObjective(text, group);
                }

                if (dead >= group.Length) yield break;
                yield return null;
            }
        }

        private void UpdateObjective(string text, Health[] group)
        {
            _objective.Set($"{text}  {CountDead(group)}/{group.Length}");
        }

        private static int CountDead(Health[] group)
        {
            int dead = 0;
            foreach (Health h in group)
            {
                if (h == null || h.IsDead) dead++;
            }
            return dead;
        }

        // ------------------------------------------------------------ Лестница

        private IEnumerator GoUpstairs()
        {
            _objective.Set(_stairsText);
            if (_stairsMarker != null) _stairsMarker.enabled = true;
            Announce("ЭТАЖ ЗАЧИЩЕН", _stairsText.ToUpper(), _stairsZone.center);
            yield return new WaitUntil(() => _over || _stairsZone.Contains(_player.transform.position));
            if (_over) yield break;

            SetControl(false);
            yield return _fader.Fade(0f, 1f, 0.4f);
            if (_stairsMarker != null) _stairsMarker.enabled = false;
            if (_waypoint != null) _waypoint.Hide();
            MoveHero(_floor2Spawn.position, _floor2Facing);
            if (HasBasement) _bossFight.Prepare(false);
            // С этого момента смерть отматывает ко второму этажу (или в подвал)
            Checkpoint.Set(_checkpoint);
            yield return new WaitForSeconds(0.2f);
            SetControl(true);
            yield return _fader.Fade(1f, 0f, 0.4f);
            yield return Floor2Grace();
        }

        // Лифт на следующий этаж: затемнение, герой в кабине наверху, чекпоинт этого этажа
        private IEnumerator RideUp(UpperFloor floor)
        {
            SetControl(false);
            yield return _fader.Fade(0f, 1f, 0.4f);
            MoveHero(floor.Spawn.position, floor.Facing);
            Checkpoint.Set(floor.Checkpoint);
            yield return new WaitForSeconds(0.2f);
            SetControl(true);
            yield return _fader.Fade(1f, 0f, 0.4f);
        }

        // Этаж выше второго: охрана оживает через _floor2Grace, засада ждёт, пока герой выйдет из кабины
        private IEnumerator RunUpperFloor(UpperFloor floor)
        {
            UpdateObjective(floor.Text, floor.Enemies);
            yield return new WaitForSeconds(_floor2Grace);
            SetEnemiesActive(floor.Enemies, true);
            if (floor.AmbushDoor != null) StartCoroutine(Ambush(floor));
            if (floor.Finale && _standoffEnemies.Length > 0)
            {
                foreach (EnemyAI ai in _standoffEnemies) ai.enabled = false;
                StartCoroutine(Standoff());
            }
            yield return WaitCleared(floor.Text, floor.Enemies);
        }

        // ------------------------------------------------------------ Финал миссии 3

        // Герой входит в лабораторию — охрана стоит и целится в него, но не стреляет. Стрелять нельзя,
        // пока не включён Сандевистан (TAB, шкала заполняется заранее); включил — все оживают разом
        private IEnumerator Standoff()
        {
            yield return new WaitUntil(() => _over || _standoffZone.Contains(_player.transform.position));
            // Всех уже перестрелял из-за двери — ждать некого
            if (_over || !AnyAlive(_standoffEnemies)) yield break;

            _player.CanShoot = false;
            if (_sandevistan != null) _sandevistan.Refill();
            _cameraFollow.Shake(0.05f, 0.2f);
            if (_banner != null) _banner.Show(_standoffTitle, _standoffHint, 4f);
            _objective.Set(_standoffHint);

            while (!_over && AnyAlive(_standoffEnemies) && (_sandevistan == null || !_sandevistan.IsActive))
            {
                foreach (EnemyAI ai in _standoffEnemies)
                {
                    if (ai == null || ai.GetComponent<Health>().IsDead) continue;
                    Transform body = ai.transform.Find("Body");
                    Vector2 dir = _player.transform.position - ai.transform.position;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    body.rotation = Quaternion.RotateTowards(body.rotation, Quaternion.Euler(0f, 0f, angle), 360f * Time.deltaTime);
                }
                yield return null;
            }
            if (_over) yield break;

            _player.CanShoot = true;
            foreach (EnemyAI ai in _standoffEnemies)
            {
                if (ai == null || ai.GetComponent<Health>().IsDead) continue;
                ai.enabled = true;
                ai.Engage(_player, _standoffReaction);
            }
        }

        private static bool AnyAlive(EnemyAI[] enemies)
        {
            foreach (EnemyAI ai in enemies)
            {
                if (ai != null && !ai.GetComponent<Health>().IsDead) return true;
            }
            return false;
        }

        // Этаж зачищен: начальник встаёт и угрожает — Рэй стреляет (TargetCutscene). У трупа — ключ,
        // ключом открывается сейф в стене, из него Рэй забирает relic
        private IEnumerator Finale()
        {
            Time.timeScale = 1f;
            yield return TargetCutscene();

            // Ключ выпадает у тела
            _keyItem.transform.position = (Vector2)_target.position + _keyOffset;
            _keyItem.gameObject.SetActive(true);
            _cameraFollow.Focus(null, 0f);
            SetControl(true);
            _player.CanAim = true;

            _objective.Set(_keyText);
            Announce("НАЧАЛЬНИК МЁРТВ", _keyText.ToUpper(), _keyItem.transform.position);
            yield return new WaitUntil(() => _over || Vector2.Distance(_player.transform.position, _keyItem.transform.position) <= _pickupRadius);
            if (_over) yield break;
            N89.Core.Sfx.Play(N89.Core.Sfx.ReloadOk, _keyItem.transform.position);
            _keyItem.gameObject.SetActive(false);

            _objective.Set(_safeText);
            if (_waypoint != null) _waypoint.Show(_safe.position);
            yield return new WaitUntil(() => _over || Vector2.Distance(_player.transform.position, _safe.position) <= _pickupRadius + 0.4f);
            if (_waypoint != null) _waypoint.Hide();
            if (_over) yield break;

            // Сейф открывается, relic светится и уходит в руки Рэю
            SetControl(false);
            _player.CanAim = false;
            _objective.Set(string.Empty);
            StartCoroutine(Letterbox(120f, 0.5f));
            _cameraFollow.Focus(_safe, 0.5f);
            yield return Turn(_heroBody.transform, _safe.position, 0.3f);
            N89.Core.Sfx.Play(N89.Core.Sfx.Door, _safe.position);
            _safeBody.sprite = _safeOpen;
            _cameraFollow.Shake(0.05f, 0.15f);
            _relicItem.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.2f);

            Vector3 from = _relicItem.transform.position;
            Vector3 startScale = _relicItem.transform.localScale;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.6f);
                _relicItem.transform.position = Vector3.Lerp(from, _player.transform.position, k);
                _relicItem.transform.localScale = startScale * (1f - 0.8f * k);
                yield return null;
            }
            _relicItem.gameObject.SetActive(false);
            N89.Core.Sfx.Play(N89.Core.Sfx.HackDone, _player.transform.position);
            _fader.Flash(new Color(0.3f, 1f, 1f, 0.35f), 0.3f);
            if (_banner != null) _banner.Show(_relicTitle, _relicSubtitle, 3f);
            foreach (string line in _relicLines)
            {
                _subtitles.Say("РЭЙ", line);
                yield return new WaitForSeconds(1.5f);
            }

            yield return new WaitForSeconds(0.5f);
            StartCoroutine(Letterbox(0f, 0.5f));
            _cameraFollow.Focus(null, 0f);
        }

        // Герой вышел из кабины — двери соседнего лифта открываются, оттуда сразу идут на него
        private IEnumerator Ambush(UpperFloor floor)
        {
            yield return new WaitUntil(() => _over || !floor.ExitZone.Contains(_player.transform.position));
            if (_over) yield break;
            floor.AmbushDoor.Open();
            foreach (EnemyAI ai in floor.Ambushers)
            {
                if (ai != null && !ai.GetComponent<Health>().IsDead) ai.Engage(_player, _ambushReaction);
            }
        }

        private static void SetEnemiesActive(Health[] enemies, bool active)
        {
            foreach (Health enemy in enemies)
            {
                if (enemy != null && enemy.TryGetComponent(out EnemyAI ai)) ai.enabled = active;
            }
        }

        // Охрана у лестницы стоит вплотную — даём герою секунду осмотреться, прежде чем она оживёт
        private IEnumerator Floor2Grace()
        {
            yield return new WaitForSeconds(_floor2Grace);
            SetFloor2Active(true);
        }

        // Чтобы игрок точно понял, куда дальше: вспышка, крупная надпись и стрелка к цели
        private void Announce(string title, string subtitle, Vector2 waypoint)
        {
            _fader.Flash(new Color(0.3f, 1f, 1f, 0.35f), 0.3f);
            if (_banner != null) _banner.Show(title, subtitle, 3f);
            if (_waypoint != null) _waypoint.Show(waypoint);
        }

        private void MoveHero(Vector2 position, float facing)
        {
            Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            _player.transform.position = position;
            _heroBody.transform.rotation = Quaternion.Euler(0f, 0f, facing);
            _cameraFollow.SnapToTarget();
        }

        private void SetFloor2Active(bool active)
        {
            if (_floor2Enemies == null) return;
            foreach (Health enemy in _floor2Enemies)
            {
                if (enemy != null && enemy.TryGetComponent(out EnemyAI ai)) ai.enabled = active;
            }
        }

        private void SetControl(bool on)
        {
            _player.CanMove = on;
            _player.CanShoot = on;
            _hacking.HackingEnabled = on;
        }

        // ------------------------------------------------------------ Катсцена

        // Цель встаёт со стула и просит пощады — герой молча стреляет, цель падает лицом на стол
        private IEnumerator TargetCutscene()
        {
            _objective.Set(string.Empty);
            SetControl(false);
            _player.CanAim = false;
            Time.timeScale = 1f;
            foreach (Bullet bullet in FindObjectsByType<Bullet>()) Destroy(bullet.gameObject);

            var focus = new GameObject("CutsceneFocus").transform;
            focus.position = Vector2.Lerp(_player.transform.position, _target.position, 0.5f);
            _cameraFollow.Focus(focus, 0.25f);
            StartCoroutine(Letterbox(120f, 0.5f));

            // Герой разворачивается к цели
            yield return Turn(_heroBody.transform, _target.position, 0.35f);
            yield return new WaitForSeconds(0.3f);

            // Цель встаёт: отодвигает стул и поворачивается к герою
            if (_targetStanding != null) _targetBody.sprite = _targetStanding;
            Vector2 seat = _target.position;
            Vector2 stand = seat + (seat - _deskPoint).normalized * 0.3f;
            yield return Move(_target, seat, stand, 0.35f);
            yield return Turn(_targetBody.transform, _player.transform.position, 0.25f);

            foreach (string line in _targetLines)
            {
                _subtitles.Say(_targetSpeaker, line);
                yield return new WaitForSeconds(1.3f);
            }

            // Тянется к пистолету на столе, хватает и разворачивается к герою — тут его и снимают
            if (_targetGun != null)
            {
                Transform gun = _targetGun.transform;
                yield return Turn(_targetBody.transform, gun.position, 0.2f);
                yield return Move(gun, gun.position, (Vector2)_target.position + (Vector2)_targetBody.transform.right * 0.35f, 0.25f);
                gun.SetParent(_targetBody.transform, true);
                yield return Turn(_targetBody.transform, _player.transform.position, 0.2f);
                yield return new WaitForSeconds(0.15f);
            }

            // Выстрел
            Vector2 dir = ((Vector2)_target.position - (Vector2)_player.transform.position).normalized;
            _heroBody.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            Collider2D targetCollider = _target.GetComponent<Collider2D>();
            if (targetCollider != null) targetCollider.enabled = false;
            _weapon.ForceFire(dir);
            _cameraFollow.Shake(0.3f, 0.15f);
            yield return new WaitForSeconds(0.06f);

            // Попадание: синяя кровь, цель валится вперёд на стол
            if (_bloodFxPrefab != null)
            {
                Instantiate(_bloodFxPrefab, _target.position, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            }
            if (_targetDead != null) _targetBody.sprite = _targetDead;
            // Пистолет выпадает из руки и остаётся лежать
            if (_targetGun != null && _targetGun.transform.parent == _targetBody.transform)
            {
                _targetGun.transform.SetParent(_target.parent, true);
                _targetGun.transform.position = (Vector2)_target.position - dir * 0.5f;
            }
            // Лежит головой к дальнему краю стола
            Vector2 fall = _deskPoint - (Vector2)_target.position;
            _targetBody.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(fall.y, fall.x) * Mathf.Rad2Deg);
            yield return Move(_target, _target.position, _deskPoint, 0.25f);
            _cameraFollow.Shake(0.15f, 0.2f);

            // Кровь растекается по столу
            if (_bloodPoolPrefab != null)
            {
                GameObject pool = Instantiate(_bloodPoolPrefab, _deskPoint, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
                SpriteRenderer poolRenderer = pool.GetComponent<SpriteRenderer>();
                if (poolRenderer != null) poolRenderer.sortingOrder = _targetBody.sortingOrder - 1;
                StartCoroutine(Grow(pool.transform, 1.6f, 2.5f));
            }

            yield return new WaitForSeconds(2.2f);
            StartCoroutine(Letterbox(0f, 0.5f));
            _objective.Set(_clearedText);
            yield return new WaitForSeconds(1f);
        }

        // ------------------------------------------------------------ Отход

        // Цель выполнена, но миссия ещё нет: вниз по лестнице (если герой наверху / в подвале), на улицу
        // к машине — сел и улетел за кадр
        private IEnumerator Extract()
        {
            // Катсцена цели держала камеру на ней — возвращаем камеру герою
            _cameraFollow.Focus(null, 0f);
            SetControl(true);
            _player.CanAim = true;

            if (_returnSpawn != null && _returnZone.width > 0f)
            {
                _objective.Set(_returnText);
                Announce("ЦЕЛЬ ВЫПОЛНЕНА", _returnText.ToUpper(), _returnZone.center);
                yield return new WaitUntil(() => _returnZone.Contains(_player.transform.position));

                SetControl(false);
                yield return _fader.Fade(0f, 1f, 0.4f);
                if (_waypoint != null) _waypoint.Hide();
                MoveHero(_returnSpawn.position, _returnFacing);
                yield return new WaitForSeconds(0.2f);
                SetControl(true);
                yield return _fader.Fade(1f, 0f, 0.4f);
                _objective.Set(_carText);
                if (_waypoint != null) _waypoint.Show(_car.transform.position);
            }
            else
            {
                _objective.Set(_carText);
                Announce("ЦЕЛЬ ВЫПОЛНЕНА", _carText.ToUpper(), _car.transform.position);
            }

            yield return new WaitUntil(() => Vector2.Distance(_player.transform.position, _car.transform.position) <= _carReach);
            if (_waypoint != null) _waypoint.Hide();
            yield return FlyAway();
        }

        // Герой садится в машину, и она улетает за кадр (AirCar.TakeOff) — камера остаётся на месте
        private IEnumerator FlyAway()
        {
            SetControl(false);
            _player.CanAim = false;
            _objective.Set(string.Empty);
            _car.DisableColliders();

            var focus = new GameObject("CarFocus").transform;
            focus.position = _car.transform.position;
            _cameraFollow.Focus(focus, 0.5f);
            StartCoroutine(Letterbox(120f, 0.5f));

            // К ближайшей двери — и внутрь
            Vector2 door = _car.DoorFor(_player.transform.position);
            yield return Turn(_heroBody.transform, door, 0.15f);
            yield return Move(_player.transform, _player.transform.position, door, 0.35f);
            _player.gameObject.SetActive(false);
            _cameraFollow.Shake(0.05f, 0.15f);
            yield return new WaitForSeconds(0.6f);

            _cameraFollow.Shake(0.12f, 1.4f);
            yield return _car.TakeOff();

            yield return new WaitForSeconds(0.4f);
            yield return Letterbox(0f, 0.5f);
        }

        private static IEnumerator Turn(Transform body, Vector2 lookAt, float duration)
        {
            Vector2 dir = lookAt - (Vector2)body.position;
            Quaternion from = body.rotation;
            Quaternion to = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                body.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            body.rotation = to;
        }

        private static IEnumerator Move(Transform target, Vector2 from, Vector2 to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                target.position = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            target.position = to;
        }

        private static IEnumerator Grow(Transform target, float scale, float duration)
        {
            Vector3 from = target.localScale * 0.2f;
            Vector3 to = target.localScale * scale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                // Быстро вначале, медленно к концу — как растекающаяся лужа
                target.localScale = Vector3.Lerp(from, to, 1f - Mathf.Pow(1f - t / duration, 3f));
                yield return null;
            }
            target.localScale = to;
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

        // ------------------------------------------------------------ Смерть

        private void OnPlayerDied(Health health)
        {
            if (_over)
            {
                return;
            }

            _over = true;
            _dead = true;
            StartCoroutine(Restart());
        }

        // Перемотка к началу уровня (или ко второму этажу, если до него дошли). R — сразу, не дожидаясь
        private IEnumerator Restart()
        {
            Time.timeScale = 1f;
            _hacking.HackingEnabled = false;
            _objective.Set("[R] — ЗАНОВО");
            yield return new WaitForSeconds(0.6f);
            yield return _fader.Fade(0f, 1f, 0.3f);
            yield return _fader.ShowTitle("ЕЩЁ РАЗ", $"{_deathLines[Random.Range(0, _deathLines.Length)]}    [R]");
            yield return new WaitForSecondsRealtime(0.6f);
            Reload();
        }

        private void Reload()
        {
            s_restarted = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            // Быстрый рестарт: после смерти R перезапускает уровень мгновенно
            if (_dead && kb != null && kb.rKey.wasPressedThisFrame)
            {
                _dead = false;
                Reload();
                return;
            }
            // Esc — пауза (PauseMenu)
        }
    }
}
