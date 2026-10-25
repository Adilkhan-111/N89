using System.Collections;
using System.Collections.Generic;
using N89.Combat;
using N89.Core;
using N89.Enemies;
using TMPro;
using UnityEngine;

namespace N89.UI
{
    // Очки как в Hotline Miami, на сериях убийств: каждое убийство в серии стоит базу × номер в серии
    // (1-е — ×1, 2-е — ×2, ...). Серия живёт _comboWindow секунд игрового времени после последнего убийства
    // (замедление TAB растягивает окно); не успел — серия закрывается и её очки падают в общий счёт.
    // Смерть героя сжигает незакрытую серию. Враги — все Health на слое Enemy (включая неактивных на старте).
    // Ранг в конце: счёт / «пар» (все враги без серий). Счёт переживает чекпоинт.
    public class ScoreCounter : MonoBehaviour
    {
        [Header("Очки")]
        [SerializeField] private int _basePoints = 400;
        [SerializeField] private int _bossPoints = 5000;
        [SerializeField] private float _comboWindow = 3f;
        [Tooltip("Счёт / пар для рангов S, A, B, C (ниже — D)")]
        [SerializeField] private float[] _rankRatios = { 3f, 2.2f, 1.6f, 1.1f };
        [SerializeField] private Health _player;
        [Tooltip("Не считать (например, сценарный убийца в туториале)")]
        [SerializeField] private Health[] _exclude;

        [Header("HUD")]
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _scoreShadow;
        [SerializeField] private RectTransform _scoreBox;
        [SerializeField] private CanvasGroup _comboGroup;
        [SerializeField] private RectTransform _comboBox;
        [SerializeField] private TMP_Text _comboText;
        [SerializeField] private TMP_Text _comboGhostA;
        [SerializeField] private TMP_Text _comboGhostB;
        [SerializeField] private TMP_Text _streakText;
        [SerializeField] private RectTransform _comboBar;
        [Tooltip("Шаблон всплывающих очков над убитым (неактивный)")]
        [SerializeField] private TMP_Text _popupTemplate;

        private static readonly Color[] ComboColors =
        {
            new Color(1f, 1f, 1f), new Color(1f, 0.9f, 0.35f), new Color(1f, 0.55f, 0.2f),
            new Color(1f, 0.25f, 0.65f), new Color(0.35f, 1f, 1f),
        };
        private static readonly string[] Ranks = { "S", "A", "B", "C", "D" };

        // Счёт на момент чекпоинта: после смерти дальше чекпоинта не обнуляется
        private static string s_savedAt;
        private static int s_savedScore;
        private static int s_savedKills;
        private static int s_savedMaxCombo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_savedAt = null;

        public static ScoreCounter Instance { get; private set; }

        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int TotalEnemies { get; private set; }
        public int MaxCombo { get; private set; }
        public int Combo => _combo;
        // Туториал прячет счёт, пока не дошёл до шага про серии
        public bool Visible { set => _group.alpha = value ? 1f : 0f; }

        private int _par;
        private int _combo;
        private int _streak;
        private float _comboLeft;
        private float _shownScore;
        private string _seenCheckpoint;
        private Camera _camera;
        private readonly List<(RectTransform rect, TMP_Text text, Vector3 world, float age)> _popups = new();

        private void Awake()
        {
            Instance = this;
            _popupTemplate.gameObject.SetActive(false);
            _comboGroup.alpha = 0f;
            if (Checkpoint.Current != null && Checkpoint.Current == s_savedAt)
            {
                Score = s_savedScore;
                Kills = s_savedKills;
                MaxCombo = s_savedMaxCombo;
            }
            _shownScore = Score;
            _seenCheckpoint = Checkpoint.Current;
            SetScoreText(Score);
        }

        private void Start()
        {
            _camera = Camera.main;
            var excluded = new HashSet<Health>(_exclude ?? new Health[0]);
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            foreach (Health health in FindObjectsByType<Health>(FindObjectsInactive.Include))
            {
                if (health == _player || excluded.Contains(health) || health.gameObject.layer != enemyLayer) continue;
                if (!health.gameObject.scene.IsValid()) continue;
                int points = health.GetComponent<BossAI>() != null ? _bossPoints : _basePoints;
                _par += points;
                TotalEnemies++;
                if (health.IsDead) continue;
                health.Died += h => OnKill(h, points);
            }
            if (_player != null) _player.Died += _ => BreakCombo(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------ Логика

        private void OnKill(Health victim, int points)
        {
            if (_player != null && _player.IsDead) return;
            _combo++;
            MaxCombo = Mathf.Max(MaxCombo, _combo);
            Kills++;
            int gained = points * _combo;
            _streak += gained;
            _comboLeft = _comboWindow;

            Color color = ComboColors[Mathf.Min(_combo - 1, ComboColors.Length - 1)];
            SpawnPopup(victim.transform.position, $"+{gained}", color, _combo);
            if (_combo >= 2)
            {
                // Чем длиннее серия, тем выше сигнал
                N89.Core.Sfx.Ui(N89.Core.Sfx.Combo, 0.8f, 1f + 0.12f * Mathf.Min(_combo - 2, 8));
                _comboText.text = $"{_combo}x";
                _comboGhostA.text = _comboText.text;
                _comboGhostB.text = _comboText.text;
                _comboText.color = color;
                _streakText.text = $"+{_streak}";
                StopCoroutine(nameof(ComboPunch));
                StartCoroutine(nameof(ComboPunch));
            }
        }

        // Серия закрыта: её очки — в общий счёт (bank) или сгорают (смерть)
        private void BreakCombo(bool bank)
        {
            if (_combo == 0) return;
            if (bank)
            {
                Score += _streak;
                StartCoroutine(Punch(_scoreBox, 1.25f, 0.25f));
            }
            if (_combo >= 2) StartCoroutine(ComboOut(bank));
            _combo = 0;
            _streak = 0;
        }

        private void Update()
        {
            if (_combo > 0)
            {
                _comboLeft -= Time.deltaTime;
                _comboBar.localScale = new Vector3(Mathf.Clamp01(_comboLeft / _comboWindow), 1f, 1f);
                if (_comboLeft <= 0f) BreakCombo(true);
            }

            // Чекпоинт поставлен — серия закрывается, счёт запоминается
            if (Checkpoint.Current != _seenCheckpoint)
            {
                _seenCheckpoint = Checkpoint.Current;
                if (_seenCheckpoint != null) SaveCheckpoint();
            }

            if (Mathf.Abs(_shownScore - Score) > 0.5f)
            {
                _shownScore = Mathf.MoveTowards(_shownScore, Score, Mathf.Max(60f, Mathf.Abs(Score - _shownScore) * 6f) * Time.unscaledDeltaTime);
                SetScoreText(Mathf.RoundToInt(_shownScore));
            }

            // Неоновое раздвоение цифры серии
            if (_comboGroup.alpha > 0f)
            {
                float j = 3f + 2f * Mathf.Sin(Time.unscaledTime * 23f);
                _comboGhostA.rectTransform.anchoredPosition = new Vector2(-j, 1f);
                _comboGhostB.rectTransform.anchoredPosition = new Vector2(j, -1f);
                // Последние полсекунды окна серии — мигает: сейчас оборвётся
                float flash = _combo > 0 && _comboLeft < 0.6f ? (Mathf.Sin(Time.unscaledTime * 30f) > 0f ? 0.35f : 1f) : 1f;
                _comboText.alpha = flash;
            }

            UpdatePopups();
        }

        private void SaveCheckpoint()
        {
            BreakCombo(true);
            s_savedAt = Checkpoint.Current;
            s_savedScore = Score;
            s_savedKills = Kills;
            s_savedMaxCombo = MaxCombo;
        }

        // Конец уровня: незакрытая серия засчитывается сразу
        public void Finish()
        {
            BreakCombo(true);
            _shownScore = Score;
            SetScoreText(Score);
        }

        public string Rank
        {
            get
            {
                float ratio = _par <= 0 ? 0f : (float)Score / _par;
                for (int i = 0; i < _rankRatios.Length; i++)
                {
                    if (ratio >= _rankRatios[i]) return Ranks[i];
                }
                return Ranks[Ranks.Length - 1];
            }
        }

        public static Color RankColor(string rank) => rank switch
        {
            "S" => new Color(1f, 0.82f, 0.25f),
            "A" => new Color(1f, 0.3f, 0.75f),
            "B" => new Color(0.35f, 1f, 1f),
            "C" => new Color(0.55f, 1f, 0.45f),
            _ => new Color(0.7f, 0.65f, 0.75f),
        };

        // ------------------------------------------------------------ Визуал

        private void SetScoreText(int value)
        {
            string text = Format(value);
            _scoreText.text = text;
            _scoreShadow.text = text;
        }

        // 12400 → «12 400»
        public static string Format(int value) =>
            value.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');

        private IEnumerator ComboPunch()
        {
            _comboGroup.alpha = 1f;
            _comboBox.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-9f, 9f));
            float from = 1.6f + 0.06f * Mathf.Min(_combo, 8);
            for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
            {
                _comboBox.localScale = Vector3.one * Mathf.Lerp(from, 1f, t / 0.18f);
                yield return null;
            }
            _comboBox.localScale = Vector3.one;
        }

        // Серия уходит: при засчёте взлетает к счёту, при смерти — гаснет, рассыпаясь красным
        private IEnumerator ComboOut(bool banked)
        {
            yield return null;
            if (_combo > 0) yield break;
            Vector2 start = _comboBox.anchoredPosition;
            if (!banked) _comboText.color = new Color(1f, 0.2f, 0.25f);
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                if (_combo > 0) break;
                float k = t / 0.35f;
                _comboGroup.alpha = 1f - k;
                _comboBox.anchoredPosition = start + (banked ? new Vector2(0f, 60f) : new Vector2(0f, -20f)) * k;
                _comboBox.localScale = Vector3.one * (banked ? 1f - 0.3f * k : 1f + 0.4f * k);
                yield return null;
            }
            _comboBox.anchoredPosition = start;
            _comboBox.localScale = Vector3.one;
            if (_combo == 0) _comboGroup.alpha = 0f;
        }

        private static IEnumerator Punch(RectTransform target, float scale, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * Mathf.Lerp(scale, 1f, t / duration);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        // Очки над убитым: выпрыгивают, всплывают и тают; чем длиннее серия — тем крупнее
        private void SpawnPopup(Vector3 world, string text, Color color, int combo)
        {
            TMP_Text popup = Instantiate(_popupTemplate, _popupTemplate.transform.parent);
            popup.gameObject.SetActive(true);
            popup.text = text;
            popup.color = color;
            popup.fontSize = _popupTemplate.fontSize * (1f + 0.12f * Mathf.Min(combo - 1, 6));
            popup.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));
            _popups.Add((popup.rectTransform, popup, world + (Vector3)Random.insideUnitCircle * 0.2f, 0f));
        }

        private void UpdatePopups()
        {
            if (_camera == null) _camera = Camera.main;
            RectTransform canvas = (RectTransform)_popupTemplate.canvas.transform;
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                var p = _popups[i];
                p.age += Time.unscaledDeltaTime;
                const float life = 1.1f;
                if (p.age >= life || _camera == null)
                {
                    Destroy(p.rect.gameObject);
                    _popups.RemoveAt(i);
                    continue;
                }
                Vector2 screen = _camera.WorldToScreenPoint(p.world);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local);
                float k = p.age / life;
                p.rect.anchoredPosition = local + new Vector2(0f, 40f + 70f * k);
                p.rect.localScale = Vector3.one * (k < 0.12f ? Mathf.Lerp(1.8f, 1f, k / 0.12f) : 1f);
                Color c = p.text.color;
                c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                p.text.color = c;
                _popups[i] = p;
            }
        }
    }
}
