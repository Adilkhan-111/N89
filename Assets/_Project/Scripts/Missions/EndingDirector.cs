using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace N89.Missions
{
    // Финал игры (концовка «не отдал relic»): Рэй очнулся на свалке — без рук и имплантов. Темнота медленно
    // расходится, в голове пара мыслей, дальше игрок сам ползёт (WASD, медленно, рывками) вперёд, на свет.
    // Дополз — экран заливает белым, титр, главное меню.
    public class EndingDirector : MonoBehaviour
    {
        [SerializeField] private Transform _hero;
        [SerializeField] private Transform _heroBody;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _crawlSpeed = 1f;
        [Tooltip("Где можно ползать (мир)")]
        [SerializeField] private Rect _area;
        [SerializeField] private Transform _lightPoint;
        [SerializeField] private Light2D _light;
        [SerializeField] private float _reach = 1.4f;
        [Tooltip("Кадры ползания (hero_crawl_0..3): подтянул ноги, толкнулся левой, вытянулся, толкнулся правой")]
        [SerializeField] private Sprite[] _crawlFrames;
        [Tooltip("Кровавый след за Рэем")]
        [SerializeField] private Sprite _bloodSmear;
        [SerializeField] private float _smearStep = 0.45f;
        [SerializeField] private N89.UI.WaypointArrow _waypoint;

        [Header("UI")]
        [SerializeField] private CanvasGroup _black;
        [SerializeField] private CanvasGroup _white;
        [SerializeField] private TMP_Text _thought;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private string[] _thoughts = { "...", "Я... жив?", "Рук нет. Имплантов нет. Ничего нет.", "Но я помню. Меня зовут Брайс." };
        [SerializeField] private string _hint = "WASD — ползти к свету";
        [SerializeField] private string _endTitle = "N89";
        [SerializeField] private string _endSubtitle = "Брайс жив.";
        [SerializeField] private string _menuScene = "MainMenu";

        private float _lightBase;

        private IEnumerator Start()
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            _black.alpha = 1f;
            _white.alpha = 0f;
            _thought.text = string.Empty;
            _title.text = _endTitle;
            _subtitle.text = _endSubtitle;
            _title.alpha = 0f;
            _subtitle.alpha = 0f;
            _lightBase = _light.intensity;
            FollowCamera(1f);

            yield return new WaitForSeconds(1.5f);
            // Глаза открываются: темнота расходится рывками
            for (int i = 0; i < 3; i++)
            {
                yield return Fade(_black, 1f, 0.5f, 0.6f);
                yield return Fade(_black, 0.5f, 0.9f, 0.4f);
            }
            yield return Fade(_black, 0.9f, 0f, 2.5f);

            foreach (string line in _thoughts)
            {
                yield return ShowThought(line, 2.2f);
            }
            _thought.text = _hint;
            _thought.alpha = 0.7f;
            if (_waypoint != null) _waypoint.Show(_lightPoint.position);

            // Ползёт к свету
            bool hintShown = true;
            float crawl = 0f;
            Vector2 lastSmear = _hero.position;
            SpriteRenderer bodySprite = _heroBody.GetComponent<SpriteRenderer>();
            Vector3 baseScale = _heroBody.localScale;
            int lastPull = -1;
            float nextBeat = 0f;
            while (Vector2.Distance(_hero.position, _lightPoint.position) > _reach)
            {
                // Сердце бьётся глухо и неровно; чем ближе свет, тем реже и тише — он угасает
                float near = 1f - Mathf.Clamp01(Vector2.Distance(_hero.position, _lightPoint.position) / 12f);
                if (Time.time >= nextBeat)
                {
                    N89.Core.Sfx.Ui(N89.Core.Sfx.Heartbeat, Mathf.Lerp(0.9f, 0.4f, near));
                    nextBeat = Time.time + Mathf.Lerp(1.1f, 1.8f, near) + Random.Range(-0.1f, 0.25f);
                }

                Vector2 input = ReadMove();
                float pull = 0f;
                if (input.sqrMagnitude > 0.01f)
                {
                    if (hintShown && crawl > 1.5f)
                    {
                        hintShown = false;
                        StartCoroutine(Fade(_thought, 0.7f, 0f, 1f));
                    }
                    crawl += Time.deltaTime;
                    // Рывками: толкнулся ногой — тело проезжает вперёд, подтянул ноги — пауза
                    pull = Mathf.Max(0f, Mathf.Sin(crawl * 5f));
                    Vector2 next = (Vector2)_hero.position + input.normalized * (_crawlSpeed * 2f * pull * Time.deltaTime);
                    next.x = Mathf.Clamp(next.x, _area.xMin, _area.xMax);
                    next.y = Mathf.Clamp(next.y, _area.yMin, _area.yMax);
                    _hero.position = next;
                    float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
                    _heroBody.rotation = Quaternion.RotateTowards(_heroBody.rotation, Quaternion.Euler(0f, 0f, angle), 120f * Time.deltaTime);

                    // Кадр по фазе рывка; ноги толкаются по очереди — каждый второй рывок другой ногой
                    if (_crawlFrames != null && _crawlFrames.Length == 4)
                    {
                        float phase = crawl * 5f / (2f * Mathf.PI);
                        float t = phase - Mathf.Floor(phase);
                        bool leftLeg = Mathf.FloorToInt(phase) % 2 == 0;
                        // Каждый рывок — тело волочится по асфальту
                        if (Mathf.FloorToInt(phase) != lastPull)
                        {
                            lastPull = Mathf.FloorToInt(phase);
                            N89.Core.Sfx.Ui(N89.Core.Sfx.Crawl, 0.8f);
                        }
                        int frame = t < 0.25f ? (leftLeg ? 1 : 3) : t < 0.5f ? 2 : t < 0.8f ? (leftLeg ? 1 : 3) : 0;
                        bodySprite.sprite = _crawlFrames[frame];
                    }

                    // За ним тянется кровавый след
                    if (_bloodSmear != null && Vector2.Distance(_hero.position, lastSmear) > _smearStep)
                    {
                        Vector2 dir = (Vector2)_hero.position - lastSmear;
                        var smear = new GameObject("BloodTrail").AddComponent<SpriteRenderer>();
                        smear.sprite = _bloodSmear;
                        smear.sharedMaterial = bodySprite.sharedMaterial;
                        smear.sortingOrder = bodySprite.sortingOrder - 5;
                        smear.transform.SetPositionAndRotation(lastSmear + dir * 0.5f + Random.insideUnitCircle * 0.06f,
                            Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + Random.Range(-12f, 12f)));
                        smear.transform.localScale = Vector3.one * Random.Range(0.9f, 1.3f);
                        lastSmear = _hero.position;
                    }
                }
                // Рывок растягивает тело; стоит — тяжело, неровно дышит
                float breath = pull > 0f ? 0f : 0.03f * Mathf.Max(0f, Mathf.Sin(Time.time * 2.2f) + 0.4f * Mathf.Sin(Time.time * 5.3f));
                _heroBody.localScale = new Vector3(baseScale.x * (1f + 0.06f * pull), baseScale.y * (1f - 0.04f * pull + breath), baseScale.z);

                // Чем ближе свет, тем он ярче
                float k = 1f - Mathf.Clamp01(Vector2.Distance(_hero.position, _lightPoint.position) / 25f);
                _light.intensity = _lightBase * (1f + 2.5f * k * k);
                FollowCamera(Time.deltaTime * 2f);
                yield return null;
            }

            // Свет заливает всё
            if (_waypoint != null) _waypoint.Hide();
            N89.Core.Sfx.Ui(N89.Core.Sfx.SandevistanOff, 0.7f, 0.5f);
            yield return Fade(_white, 0f, 1f, 2.5f);
            yield return new WaitForSeconds(0.8f);
            _title.color = Color.black;
            _subtitle.color = new Color(0.1f, 0.1f, 0.15f);
            N89.Core.Sfx.Ui(N89.Core.Sfx.Stinger);
            yield return Fade(_title, 0f, 1f, 1.5f);
            yield return Fade(_subtitle, 0f, 1f, 1.5f);
            yield return new WaitForSeconds(4f);
            yield return Fade(_black, 0f, 1f, 2f);
            SceneManager.LoadScene(_menuScene);
        }

        private IEnumerator ShowThought(string line, float hold)
        {
            _thought.text = line;
            N89.Core.Sfx.Ui(N89.Core.Sfx.Decode, 0.6f);
            yield return Fade(_thought, 0f, 1f, 0.6f);
            yield return new WaitForSeconds(hold);
            yield return Fade(_thought, 1f, 0f, 0.6f);
        }

        private void FollowCamera(float k)
        {
            Vector3 target = new Vector3(_hero.position.x, _hero.position.y + 1.5f, _camera.transform.position.z);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, target, Mathf.Clamp01(k));
        }

        private static Vector2 ReadMove()
        {
            if (N89.UI.PauseMenu.IsPaused) return Vector2.zero;
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            Vector2 v = Vector2.zero;
            if (kb.wKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed) v.y -= 1f;
            if (kb.dKey.isPressed) v.x += 1f;
            if (kb.aKey.isPressed) v.x -= 1f;
            return v;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }

        private static IEnumerator Fade(TMP_Text text, float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                text.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            text.alpha = to;
        }
    }
}
