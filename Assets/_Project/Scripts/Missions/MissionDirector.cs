using System.Collections;
using N89.Combat;
using N89.Core;
using N89.Hacking;
using N89.Player;
using N89.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace N89.Missions
{
    // Сценарий миссии: герой стартует со снаряжением из арсенала (Loadout), цель — убить всех врагов.
    // Смерть — как в Hotline Miami: короткая фраза героя и перезапуск уровня с начала.
    // Когда враги кончились — итоговый экран миссии.
    public class MissionDirector : MonoBehaviour
    {
        [Header("Hero")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private HackingController _hacking;
        [SerializeField] private Weapon _weapon;
        [SerializeField] private SpriteRenderer _heroBody;

        [Header("Снаряжение")]
        [SerializeField] private int _mission = 1;
        [SerializeField] private WeaponData[] _weapons;
        [Tooltip("Если в арсенале перед этой миссией ничего не выбирали (сцену запустили напрямую)")]
        [SerializeField] private int _defaultWeapon = 1;

        [Header("Враги")]
        [SerializeField] private Health[] _enemies;

        [Header("UI")]
        [SerializeField] private ScreenFader _fader;
        [SerializeField] private ObjectivePanel _objective;
        [SerializeField] private HackingHud _hackingHud;
        [SerializeField] private TutorialEndScreen _endScreen;
        [SerializeField] private string _title = "МИССИЯ 1";
        [SerializeField] private string _subtitle = "Логово банды";
        [SerializeField] private string _objectiveText = "Зачисти логово банды";
        [SerializeField] private string _clearedText = "Логово зачищено";
        [Tooltip("Фразы героя после смерти — одна случайная на экране перемотки")]
        [SerializeField] private string[] _deathLines = { "Это не то.", "Я что-то упускаю.", "Ещё раз. Тише.", "Слишком громко." };
        [SerializeField] private string _menuScene = "MainMenu";

        private int _killed;
        private bool _over;

        private IEnumerator Start()
        {
            ApplyLoadout();
            _player.Health.Died += OnPlayerDied;
            foreach (Health enemy in _enemies) enemy.Died += OnEnemyDied;

            _player.CanMove = false;
            _player.CanShoot = false;
            _hacking.HackingEnabled = false;
            _hackingHud.RamVisible = true;
            _objective.Set(string.Empty);
            _fader.SetBlack(1f);

            yield return new WaitForSeconds(0.3f);
            yield return _fader.ShowTitle(_title, _subtitle);
            yield return new WaitForSeconds(1f);
            yield return _fader.HideTitle(0.5f);

            _player.CanMove = true;
            _player.CanShoot = true;
            _hacking.HackingEnabled = true;
            UpdateObjective();
            yield return _fader.Fade(1f, 0f, 1f);

            yield return new WaitUntil(() => _over || _killed >= _enemies.Length);
            if (_over)
            {
                yield break;
            }

            _over = true;
            _objective.Set(_clearedText);
            yield return new WaitForSeconds(1.5f);
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

        private void OnEnemyDied(Health enemy)
        {
            _killed++;
            if (!_over) UpdateObjective();
        }

        private void UpdateObjective()
        {
            _objective.Set($"{_objectiveText}  {_killed}/{_enemies.Length}");
        }

        private void OnPlayerDied(Health health)
        {
            if (_over)
            {
                return;
            }

            _over = true;
            StartCoroutine(Restart());
        }

        // Перемотка к началу уровня
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
                SceneManager.LoadScene(_menuScene);
            }
        }
    }
}
