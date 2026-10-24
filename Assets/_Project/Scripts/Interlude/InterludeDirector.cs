using System.Collections;
using N89.Player;
using N89.UI;
using N89.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace N89.Interlude
{
    // Интерлюдия в квартире героя (кличка — Рэй). Начало зависит от _opening:
    //  Sleep (перед миссией 1) — спит на диване, ворочается, просыпается и встаёт; цель — зайти
    //  в ванную, там босс присылает брифинг в мессенджер;
    //  WatchTv (перед миссией 2) — сидит на диване и смотрит телевизор, босс пишет прямо туда,
    //  после брифинга Рэй встаёт.
    // Дальше одинаково: арсенал (выбор оружия и импланта на миссию), входная дверь, коридор
    // мегаблока, в конце коридора экран загрузки миссии.
    public class InterludeDirector : MonoBehaviour
    {
        public enum Opening { Sleep, WatchTv }

        [Header("Начало")]
        [SerializeField] private Opening _opening = Opening.Sleep;
        [Tooltip("WatchTv: откуда камера смотрит в начале — диван и телевизор в кадре")]
        [SerializeField] private Transform _tvCamPoint;
        [Tooltip("WatchTv: сколько Рэй смотрит телевизор, пока камера наезжает на него")]
        [SerializeField] private float _tvDuration = 6f;

        [Header("Hero")]
        [SerializeField] private PlayerController _hero;
        [SerializeField] private SpriteRenderer _heroBody;
        [SerializeField] private CameraFollow _cameraFollow;

        [Header("Пробуждение")]
        [Tooltip("Лежит на спине, головой влево (-x)")]
        [SerializeField] private Sprite _sleepingSprite;
        [Tooltip("Сидит, смотрит вправо (+x); поворот берётся из _sitPoint")]
        [SerializeField] private Sprite _sittingSprite;
        [SerializeField] private Transform _sleepPoint;
        [SerializeField] private Transform _sitPoint;
        [SerializeField] private Transform _standPoint;
        [SerializeField] private float _sleepDuration = 4f;
        [SerializeField] private float _closeZoom = 3f;
        [SerializeField] private float _roamZoom = 5f;

        [Header("Цели")]
        [SerializeField] private string _bathroomObjective = "Зайди в ванную";
        [SerializeField] private string _arsenalObjective = "Иди в арсенал";
        [SerializeField] private TriggerZone _arsenalZone;

        [Header("Арсенал")]
        [SerializeField] private ArsenalPanel _arsenal;
        [Tooltip("Перед какой миссией эта интерлюдия: от номера зависит доступный имплант")]
        [SerializeField] private int _mission = 1;

        [Header("Выход на миссию")]
        [SerializeField] private Door _frontDoor;
        [Tooltip("Площадка у входной двери изнутри: подошёл — дверь открывается")]
        [SerializeField] private TriggerZone _doorZone;
        [Tooltip("Конец коридора за дверью: дошёл — грузится миссия")]
        [SerializeField] private TriggerZone _exitZone;
        [SerializeField] private LoadingScreen _loading;
        [SerializeField] private string _doorObjective = "Иди к двери";
        [SerializeField] private string _leaveObjective = "Выйди из коридора";
        [SerializeField] private string _missionScene = "Mission01";
        [SerializeField] private string _missionTitle = "МИССИЯ 1";
        [SerializeField] private string _missionSubtitle = "ЛОГОВО БАНДЫ  //  ЗАЧИСТИТЬ ВСЕХ";

        [Header("Брифинг от босса")]
        [SerializeField] private TriggerZone _bathroomZone;
        [SerializeField] private MessengerPanel _messenger;
        [Tooltip("Куда смотрит камера, пока открыт мессенджер: панель закрывает зал, ванная остаётся справа")]
        [SerializeField] private Transform _messageCamPoint;
        [SerializeField] private string _bossName = "БОСС";
        [SerializeField] private string _briefingTopic = "ЗАДАНИЕ: ЛОГОВО БАНДЫ";
        [SerializeField, TextArea] private string[] _briefing =
        {
            "Рэй. Есть работа.",
            "Одна уличная банда уже который месяц грабит и громит имущество Корпорации Е. Склады, конвои, всё, до чего дотянется.",
            "Наверху это надоело. Пойдёшь в их логово и уберёшь всех. Без исключений.",
            "Местоположение — в файле. Не подведи.",
        };
        [SerializeField] private string _briefingFile = "ЛОГОВО_БАНДЫ.loc";

        [Header("UI")]
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private ObjectivePanel _objective;
        [Tooltip("Перекрестие: в катсцене спрятано, появляется вместе с управлением")]
        [SerializeField] private GameObject _crosshair;
        [SerializeField] private RectTransform _letterboxTop;
        [SerializeField] private RectTransform _letterboxBottom;
        [SerializeField] private float _letterboxHeight = 120f;
        [SerializeField] private string _title = "ИНТЕРЛЮДИЯ I";
        [SerializeField] private string _subtitle = "Дом";
        [SerializeField] private string _menuScene = "MainMenu";

        private IEnumerator Start()
        {
            Rigidbody2D rb = _hero.GetComponent<Rigidbody2D>();
            Sprite standing = _heroBody.sprite;

            // Пока идёт катсцена, герой — просто картинка на диване: физика и управление выключены
            rb.simulated = false;
            SetControl(false, rb);
            SetLetterbox(_letterboxHeight);
            _objective.Set(string.Empty);
            Cursor.visible = false;
            _fader.SetBlack(1f);

            if (_opening == Opening.WatchTv)
            {
                yield return WatchTv();
                yield return StandUp(rb, standing);
            }
            else
            {
                yield return WakeUp();
                yield return StandUp(rb, standing);

                _objective.Set(_bathroomObjective);
                yield return new WaitUntil(() => _bathroomZone.WasEntered);
                _objective.Set(string.Empty);
                SetControl(false, rb);
                yield return BossBriefing();
                SetControl(true, rb);
            }

            _objective.Set(_arsenalObjective);
            yield return new WaitUntil(() => _arsenalZone.PlayerInside);
            _objective.Set(string.Empty);
            yield return ChooseLoadout(rb);

            // К двери, через коридор мегаблока — и загрузка миссии
            _objective.Set(_doorObjective);
            yield return new WaitUntil(() => _doorZone.PlayerInside);
            _frontDoor.Open();
            _objective.Set(_leaveObjective);
            yield return new WaitUntil(() => _exitZone.WasEntered);

            _objective.Set(string.Empty);
            SetControl(false, rb);
            yield return _loading.Load(_missionScene, _missionTitle, _missionSubtitle);
        }

        private IEnumerator ShowTitle()
        {
            yield return new WaitForSeconds(0.5f);
            yield return _fader.ShowTitle(_title, _subtitle);
            yield return new WaitForSeconds(1.2f);
            yield return _fader.HideTitle(0.6f);
            yield return _fader.Fade(1f, 0f, 2.5f);
        }

        // Перед миссией 1: спит на диване, ворочается, просыпается и садится
        private IEnumerator WakeUp()
        {
            Transform root = _hero.transform;
            Transform body = _heroBody.transform;
            _heroBody.sprite = _sleepingSprite;
            root.position = _sleepPoint.position;
            body.rotation = _sleepPoint.rotation;
            _cameraFollow.CutTo(_sleepPoint);
            Camera.main.orthographicSize = _closeZoom;

            yield return ShowTitle();

            // Спит, потом ворочается — сон беспокойный
            yield return Breathe(body, _sleepDuration);
            yield return Toss(body, 0.3f, 4f);
            yield return Breathe(body, 1.4f);
            yield return Toss(body, 0.4f, 7f);

            // Просыпается и садится
            _heroBody.sprite = _sittingSprite;
            root.position = _sitPoint.position;
            body.rotation = _sitPoint.rotation;
            _cameraFollow.Shake(0.06f, 0.15f);
            yield return new WaitForSeconds(1.8f);
        }

        // Перед миссией 2: сидит на диване лицом к телевизору, камера медленно наезжает на него.
        // Приходит сообщение — брифинг прямо на диване
        private IEnumerator WatchTv()
        {
            _heroBody.sprite = _sittingSprite;
            _hero.transform.position = _sitPoint.position;
            _heroBody.transform.rotation = _sitPoint.rotation;
            _cameraFollow.CutTo(_tvCamPoint);
            Camera.main.orthographicSize = _roamZoom;

            yield return ShowTitle();

            _cameraFollow.Focus(_sitPoint, 0.5f);
            yield return _cameraFollow.Zoom(_closeZoom, _tvDuration);

            // Вибрация импланта-коммуникатора: сообщение от босса
            _cameraFollow.Shake(0.05f, 0.2f);
            yield return new WaitForSeconds(0.6f);
            yield return BossBriefing();
            _cameraFollow.Focus(_sitPoint, 4f);
            yield return new WaitForSeconds(0.5f);
        }

        // Встаёт с дивана, камера отъезжает, управление — игроку
        private IEnumerator StandUp(Rigidbody2D rb, Sprite standing)
        {
            _heroBody.sprite = standing;
            _heroBody.transform.rotation = _standPoint.rotation;
            yield return Move(_hero.transform, _sitPoint.position, _standPoint.position, 0.45f);

            _cameraFollow.Focus(null, 0f);
            StartCoroutine(_cameraFollow.Zoom(_roamZoom, 1.5f));
            yield return Letterbox(0f, 0.6f);

            rb.simulated = true;
            SetControl(true, rb);
        }

        private void SetControl(bool enabled, Rigidbody2D rb)
        {
            _hero.CanMove = enabled;
            _hero.CanAim = enabled;
            if (!enabled) rb.linearVelocity = Vector2.zero;
            _crosshair.SetActive(enabled);
        }

        // Выбор оружия и импланта на миссию; выбор сохраняется в Loadout
        private IEnumerator ChooseLoadout(Rigidbody2D rb)
        {
            SetControl(false, rb);
            Cursor.visible = true;

            yield return _arsenal.Show(_mission);

            Cursor.visible = false;
            SetControl(true, rb);
        }

        // Босс пишет в мессенджер: панель закрывает левую часть экрана, камера сдвигается,
        // чтобы Рэй остался справа. Он не отвечает — игрок закрывает переписку на Enter
        private IEnumerator BossBriefing()
        {
            Cursor.visible = false;
            _cameraFollow.Focus(_messageCamPoint, 4f);

            yield return new WaitForSeconds(0.4f);
            yield return _messenger.Show(_bossName, _briefingTopic, _briefing, _briefingFile);

            _cameraFollow.Focus(null, 0f);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(_menuScene);
            }
        }

        // Грудь медленно поднимается и опускается
        private static IEnumerator Breathe(Transform body, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                body.localScale = new Vector3(1f, 1f + 0.035f * Mathf.Sin(t * 2.2f), 1f);
                yield return null;
            }
            body.localScale = Vector3.one;
        }

        // Дёргается во сне: короткое покачивание корпуса
        private static IEnumerator Toss(Transform body, float duration, float angle)
        {
            Quaternion rest = body.rotation;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                body.rotation = rest * Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 3f) * angle * (1f - k));
                yield return null;
            }
            body.rotation = rest;
        }

        private static IEnumerator Move(Transform target, Vector3 from, Vector3 to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                target.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            target.position = to;
        }

        private IEnumerator Letterbox(float height, float duration)
        {
            float from = _letterboxTop.sizeDelta.y;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetLetterbox(Mathf.Lerp(from, height, Mathf.SmoothStep(0f, 1f, t / duration)));
                yield return null;
            }
            SetLetterbox(height);
        }

        private void SetLetterbox(float height)
        {
            _letterboxTop.sizeDelta = new Vector2(0f, height);
            _letterboxBottom.sizeDelta = new Vector2(0f, height);
        }
    }
}
