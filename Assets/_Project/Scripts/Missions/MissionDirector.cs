using System.Collections;
using N89.Combat;
using N89.Core;
using N89.Enemies;
using N89.Hacking;
using N89.Player;
using N89.UI;
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
        private const string TargetSpeaker = "ГЛАВАРЬ";

        [Header("Hero")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private HackingController _hacking;
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
        [SerializeField] private float _floor2Grace = 1f;
        [Tooltip("Кабинет цели: герой внутри после зачистки — катсцена")]
        [SerializeField] private Rect _officeZone;
        [Tooltip("Куда указывает стрелка после зачистки второго этажа — дверь кабинета")]
        [SerializeField] private Vector2 _officeDoor;
        [SerializeField] private Banner _banner;
        [SerializeField] private WaypointArrow _waypoint;
        [SerializeField] private string _stairsText = "Поднимись на второй этаж";
        [SerializeField] private string _floor2Text = "Убей всех, кроме цели";
        [SerializeField] private string _officeText = "Зайди в кабинет цели";
        [SerializeField] private string _checkpoint = "mission01_floor2";

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
        private bool HasFloor2 => _floor2Enemies != null && _floor2Enemies.Length > 0 && _target != null;

        private IEnumerator Start()
        {
            ApplyLoadout();
            _player.Health.Died += OnPlayerDied;
            if (_stairsMarker != null) _stairsMarker.enabled = false;

            _player.CanMove = false;
            _player.CanShoot = false;
            _hacking.HackingEnabled = false;
            _hackingHud.RamVisible = true;
            _objective.Set(string.Empty);
            _fader.SetBlack(1f);

            // Враги второго этажа ждут, пока герой не поднимется
            SetFloor2Active(false);

            bool fromCheckpoint = HasFloor2 && Checkpoint.Is(_checkpoint);
            if (fromCheckpoint)
            {
                // Смерть на втором этаже: первый уже зачищен
                foreach (Health enemy in _enemies) enemy.gameObject.SetActive(false);
                MoveHero(_floor2Spawn.position, _floor2Facing);
                yield return new WaitForSeconds(0.3f);
            }
            else
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
            yield return _fader.Fade(1f, 0f, fromCheckpoint ? 0.5f : 1f);
            if (fromCheckpoint)
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

                yield return WaitCleared(_floor2Text, _floor2Enemies);
                if (_over) yield break;

                _objective.Set(_officeText);
                Announce("ЭТАЖ ЗАЧИЩЕН", _officeText.ToUpper(), _officeDoor);
                yield return new WaitUntil(() => _over || _officeZone.Contains(_player.transform.position));
                if (_waypoint != null) _waypoint.Hide();
                if (_over) yield break;

                _over = true;
                yield return TargetCutscene();
                Checkpoint.Clear();
            }
            else
            {
                _over = true;
                _objective.Set(_clearedText);
                yield return new WaitForSeconds(1.5f);
            }

            Time.timeScale = 1f;
            _player.CanShoot = false;
            _hacking.HackingEnabled = false;
            yield return _endScreen.Show();
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
            // С этого момента смерть отматывает ко второму этажу
            Checkpoint.Set(_checkpoint);
            yield return new WaitForSeconds(0.2f);
            SetControl(true);
            yield return _fader.Fade(1f, 0f, 0.4f);
            yield return Floor2Grace();
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
                _subtitles.Say(TargetSpeaker, line);
                yield return new WaitForSeconds(1.3f);
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
            StartCoroutine(Restart());
        }

        // Перемотка к началу уровня (или ко второму этажу, если до него дошли)
        private IEnumerator Restart()
        {
            Time.timeScale = 1f;
            _hacking.HackingEnabled = false;
            _objective.Set(string.Empty);
            yield return new WaitForSeconds(0.8f);
            yield return _fader.Fade(0f, 1f, 0.4f);
            yield return _fader.ShowTitle("ЕЩЁ РАЗ", _deathLines[Random.Range(0, _deathLines.Length)]);
            yield return new WaitForSecondsRealtime(0.9f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                Checkpoint.Clear();
                SceneManager.LoadScene(_menuScene);
            }
        }
    }
}
