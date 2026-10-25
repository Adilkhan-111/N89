using System;
using System.Collections;
using System.Collections.Generic;
using N89.Enemies;
using N89.Hacking;
using N89.Player;
using N89.UI;
using N89.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace N89.Missions
{
    // Бой с Боссом в подвале миссии 2. Запускает MissionDirector, когда герой спустился по лестнице.
    // В подвале темно (глобальный свет почти погашен), зал не освещён — через стекло ничего не видно.
    // Герой входит в зал (_arenaEntry) — катсцена: три лампы загораются по очереди, Босс встаёт, бой.
    // Правила арены:
    //  - ракета Босса, попавшая в колонну, рушит её: вокруг колонны мигает зона обрушения, через _collapseDelay
    //    падает потолок — Босс внутри теряет _collapseDamage (1/4), герой внутри погибает;
    //  - если 4 обрушения прошли мимо Босса (или колонн не осталось), сверху падают газовые баллоны:
    //    подстрелить баллон рядом с Боссом — минус 1/8; кончились — падают новые;
    //  - пули почти не ранят Босса, взлом лишь обездвиживает его.
    // Победа — финал: Босс узнаёт в герое своего друга Брайса, прощается и умирает.
    public class BossFight : MonoBehaviour
    {
        [Serializable]
        private struct Line
        {
            public string Speaker;
            [TextArea] public string Text;
        }

        [Header("Герой")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private HackingController _hacking;
        [SerializeField] private CameraFollow _cameraFollow;

        [Header("UI")]
        [SerializeField] private ObjectivePanel _objective;
        [SerializeField] private SubtitleBox _subtitles;
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private RectTransform _letterboxTop;
        [SerializeField] private RectTransform _letterboxBottom;
        [SerializeField] private BossHud _hud;

        [Header("Босс")]
        [SerializeField] private BossAI _boss;
        [SerializeField] private SpriteRenderer _bossBody;
        [Tooltip("Поверженный: стоит на коленях")]
        [SerializeField] private Sprite _bossDownSprite;
        [SerializeField] private Sprite _bossDeadSprite;
        [SerializeField] private string _bossName = "БОСС";
        [SerializeField] private GameObject _bloodFxPrefab;
        [SerializeField] private GameObject _bloodPoolPrefab;

        [Header("Темнота и лампы")]
        [SerializeField] private Light2D _globalLight;
        [SerializeField] private float _darkIntensity = 0.03f;
        [SerializeField] private Light2D[] _lamps;
        [SerializeField] private SpriteRenderer[] _lampFixtures;
        [SerializeField] private float _lampIntensity = 1f;

        [Header("Арена")]
        [Tooltip("Зона сразу за дверью в зал (мировые координаты): вошёл — катсцена и бой")]
        [SerializeField] private Rect _arenaEntry;
        [SerializeField] private Transform _arenaCenter;
        [SerializeField] private float _cutsceneZoom = 7.5f;

        [Header("Обрушение потолка")]
        [SerializeField] private Pillar[] _pillars;
        [SerializeField] private float _collapseDelay = 3.5f;
        [SerializeField] private float _collapseRadius = 3.2f;
        [SerializeField] private int _collapseDamage = 100;
        [Tooltip("Сколько обрушений мимо Босса — и начинают падать газовые баллоны")]
        [SerializeField] private int _missesBeforeGas = 4;
        [SerializeField] private Sprite _warningSprite;
        [SerializeField] private Sprite[] _rubbleSprites;
        [SerializeField] private GameObject _dustFxPrefab;
        [Tooltip("Искры из пробитого корпуса Босса в финальной катсцене")]
        [SerializeField] private GameObject _sparkFxPrefab;
        [SerializeField] private Material _material;

        [Header("Газовые баллоны")]
        [SerializeField] private GasCylinder _cylinderPrefab;
        [Tooltip("Куда могут упасть (мировые координаты)")]
        [SerializeField] private Vector2[] _dropPoints;
        [SerializeField] private int _waveSize = 3;
        [SerializeField] private float _waveDelay = 2f;

        [Header("Кейс с компроматом")]
        [Tooltip("Выпадает из-под тела Босса после финала; подобрать — миссия выполнена")]
        [SerializeField] private Sprite _caseSprite;
        [SerializeField] private float _caseSize = 1.1f;
        [SerializeField] private float _pickupRadius = 1.3f;
        [SerializeField] private string _caseObjective = "Забери кейс с компроматом";

        [Header("Тексты")]
        [SerializeField] private string _enterObjective = "Осмотри подвал";
        [SerializeField] private string _fightObjective = "Убей Босса";
        [SerializeField] private string _pillarHint = "Ракеты рушат колонны — заманите Босса под обвал";
        [SerializeField] private string _gasObjective = "Подстрели газовый баллон рядом с Боссом";
        [SerializeField] private Line[] _introLines =
        {
            new Line { Speaker = "БОСС", Text = "Ещё одна корпоративная шавка. Иди сюда." },
        };
        [SerializeField] private Line[] _finaleLines =
        {
            new Line { Speaker = "БОСС", Text = "Брайс?.. Я думал, ты мёртв. Не думал, что именно ты убьёшь меня." },
            new Line { Speaker = "РЭЙ", Text = "..." },
            new Line { Speaker = "БОСС", Text = "Прощай, мой друг. Встретимся на том свете." },
        };

        private bool _fighting;
        private bool _gasStarted;
        private int _misses;
        private int _collapsing;

        private bool HeroDead => _player.Health.IsDead;

        // Герой спустился в подвал: темно; после смерти (lit) лампы уже горят — катсцену второй раз не показываем
        public void Prepare(bool lit)
        {
            if (_globalLight != null) _globalLight.intensity = _darkIntensity;
            for (int i = 0; i < _lamps.Length; i++) SetLamp(i, lit ? 1f : 0f);
            foreach (Pillar pillar in _pillars)
            {
                pillar.Broken -= OnPillarBroken;
                pillar.Broken += OnPillarBroken;
            }
        }

        public IEnumerator Run(bool restart)
        {
            _objective.Set(_enterObjective);
            yield return new WaitUntil(() => HeroDead || _arenaEntry.Contains(_player.transform.position));
            if (HeroDead) yield break;

            if (!restart) yield return Intro();
            if (HeroDead) yield break;

            _fighting = true;
            _boss.Activate(_player.transform);
            _hud.Show(_boss.Health, _bossName);
            _objective.Set(_fightObjective);
            yield return new WaitUntil(() => HeroDead || _boss.Health.IsDead);
            _fighting = false;
            if (HeroDead) yield break;

            _hud.Hide();
            yield return Finale();
            yield return PickUpCase();
        }

        // Из-под тела Босса виден кейс: светится, покачивается — подойти и забрать
        private IEnumerator PickUpCase()
        {
            if (_caseSprite == null) yield break;

            // Труп Босса не должен мешать подойти к кейсу
            foreach (Collider2D col in _boss.GetComponentsInChildren<Collider2D>()) col.enabled = false;

            var go = new GameObject("KompromatCase");
            Vector3 at = _boss.transform.position;
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _caseSprite;
            sr.sortingLayerID = _bossBody.sortingLayerID;
            sr.sortingOrder = _bossBody.sortingOrder + 2;
            if (_material != null) sr.sharedMaterial = _material;
            float scale = _caseSize / Mathf.Max(0.01f, _caseSprite.bounds.size.x);
            var glow = go.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.color = new Color(0.3f, 0.9f, 1f);
            glow.pointLightOuterRadius = 2.2f;

            _objective.Set(_caseObjective);
            _fader.Flash(new Color(0.3f, 0.9f, 1f, 0.2f), 0.3f);
            SetControl(true);

            // Выскальзывает из-под тела и дальше пульсирует, пока герой не подойдёт
            for (float t = 0f; ; t += Time.deltaTime)
            {
                float pop = Mathf.Clamp01(t / 0.35f);
                go.transform.localScale = Vector3.one * scale * (Mathf.SmoothStep(0.3f, 1f, pop) + 0.05f * Mathf.Sin(t * 4f));
                glow.intensity = 0.8f + 0.5f * Mathf.Sin(t * 5f);
                if (HeroDead) yield break;
                if (pop >= 1f && Vector2.Distance(_player.transform.position, at) <= _pickupRadius) break;
                yield return null;
            }

            SetControl(false);
            N89.Core.Sfx.Play(N89.Core.Sfx.Pickup, at);
            _cameraFollow.Shake(0.1f, 0.1f);
            _fader.Flash(new Color(0.3f, 0.9f, 1f, 0.35f), 0.4f);
            Destroy(go);
            yield return new WaitForSeconds(0.6f);
        }

        // ------------------------------------------------------------ Катсцены

        // Три лампы загораются по очереди — мигнут и встанут, — и в свете виден Босс
        private IEnumerator Intro()
        {
            Time.timeScale = 1f;
            SetControl(false);
            float zoom = Camera.main.orthographicSize;
            StartCoroutine(Letterbox(120f, 0.5f));
            _cameraFollow.Focus(_arenaCenter, 1.6f);
            StartCoroutine(_cameraFollow.Zoom(_cutsceneZoom, 1.4f));
            yield return new WaitForSeconds(1f);

            // Камера перескакивает к каждой загорающейся лампе
            var lampFocus = new GameObject("LampFocus").transform;
            lampFocus.position = _arenaCenter.position;
            _cameraFollow.Focus(lampFocus, 4f);
            for (int i = 0; i < _lamps.Length; i++)
            {
                if (_lamps[i] != null) lampFocus.position = Vector2.Lerp(_arenaCenter.position, _lamps[i].transform.position, 0.45f);
                yield return LampOn(i);
                yield return new WaitForSeconds(0.55f);
            }

            // В полном свете — Босс: камера на него, он тяжело разворачивается к Рэю и делает шаг навстречу
            _cameraFollow.Focus(_boss.transform, 3f);
            yield return new WaitForSeconds(0.4f);
            N89.Core.Sfx.Play(N89.Core.Sfx.MinigunSpin, _boss.transform.position, 0.6f, 0.6f);
            yield return N89.Core.Cutscene.Turn(_bossBody.transform, _player.transform.position, 0.7f);
            N89.Core.Sfx.Play(N89.Core.Sfx.Clang, _boss.transform.position, 0.8f, 0.5f);
            _cameraFollow.Shake(0.2f, 0.25f);
            _cameraFollow.Punch(0.08f, 0.5f);
            yield return N89.Core.Cutscene.Lunge(_bossBody.transform, _player.transform.position - _boss.transform.position, 0.2f, 0.35f);
            _cameraFollow.Focus(_arenaCenter, 1.6f);
            Destroy(lampFocus.gameObject);

            yield return new WaitForSeconds(0.3f);
            foreach (Line line in _introLines) yield return _subtitles.Say(line.Speaker, line.Text);

            _cameraFollow.Focus(null, 0f);
            StartCoroutine(_cameraFollow.Zoom(zoom, 0.8f));
            yield return Letterbox(0f, 0.5f);
            SetControl(true);
            _fader.Flash(new Color(1f, 0.3f, 0.2f, 0.25f), 0.3f);
        }

        // Лампа ловит напряжение: пара вспышек и гаснет, потом загорается насовсем с гулким ударом
        private IEnumerator LampOn(int index)
        {
            float[] flicker = { 0.07f, 0.12f, 0.05f, 0.18f };
            Vector2 at = index < _lamps.Length && _lamps[index] != null ? (Vector2)_lamps[index].transform.position : (Vector2)_arenaCenter.position;
            for (int i = 0; i < flicker.Length; i++)
            {
                bool on = i % 2 == 0;
                SetLamp(index, on ? Random.Range(0.5f, 0.9f) : 0f);
                if (on) N89.Core.Sfx.Play(N89.Core.Sfx.LampFlicker, at);
                yield return new WaitForSeconds(flicker[i]);
            }
            SetLamp(index, 1f);
            N89.Core.Sfx.Play(N89.Core.Sfx.LampOn, at);
            _cameraFollow.Shake(0.12f, 0.15f);
        }

        private void SetLamp(int index, float k)
        {
            if (index < _lamps.Length && _lamps[index] != null) _lamps[index].intensity = _lampIntensity * k;
            if (index < _lampFixtures.Length && _lampFixtures[index] != null)
            {
                _lampFixtures[index].color = Color.Lerp(new Color(0.15f, 0.15f, 0.18f), Color.white, k);
            }
        }

        // Босс падает на колени, узнаёт героя — и умирает, не дав ничего спросить
        private IEnumerator Finale()
        {
            Time.timeScale = 1f;
            SetControl(false);
            foreach (GasCylinder cylinder in FindObjectsByType<GasCylinder>()) Destroy(cylinder.gameObject);
            float zoom = Camera.main.orthographicSize;
            var focus = new GameObject("FinaleFocus").transform;
            focus.position = Vector2.Lerp(_player.transform.position, _boss.transform.position, 0.6f);
            _cameraFollow.Focus(focus, 1.2f);
            StartCoroutine(_cameraFollow.Zoom(4f, 1f));
            StartCoroutine(Letterbox(120f, 0.5f));

            // Шатается назад, искрит — и рушится на колени
            Vector2 away = (Vector2)_boss.transform.position - (Vector2)_player.transform.position;
            for (int i = 0; i < 2; i++)
            {
                if (_sparkFxPrefab != null) Instantiate(_sparkFxPrefab, (Vector2)_boss.transform.position + Random.insideUnitCircle * 0.6f, Quaternion.identity);
                N89.Core.Sfx.Play(N89.Core.Sfx.LampFlicker, _boss.transform.position);
                yield return N89.Core.Cutscene.Stagger(_boss.transform, _bossBody.transform, away, 0.25f, 0.3f);
            }
            if (_bossDownSprite != null) _bossBody.sprite = _bossDownSprite;
            N89.Core.Sfx.Play(N89.Core.Sfx.BodyFall, _boss.transform.position, 1f, 0.7f);
            N89.Core.Sfx.Play(N89.Core.Sfx.Clang, _boss.transform.position, 0.6f, 0.6f);
            _cameraFollow.Shake(0.3f, 0.3f);
            if (_dustFxPrefab != null) Instantiate(_dustFxPrefab, _boss.transform.position, Quaternion.identity);
            yield return new WaitForSeconds(0.8f);

            // Рэй медленно подходит, пока Босс говорит
            Vector2 close = (Vector2)_boss.transform.position - away.normalized * (_boss.Radius + 1.3f);
            Rigidbody2D heroRb = _player.GetComponent<Rigidbody2D>();
            if (heroRb != null) heroRb.linearVelocity = Vector2.zero;
            StartCoroutine(N89.Core.Cutscene.Walk(_player.transform, _player.transform.Find("Body"), close, 1.2f));
            yield return new WaitForSeconds(0.4f);

            foreach (Line line in _finaleLines)
            {
                yield return _subtitles.Say(line.Speaker, line.Text);
                yield return new WaitForSeconds(0.3f);
            }

            // Падает навзничь — в замедлении, синяя кровь растекается
            Time.timeScale = 0.35f;
            _cameraFollow.Punch(0.12f, 0.8f);
            yield return N89.Core.Cutscene.Stagger(_boss.transform, _bossBody.transform, away, 0.2f, 0.25f);
            Time.timeScale = 1f;
            if (_bossDeadSprite != null) _bossBody.sprite = _bossDeadSprite;
            N89.Core.Sfx.Play(N89.Core.Sfx.BodyFall, _boss.transform.position, 1f, 0.6f);
            if (_dustFxPrefab != null) Instantiate(_dustFxPrefab, _boss.transform.position, Quaternion.identity);
            if (_bloodFxPrefab != null) Instantiate(_bloodFxPrefab, _boss.transform.position, Quaternion.identity);
            if (_bloodPoolPrefab != null)
            {
                GameObject pool = Instantiate(_bloodPoolPrefab, _boss.transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
                StartCoroutine(Grow(pool.transform, 2.6f, 2.5f));
            }
            _cameraFollow.Shake(0.35f, 0.25f);
            yield return new WaitForSeconds(2f);

            _cameraFollow.Focus(null, 0f);
            StartCoroutine(_cameraFollow.Zoom(zoom, 0.8f));
            yield return Letterbox(0f, 0.5f);
        }

        // ------------------------------------------------------------ Обрушение потолка

        private void OnPillarBroken(Pillar pillar)
        {
            if (!_fighting) return;
            StartCoroutine(Collapse(pillar.Center));
        }

        private IEnumerator Collapse(Vector2 center)
        {
            _collapsing++;
            _objective.Set(_pillarHint);

            // Зона на полу мигает всё чаще, с потолка сыплется пыль, пол дрожит
            SpriteRenderer warning = Decal("CollapseWarning", _warningSprite, center, _collapseRadius * 2f, -80, true);
            N89.Core.Sfx.Play(N89.Core.Sfx.Rumble, center);
            float nextDust = 0f;
            for (float t = 0f; t < _collapseDelay; t += Time.deltaTime)
            {
                float k = t / _collapseDelay;
                float blink = 0.5f + 0.5f * Mathf.Sin(t * Mathf.Lerp(6f, 30f, k));
                warning.color = new Color(1f, 0.2f, 0.15f, Mathf.Lerp(0.25f, 0.8f, blink));
                if (t >= nextDust && _dustFxPrefab != null)
                {
                    nextDust = t + 0.2f;
                    Instantiate(_dustFxPrefab, center + Random.insideUnitCircle * _collapseRadius, Quaternion.identity);
                }
                _cameraFollow.Shake(0.03f + 0.05f * k, 0.05f);
                yield return null;
            }
            Destroy(warning.gameObject);

            // Удар: груда обломков, облака пыли, тряска
            N89.Core.Sfx.Play(N89.Core.Sfx.Crash, center);
            foreach (Sprite rubble in _rubbleSprites)
            {
                for (int i = 0; i < 3; i++)
                {
                    SpriteRenderer piece = Decal("Rubble", rubble, center + Random.insideUnitCircle * _collapseRadius * 0.75f, Random.Range(0.8f, 1.5f), -75);
                    piece.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                }
            }
            for (int i = 0; i < 6 && _dustFxPrefab != null; i++)
            {
                Instantiate(_dustFxPrefab, center + Random.insideUnitCircle * _collapseRadius, Quaternion.identity);
            }
            _cameraFollow.Shake(0.6f, 0.35f);

            bool bossHit = !_boss.Health.IsDead && Vector2.Distance(_boss.transform.position, center) <= _collapseRadius + _boss.Radius * 0.5f;
            if (bossHit)
            {
                _boss.Health.TakeDamage(_collapseDamage);
                _fader.Flash(new Color(1f, 0.85f, 0.6f, 0.35f), 0.25f);
            }
            else
            {
                _misses++;
            }

            if (!HeroDead && Vector2.Distance(_player.transform.position, center) <= _collapseRadius)
            {
                _player.Health.Kill();
            }

            _collapsing--;
            if (!_boss.Health.IsDead && _fighting) _objective.Set(_gasStarted ? _gasObjective : _fightObjective);
            CheckGas();
        }

        private void CheckGas()
        {
            if (_gasStarted || !_fighting || _boss.Health.IsDead) return;
            bool pillarsLeft = false;
            foreach (Pillar pillar in _pillars) pillarsLeft |= !pillar.IsBroken;
            if (_misses >= _missesBeforeGas || (!pillarsLeft && _collapsing == 0))
            {
                _gasStarted = true;
                StartCoroutine(GasWaves());
            }
        }

        // ------------------------------------------------------------ Газовые баллоны

        // Волна баллонов с потолка; когда все взорваны, а Босс жив, — следующая
        private IEnumerator GasWaves()
        {
            _objective.Set(_gasObjective);
            var wave = new List<GasCylinder>();
            while (_fighting && !_boss.Health.IsDead)
            {
                wave.Clear();
                foreach (Vector2 point in PickDropPoints())
                {
                    wave.Add(Instantiate(_cylinderPrefab, point, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))));
                    yield return new WaitForSeconds(0.35f);
                }

                yield return new WaitUntil(() => !_fighting || _boss.Health.IsDead || wave.TrueForAll(c => c == null));
                yield return new WaitForSeconds(_waveDelay);
            }
        }

        // Случайные точки, не на голове у героя и Босса
        private List<Vector2> PickDropPoints()
        {
            var points = new List<Vector2>(_dropPoints);
            for (int i = 0; i < points.Count; i++)
            {
                int j = Random.Range(i, points.Count);
                (points[i], points[j]) = (points[j], points[i]);
            }

            var picked = new List<Vector2>();
            foreach (Vector2 p in points)
            {
                if (picked.Count >= _waveSize) break;
                if (Vector2.Distance(p, _player.transform.position) < 1.6f) continue;
                if (Vector2.Distance(p, _boss.transform.position) < _boss.Radius + 0.8f) continue;
                picked.Add(p);
            }
            return picked;
        }

        // ------------------------------------------------------------ Помощники

        // glow — светится в темноте (материал без освещения), иначе обычный освещаемый спрайт
        private SpriteRenderer Decal(string name, Sprite sprite, Vector2 at, float size, int order, bool glow = false)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (glow && _material != null) sr.sharedMaterial = _material;
            sr.sortingOrder = order;
            sr.transform.position = at;
            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            sr.transform.localScale = Vector3.one * (size / spriteSize);
            return sr;
        }

        private void SetControl(bool on)
        {
            _player.CanMove = on;
            _player.CanShoot = on;
            _player.CanAim = on;
            _hacking.HackingEnabled = on;
            if (!on) _player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }

        private IEnumerator Letterbox(float height, float duration)
        {
            if (_letterboxTop == null || _letterboxBottom == null) yield break;
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

        private static IEnumerator Grow(Transform target, float scale, float duration)
        {
            Vector3 from = target.localScale * 0.2f;
            Vector3 to = target.localScale * scale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localScale = Vector3.Lerp(from, to, 1f - Mathf.Pow(1f - t / duration, 3f));
                yield return null;
            }
            target.localScale = to;
        }
    }
}
