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

        [Header("Миссия 3: спуск с relic и концовки (нет мессенджера — обычный отход к машине)")]
        [SerializeField] private MessengerPanel _messenger;
        [SerializeField] private string _bossContact = "БОСС";
        [SerializeField] private string _dialogTopic = "RELIC";
        [Tooltip("Переписка на третьем этаже; _dialogFromHero[i] — эту реплику пишет Рэй")]
        [SerializeField, TextArea] private string[] _dialogLines;
        [SerializeField] private bool[] _dialogFromHero;
        [Tooltip("0 — отдать relic, 1 — отказаться")]
        [SerializeField] private string[] _dialogChoices = { "Отдать relic", "Отказаться" };
        [Tooltip("Солдаты корпорации на этажах 2 и 1: неактивны, пока Рэй не откажется отдать relic")]
        [SerializeField] private GameObject _corpFloor2;
        [SerializeField] private GameObject _corpFloor1;
        [SerializeField] private string _corpTitle = "КОРПОРАЦИЯ В ЗДАНИИ";
        [SerializeField] private string _corpHint = "Прорвись вниз";
        [SerializeField] private string _corpText2 = "Прорвись через второй этаж";
        [SerializeField] private string _corpText1 = "Прорвись через первый этаж";
        [SerializeField] private string _descendText = "Спустись на лифте";
        [Tooltip("Машины Корпорации Е у входа: прилетают в катсцене после отказа (неактивны до неё)")]
        [SerializeField] private AirCar[] _corpCars = new AirCar[0];
        [Tooltip("Куда смотрит камера, пока прилетает корпорация")]
        [SerializeField] private Transform _streetCamPoint;
        [SerializeField] private string _corpArrivalLine = "Жаль, Рэй. Ты был хорошим псом.";
        [Tooltip("Рука Рэя (рукав и кисть, шарнир — в левом крае): в финале подносит relic к виску")]
        [SerializeField] private Sprite _heroArm;
        [Tooltip("Лежащий Рэй без правой руки (на время вставки relic)")]
        [SerializeField] private Sprite _heroDying;
        [Tooltip("Отряд у входа на улице: появляется в катсцене и стреляет без предупреждения")]
        [SerializeField] private GameObject _exitSquad;
        [Tooltip("Солдат отряда, который подходит и добивает (концовка с отказом)")]
        [SerializeField] private Transform _executioner;
        [Tooltip("Сразу за входной дверью снаружи: герой вышел — катсцена")]
        [SerializeField] private Rect _exitZone;
        [Tooltip("Куда Рэй ныряет обратно в здание под Сандевистаном")]
        [SerializeField] private Vector2 _insidePoint;
        [SerializeField] private Sprite _heroLying;
        [SerializeField] private string _exitText = "Выйди из здания";
        [SerializeField] private string _giveTitle = "ОКОНЧАТЕЛЬНАЯ СМЕРТЬ";
        [SerializeField] private string _giveSubtitle = "Relic у корпорации. Цепной пёс больше не нужен.";
        [SerializeField] private string[] _dyingLines = { "Вот и всё...", "Ну что, relic... посмотрим, на что ты годен." };
        [SerializeField] private string _endingScene = "Ending";

        private const string CorpCheckpoint2 = "mission03_corp_floor2";
        private const string CorpCheckpoint1 = "mission03_corp_floor1";

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

            // Миссия 3 после отказа отдать relic: корпорация заняла этажи 1–2, смерть отматывает к лифту этажа
            if (_corpFloor2 != null) _corpFloor2.SetActive(false);
            if (_corpFloor1 != null) _corpFloor1.SetActive(false);
            if (_exitSquad != null) _exitSquad.SetActive(false);
            foreach (AirCar corpCar in _corpCars) corpCar.gameObject.SetActive(false);
            int corpStage = Checkpoint.Is(CorpCheckpoint2) ? 2 : Checkpoint.Is(CorpCheckpoint1) ? 1 : 0;

            bool fromCheckpoint = (HasFloor2 || HasBasement) && (Checkpoint.Is(_checkpoint) || fromUpper || corpStage > 0);
            // После смерти — без заставки: сразу в бой
            bool restarted = s_restarted;
            s_restarted = false;
            if (corpStage > 0)
            {
                foreach (Health enemy in _enemies) enemy.gameObject.SetActive(false);
                foreach (Health enemy in _floor2Enemies) enemy.gameObject.SetActive(false);
                foreach (UpperFloor upper in _upperFloors)
                {
                    foreach (Health enemy in upper.Enemies) enemy.gameObject.SetActive(false);
                }
                if (corpStage == 2) _corpFloor2.SetActive(true);
                _corpFloor1.SetActive(true);
                PlaceCorpOutside();
                if (corpStage == 2) MoveHero(_floor2Spawn.position, _floor2Facing);
                else MoveHero(_returnSpawn.position, _returnFacing);
            }
            else if (fromUpper)
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
            if (corpStage > 0)
            {
                yield return Descent(corpStage);
                yield break;
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
            // Миссия 3: с relic — вниз, переписка с Боссом и концовка (сцена уходит сама, итогового экрана нет)
            if (_messenger != null)
            {
                yield return Descent(0);
                yield break;
            }
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
            yield return RideTo(floor.Spawn.position, floor.Facing, floor.Checkpoint);
        }

        // Лифт: затемнение, герой в кабине другого этажа, чекпоинт (если задан)
        private IEnumerator RideTo(Vector2 position, float facing, string checkpoint)
        {
            SetControl(false);
            yield return _fader.Fade(0f, 1f, 0.4f);
            MoveHero(position, facing);
            if (!string.IsNullOrEmpty(checkpoint)) Checkpoint.Set(checkpoint);
            yield return new WaitForSeconds(0.2f);
            SetControl(true);
            yield return _fader.Fade(1f, 0f, 0.4f);
        }

        // ------------------------------------------------------------ Миссия 3: спуск и концовки

        // stage 0 — с relic из лаборатории: лифт на третий этаж, там пишет Босс (выбор). Отдал — лифт вниз и смерть
        // у выхода. Отказал — корпорация заняла этажи 1–2: stage 2 — второй этаж, stage 1 — первый, потом выход
        private IEnumerator Descent(int stage)
        {
            _over = false;
            _cameraFollow.Focus(null, 0f);
            SetControl(true);
            _player.CanAim = true;

            if (stage == 0)
            {
                _objective.Set(_returnText);
                Announce("RELIC У ТЕБЯ", _returnText.ToUpper(), _returnZone.center);
                yield return new WaitUntil(() => _over || _returnZone.Contains(_player.transform.position));
                if (_waypoint != null) _waypoint.Hide();
                if (_over) yield break;
                yield return RideTo(_upperFloors[0].Spawn.position, _upperFloors[0].Facing, null);

                // Вибрация коммуникатора — пишет Босс
                SetControl(false);
                _objective.Set(string.Empty);
                yield return new WaitForSeconds(0.6f);
                _cameraFollow.Shake(0.05f, 0.2f);
                yield return new WaitForSeconds(0.5f);
                Cursor.visible = false;
                // Панель закрывает левую часть экрана — камера сдвигается, чтобы Рэй остался справа
                var chatFocus = new GameObject("ChatFocus").transform;
                chatFocus.position = (Vector2)_player.transform.position + new Vector2(-4.5f, 0f);
                _cameraFollow.Focus(chatFocus, 3f);
                yield return _messenger.ShowDialog(_bossContact, _dialogTopic, "РЭЙ", _dialogLines, _dialogFromHero, _dialogChoices);
                _cameraFollow.Focus(null, 0f);
                Destroy(chatFocus.gameObject);
                bool refused = _messenger.Choice == 1;

                if (!refused)
                {
                    // Отдал: лифт вниз, у выхода ждут
                    yield return RideTo(_returnSpawn.position, _returnFacing, null);
                    yield return GoOutside();
                    if (_over) yield break;
                    yield return GiveEnding();
                    yield break;
                }

                // Отказал: к башне прилетает корпорация, лифт встаёт на втором — она уже в здании
                yield return CorpArrival();
                _corpFloor2.SetActive(true);
                _corpFloor1.SetActive(true);
                yield return RideTo(_floor2Spawn.position, _floor2Facing, CorpCheckpoint2);
                if (_banner != null) _banner.Show(_corpTitle, _corpHint, 3.5f);
                stage = 2;
            }

            if (stage == 2)
            {
                yield return WaitCleared(_corpText2, _corpFloor2.GetComponentsInChildren<Health>(true));
                if (_over) yield break;
                _objective.Set(_descendText);
                Announce("ЭТАЖ ЗАЧИЩЕН", _descendText.ToUpper(), _floor2ExitZone.center);
                yield return new WaitUntil(() => _over || !_floor2ExitZone.Contains(_player.transform.position));
                yield return new WaitUntil(() => _over || _floor2ExitZone.Contains(_player.transform.position));
                if (_waypoint != null) _waypoint.Hide();
                if (_over) yield break;
                yield return RideTo(_returnSpawn.position, _returnFacing, CorpCheckpoint1);
            }

            yield return WaitCleared(_corpText1, _corpFloor1.GetComponentsInChildren<Health>(true));
            if (_over) yield break;
            yield return GoOutside();
            if (_over) yield break;
            yield return RefuseEnding();
        }

        // Отряд у входа стоит на постах, машины корпорации — на тротуаре; ИИ отряда выключен (он ждёт катсцену у выхода)
        private void PlaceCorpOutside()
        {
            foreach (AirCar corpCar in _corpCars) corpCar.gameObject.SetActive(true);
            _exitSquad.SetActive(true);
            foreach (EnemyAI ai in _exitSquad.GetComponentsInChildren<EnemyAI>()) ai.enabled = false;
        }

        // Катсцена после отказа: камера у входа в башню, прилетают машины Корпорации Е, из них выходят
        // солдаты и встают у дверей
        private IEnumerator CorpArrival()
        {
            SetControl(false);
            yield return _fader.Fade(0f, 1f, 0.4f);
            StartCoroutine(Letterbox(120f, 0.3f));
            _cameraFollow.CutTo(_streetCamPoint);

            // Солдаты прячутся, пока машины не сели
            PlaceCorpOutside();
            Transform[] soldiers = new Transform[_exitSquad.transform.childCount];
            Vector3[] posts = new Vector3[soldiers.Length];
            for (int i = 0; i < soldiers.Length; i++)
            {
                soldiers[i] = _exitSquad.transform.GetChild(i);
                posts[i] = soldiers[i].position;
                soldiers[i].gameObject.SetActive(false);
            }
            yield return _fader.Fade(1f, 0f, 0.5f);

            for (int i = 0; i < _corpCars.Length; i++)
            {
                Coroutine landing = StartCoroutine(_corpCars[i].Land());
                if (i == _corpCars.Length - 1) yield return landing;
                else yield return new WaitForSeconds(0.5f);
            }
            _cameraFollow.Shake(0.12f, 0.3f);
            N89.Core.Sfx.Play(N89.Core.Sfx.Door, _streetCamPoint.position);
            yield return new WaitForSeconds(0.4f);

            // Выпрыгивают из машин и встают полукругом у входа
            for (int i = 0; i < soldiers.Length; i++)
            {
                AirCar car = _corpCars[i % _corpCars.Length];
                Vector2 door = car.DoorFor(posts[i]);
                soldiers[i].position = door;
                soldiers[i].gameObject.SetActive(true);
                Vector2 dir = (Vector2)posts[i] - door;
                soldiers[i].Find("Body").rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                StartCoroutine(Move(soldiers[i], door, posts[i], 0.9f));
                yield return new WaitForSeconds(0.15f);
            }
            yield return new WaitForSeconds(1f);
            // Все лицом ко входу
            foreach (Transform soldier in soldiers)
            {
                Vector2 dir = (Vector2)_exitZone.center - (Vector2)soldier.position;
                soldier.Find("Body").rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
            if (_corpArrivalLine.Length > 0) _subtitles.Say(_bossContact, _corpArrivalLine);
            yield return new WaitForSeconds(2.2f);

            // Затемняет и возвращает камеру к герою уже лифт (RideTo)
            StartCoroutine(Letterbox(0f, 0.3f));
            _cameraFollow.Focus(null, 0f);
        }

        private IEnumerator GoOutside()
        {
            // Без баннера: он висел бы поверх катсцены у входа — только задача и стрелка
            _objective.Set(_exitText);
            if (_waypoint != null) _waypoint.Show(_exitZone.center);
            yield return new WaitUntil(() => _over || _exitZone.Contains(_player.transform.position));
            if (_waypoint != null) _waypoint.Hide();
        }

        // Отряд у входа: появляется, вскидывает оружие и стреляет залпами без предупреждения
        private IEnumerator SquadOpensFire(int volleys)
        {
            _exitSquad.SetActive(true);
            EnemyAI[] squad = _exitSquad.GetComponentsInChildren<EnemyAI>();
            foreach (EnemyAI ai in squad) ai.enabled = false;
            for (int v = 0; v < volleys; v++)
            {
                foreach (EnemyAI ai in squad)
                {
                    Vector2 dir = (Vector2)(_player.transform.position - ai.transform.position);
                    ai.transform.Find("Body").rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    if (ai.TryGetComponent(out Weapon weapon)) weapon.ForceFire(dir.normalized);
                }
                _cameraFollow.Shake(0.15f, 0.15f);
                yield return new WaitForSecondsRealtime(0.18f);
            }
        }

        // Кровь на герое и красная вспышка — попали, но герой (пока) жив
        private void HeroHit(Vector2 from)
        {
            Vector2 dir = ((Vector2)_player.transform.position - from).normalized;
            if (_bloodFxPrefab != null) Instantiate(_bloodFxPrefab, _player.transform.position, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            _fader.Flash(new Color(1f, 0.1f, 0.15f, 0.5f), 0.4f);
            N89.Core.Sfx.Play(N89.Core.Sfx.Hit, _player.transform.position);
        }

        private void BeginEnding()
        {
            _over = true;
            SetControl(false);
            _player.CanAim = false;
            Time.timeScale = 1f;
            _objective.Set(string.Empty);
            // Пули отряда не должны «по-настоящему» убить героя — смерть здесь сценарная
            foreach (Collider2D col in _player.GetComponents<Collider2D>()) col.enabled = false;
            if (_player.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
            StartCoroutine(Letterbox(120f, 0.5f));
            var focus = new GameObject("EndingFocus").transform;
            focus.position = Vector2.Lerp(_player.transform.position, _exitSquad.transform.position, 0.6f);
            _cameraFollow.Focus(focus, 3f);
        }

        // Отдал relic: залп — Рэй падает. Окончательная смерть
        private IEnumerator GiveEnding()
        {
            BeginEnding();
            yield return new WaitForSeconds(0.4f);
            StartCoroutine(SquadOpensFire(4));
            yield return new WaitForSeconds(0.1f);
            for (int i = 0; i < 3; i++)
            {
                HeroHit(_exitSquad.transform.position);
                yield return new WaitForSeconds(0.15f);
            }
            LayHeroDown(_exitSquad.transform.position);
            N89.Core.Sfx.Play(N89.Core.Sfx.Death, _player.transform.position);
            yield return new WaitForSeconds(2.5f);
            yield return _fader.Fade(0f, 1f, 1.2f);
            yield return _fader.ShowTitle(_giveTitle, _giveSubtitle);
            yield return new WaitForSecondsRealtime(4f);
            Checkpoint.Clear();
            SceneManager.LoadScene(_menuScene);
        }

        // Отказал: залп, Рэй ранен — Сандевистан, назад в здание; лёжа вставляет relic, солдат добивает
        private IEnumerator RefuseEnding()
        {
            BeginEnding();
            yield return new WaitForSeconds(0.3f);
            StartCoroutine(SquadOpensFire(6));
            yield return new WaitForSeconds(0.1f);
            HeroHit(_exitSquad.transform.position);
            yield return new WaitForSeconds(0.2f);
            HeroHit(_exitSquad.transform.position);

            // Сандевистан — мир замирает, Рэй ныряет обратно в здание
            if (_sandevistan != null) _sandevistan.ForceActivate();
            Vector2 from = _player.transform.position;
            Vector2 to = _insidePoint;
            Vector2 run = to - from;
            // Лицом к двери: и корпус, и ноги — по направлению бега (ноги сами крутятся за скоростью тела)
            Quaternion facing = Quaternion.Euler(0f, 0f, Mathf.Atan2(run.y, run.x) * Mathf.Rad2Deg);
            Transform legs = _player.transform.Find("Legs");
            Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();
            for (float t = 0f; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                Vector2 p = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / 0.9f));
                rb.position = p;
                rb.linearVelocity = run.normalized * 6f;
                _player.transform.position = p;
                _heroBody.transform.rotation = facing;
                if (legs != null) legs.rotation = facing;
                yield return null;
            }
            rb.linearVelocity = Vector2.zero;
            _cameraFollow.Focus(null, 0f);
            yield return new WaitUntil(() => _sandevistan == null || !_sandevistan.IsActive);
            Time.timeScale = 1f;

            // Падает, тяжело ранен
            LayHeroDown(_exitSquad.transform.position);
            _cameraFollow.Shake(0.1f, 0.2f);
            yield return new WaitForSeconds(1f);
            foreach (string line in _dyingLines)
            {
                _subtitles.Say("РЭЙ", line);
                yield return new WaitForSeconds(2f);
            }

            // Вставляет relic: рука с чипом медленно поднимается от бедра к виску, чип входит в разъём
            yield return InsertRelic();
            yield return new WaitForSeconds(1.2f);

            // Солдат медленно подходит и стреляет в голову
            Transform killer = _executioner;
            Vector2 stand = (Vector2)_player.transform.position + new Vector2(0.9f, -0.6f);
            Vector2 walk = stand - (Vector2)killer.position;
            killer.Find("Body").rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(walk.y, walk.x) * Mathf.Rad2Deg);
            var killerFocus = new GameObject("KillerFocus").transform;
            killerFocus.position = Vector2.Lerp(_player.transform.position, killer.position, 0.5f);
            _cameraFollow.Focus(killerFocus, 2f);
            yield return Move(killer, killer.position, stand, 3.5f);
            Vector2 aim = ((Vector2)_player.transform.position - stand).normalized;
            killer.Find("Body").rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            yield return new WaitForSeconds(1.5f);
            if (killer.TryGetComponent(out Weapon gun)) gun.ForceFire(aim);
            Vector2 head = (Vector2)_player.transform.position + (Vector2)_heroBody.transform.right * 0.34f;
            if (_bloodFxPrefab != null) Instantiate(_bloodFxPrefab, head, Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg));
            _fader.SetBlack(1f);
            yield return new WaitForSecondsRealtime(2f);
            Checkpoint.Clear();
            SceneManager.LoadScene(_endingScene);
        }

        // Рука лежащего Рэя (спрайт трупа: голова — вправо по корпусу, плечо — чуть ниже оси) поднимает relic к виску.
        // Шарнир — у плеча; рука идёт дугой снаружи тела, от бедра к голове
        private IEnumerator InsertRelic()
        {
            Transform body = _heroBody.transform;
            int order = _heroBody.sortingOrder;
            // Тот же лежащий Рэй, но правой руки на спрайте нет — её рисуем отдельно
            if (_heroDying != null) _heroBody.sprite = _heroDying;
            var pivot = new GameObject("ReachArm").transform;
            pivot.SetParent(body, false);
            pivot.localPosition = new Vector3(0.16f, -0.14f, 0f);

            const float armLength = 0.3f;
            var arm = new GameObject("Arm").AddComponent<SpriteRenderer>();
            arm.transform.SetParent(pivot, false);
            arm.sprite = _heroArm;
            arm.sortingOrder = order + 1;
            if (_heroArm != null) arm.transform.localScale = Vector3.one * (armLength / _heroArm.bounds.size.x);
            arm.transform.localPosition = new Vector3(armLength * 0.5f, 0f, 0f);

            var relic = new GameObject("RelicInHand").AddComponent<SpriteRenderer>();
            relic.transform.SetParent(pivot, false);
            relic.sprite = _relicItem.sprite;
            relic.sortingOrder = order + 2;
            relic.transform.localPosition = new Vector3(armLength, 0f, 0f);
            float relicScale = 0.32f / _relicItem.sprite.bounds.size.x;
            relic.transform.localScale = new Vector3(relicScale / body.lossyScale.x, relicScale / body.lossyScale.y, 1f);
            var glow = new GameObject("Glow").AddComponent<Light2D>();
            glow.transform.SetParent(relic.transform, false);
            glow.lightType = Light2D.LightType.Point;
            glow.color = new Color(0.3f, 1f, 1f);
            glow.pointLightOuterRadius = 1.2f;
            glow.intensity = 0.8f;

            // Достаёт: рука у бедра, потом медленно, с дрожью, тянется к голове
            const float fromAngle = 200f, toAngle = 375f;
            pivot.localRotation = Quaternion.Euler(0f, 0f, fromAngle);
            yield return new WaitForSeconds(0.6f);
            for (float t = 0f; t < 2.2f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 2.2f);
                float tremble = Mathf.Sin(t * 23f) * 3f * (1f - k);
                pivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(fromAngle, toAngle, k) + tremble);
                glow.intensity = 0.8f + 0.6f * k;
                yield return null;
            }
            yield return new WaitForSeconds(0.4f);

            // Щелчок разъёма
            N89.Core.Sfx.Play(N89.Core.Sfx.HackDone, relic.transform.position);
            _fader.Flash(new Color(0.3f, 1f, 1f, 0.5f), 0.5f);
            _cameraFollow.Shake(0.08f, 0.2f);
            Destroy(relic.gameObject);
            yield return new WaitForSeconds(0.6f);

            // Рука бессильно падает
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                pivot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(toAngle, 300f, Mathf.SmoothStep(0f, 1f, t / 0.5f)));
                yield return null;
            }
        }

        // Рэй лежит навзничь, головой от стрелков
        private void LayHeroDown(Vector2 shooters)
        {
            if (_heroLying != null) _heroBody.sprite = _heroLying;
            Vector2 away = ((Vector2)_player.transform.position - shooters).normalized;
            _heroBody.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg);
            Transform legs = _player.transform.Find("Legs");
            if (legs != null) legs.gameObject.SetActive(false);
            if (_bloodPoolPrefab != null) Instantiate(_bloodPoolPrefab, _player.transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
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
