using System.Collections;
using N89.Player;
using N89.UI;
using N89.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace N89.Interlude
{
    // Интерлюдия перед миссией 1: квартира героя (кличка — Рэй). Он спит на диване, ворочается,
    // просыпается, садится и встаёт — дальше игрок свободно ходит по квартире.
    // Цель — зайти в ванную; там босс присылает брифинг в мессенджер. Потом цель — арсенал.
    // Продолжение — дальше по сценарию.
    public class InterludeDirector : MonoBehaviour
    {
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
            Transform root = _hero.transform;
            Transform body = _heroBody.transform;
            Rigidbody2D rb = _hero.GetComponent<Rigidbody2D>();
            Sprite standing = _heroBody.sprite;

            // Пока идёт катсцена, герой — просто картинка на диване: физика и управление выключены
            _hero.CanMove = false;
            _hero.CanAim = false;
            rb.simulated = false;
            _heroBody.sprite = _sleepingSprite;
            root.position = _sleepPoint.position;
            body.rotation = _sleepPoint.rotation;
            _cameraFollow.CutTo(_sleepPoint);
            Camera.main.orthographicSize = _closeZoom;
            SetLetterbox(_letterboxHeight);
            _objective.Set(string.Empty);
            _crosshair.SetActive(false);
            Cursor.visible = false;
            _fader.SetBlack(1f);

            yield return new WaitForSeconds(0.5f);
            yield return _fader.ShowTitle(_title, _subtitle);
            yield return new WaitForSeconds(1.2f);
            yield return _fader.HideTitle(0.6f);
            yield return _fader.Fade(1f, 0f, 2.5f);

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

            // Встаёт с дивана
            _heroBody.sprite = standing;
            body.rotation = _standPoint.rotation;
            yield return Move(root, _sitPoint.position, _standPoint.position, 0.45f);

            _cameraFollow.Focus(null, 0f);
            StartCoroutine(_cameraFollow.Zoom(_roamZoom, 1.5f));
            yield return Letterbox(0f, 0.6f);

            rb.simulated = true;
            _hero.CanMove = true;
            _hero.CanAim = true;
            _crosshair.SetActive(true);

            _objective.Set(_bathroomObjective);
            yield return new WaitUntil(() => _bathroomZone.WasEntered);
            _objective.Set(string.Empty);
            yield return BossBriefing(rb);

            _objective.Set(_arsenalObjective);
            yield return new WaitUntil(() => _arsenalZone.PlayerInside);

            // Дальше — арсенал (будет описано позже)
        }

        // Босс пишет в мессенджер: панель закрывает зал, камера сдвигается, чтобы ванная была справа.
        // Рэй не отвечает — игрок закрывает переписку на Enter
        private IEnumerator BossBriefing(Rigidbody2D rb)
        {
            _hero.CanMove = false;
            _hero.CanAim = false;
            rb.linearVelocity = Vector2.zero;
            _crosshair.SetActive(false);
            Cursor.visible = false;
            _cameraFollow.Focus(_messageCamPoint, 4f);

            yield return new WaitForSeconds(0.4f);
            yield return _messenger.Show(_bossName, _briefingTopic, _briefing, _briefingFile);

            _cameraFollow.Focus(null, 0f);
            _hero.CanMove = true;
            _hero.CanAim = true;
            _crosshair.SetActive(true);
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
