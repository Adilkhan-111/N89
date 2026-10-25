using System.Collections;
using N89.Combat;
using N89.Core;
using N89.Implants;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace N89.UI
{
    // Арсенал перед миссией: слева барабан оружия (любое из трёх), справа барабан имплантов
    // (на миссию доступен один, остальные видно, но взять нельзя). По краям — описания.
    // W/S или колесо — крутить барабан, A/D или мышь — выбрать барабан, Enter — подтвердить.
    // Выбор сохраняется в Loadout.
    public class ArsenalPanel : MonoBehaviour
    {
        private static readonly string[] StatNames = { "Скорострельность", "Мощь", "Точность", "Дальность" };

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private DrumSelector _weaponDrum;
        [SerializeField] private DrumSelector _implantDrum;
        [SerializeField] private WeaponData[] _weapons;
        [SerializeField] private ImplantData[] _implants;

        [Header("Описание оружия")]
        [SerializeField] private TMP_Text _weaponName;
        [SerializeField] private TMP_Text _weaponDescription;
        [Tooltip("Полоски характеристик: 4 строки по 5 сегментов, подряд")]
        [SerializeField] private Image[] _statSegments;
        [SerializeField] private TMP_Text[] _statLabels;

        [Header("Описание импланта")]
        [SerializeField] private TMP_Text _implantName;
        [SerializeField] private TMP_Text _implantStatus;
        [SerializeField] private TMP_Text _implantDescription;

        [Header("Управление")]
        [SerializeField] private TMP_Text _warning;
        [SerializeField] private Button _confirm;
        [SerializeField] private Button _weaponUp;
        [SerializeField] private Button _weaponDown;
        [SerializeField] private Button _implantUp;
        [SerializeField] private Button _implantDown;
        [SerializeField] private Color _statOn = new Color(0.3f, 1f, 1f);
        [SerializeField] private Color _statOff = new Color(0.3f, 1f, 1f, 0.15f);
        [SerializeField] private Color _lockedTint = new Color(0.3f, 0.3f, 0.36f, 1f);

        private int _mission;
        private bool _confirmed;
        private bool _implantDrumActive;
        private float _warningAlpha;

        public WeaponData ChosenWeapon => _weapons[_weaponDrum.Index];
        public ImplantData ChosenImplant => _implants[_implantDrum.Index];

        private void Awake()
        {
            SetVisible(0f);
            _weaponDrum.Changed += _ => RefreshWeapon();
            _implantDrum.Changed += _ => RefreshImplant();
            _confirm.onClick.AddListener(TryConfirm);
            _weaponUp.onClick.AddListener(() => Spin(false, -1));
            _weaponDown.onClick.AddListener(() => Spin(false, 1));
            _implantUp.onClick.AddListener(() => Spin(true, -1));
            _implantDown.onClick.AddListener(() => Spin(true, 1));

            // Кнопки — только для мыши: без навигации с клавиатуры, иначе выбранная стрелка
            // ловит Enter/Space как «нажатие» и крутит барабан вместо подтверждения
            foreach (Button button in new[] { _confirm, _weaponUp, _weaponDown, _implantUp, _implantDown })
            {
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }

            for (int i = 0; i < _weapons.Length; i++)
            {
                FillItem(_weaponDrum.Items[i], _weapons[i].Icon, _weapons[i].DisplayName, Color.white);
            }
            for (int i = 0; i < StatNames.Length; i++)
            {
                _statLabels[i].text = StatNames[i];
            }
        }

        public IEnumerator Show(int mission)
        {
            _mission = mission;
            _confirmed = false;
            _warningAlpha = 0f;
            _title.text = $"АРСЕНАЛ  //  МИССИЯ {mission}";

            int implantIndex = 0;
            for (int i = 0; i < _implants.Length; i++)
            {
                bool available = _implants[i].Mission == mission;
                if (available) implantIndex = i;
                FillItem(_implantDrum.Items[i], _implants[i].Icon, available ? _implants[i].DisplayName : "ЗАБЛОКИРОВАН",
                    available ? Color.white : _lockedTint);
            }

            // Если на эту миссию уже выбирали — барабан встаёт на прошлый выбор
            int weaponIndex = 0;
            if (Loadout.IsChosenFor(mission))
            {
                for (int i = 0; i < _weapons.Length; i++)
                {
                    if (_weapons[i].name == Loadout.WeaponId) weaponIndex = i;
                }
            }

            _weaponDrum.SetIndex(weaponIndex);
            _implantDrum.SetIndex(implantIndex);
            SetActiveDrum(false);

            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                SetVisible(t / 0.3f);
                yield return null;
            }
            SetVisible(1f);
            _group.interactable = true;
            _group.blocksRaycasts = true;

            while (!_confirmed)
            {
                HandleInput();
                _warningAlpha = Mathf.MoveTowards(_warningAlpha, 0f, 0.5f * Time.unscaledDeltaTime);
                _warning.alpha = Mathf.Clamp01(_warningAlpha * 2f);
                yield return null;
            }

            Loadout.Save(mission, ChosenWeapon.name, ChosenImplant.name);
            _group.interactable = false;
            _group.blocksRaycasts = false;
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                SetVisible(1f - t / 0.3f);
                yield return null;
            }
            SetVisible(0f);
        }

        private void HandleInput()
        {
            if (N89.UI.PauseMenu.IsPaused) return;
            // Клавиатурой управляет только сам арсенал: кликнутая стрелка не остаётся «выбранной»
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) SetActiveDrum(false);
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) SetActiveDrum(true);
                if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Spin(_implantDrumActive, -1);
                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) Spin(_implantDrumActive, 1);
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) TryConfirm();
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                // Барабан выбирается той половиной экрана, где мышь
                if (mouse.delta.ReadValue().sqrMagnitude > 1f)
                {
                    SetActiveDrum(mouse.position.ReadValue().x > Screen.width * 0.5f);
                }

                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f) Spin(_implantDrumActive, wheel > 0f ? -1 : 1);
            }
        }

        private void Spin(bool implant, int delta)
        {
            SetActiveDrum(implant);
            (implant ? _implantDrum : _weaponDrum).Step(delta);
        }

        private void SetActiveDrum(bool implant)
        {
            _implantDrumActive = implant;
            _weaponDrum.Active = !implant;
            _implantDrum.Active = implant;
        }

        private void TryConfirm()
        {
            if (ChosenImplant.Mission != _mission)
            {
                _warning.text = $"ЭТОТ ИМПЛАНТ НЕДОСТУПЕН — ОН ОТКРОЕТСЯ НА МИССИИ {ChosenImplant.Mission}";
                _warningAlpha = 1.5f;
                SetActiveDrum(true);
                return;
            }

            _confirmed = true;
        }

        private void RefreshWeapon()
        {
            WeaponData weapon = ChosenWeapon;
            _weaponName.text = weapon.DisplayName;
            _weaponDescription.text = weapon.Description;
            int[] ratings = weapon.Ratings;
            for (int row = 0; row < ratings.Length; row++)
            for (int i = 0; i < 5; i++)
            {
                _statSegments[row * 5 + i].color = i < ratings[row] ? _statOn : _statOff;
            }
        }

        private void RefreshImplant()
        {
            ImplantData implant = ChosenImplant;
            bool available = implant.Mission == _mission;
            _implantName.text = implant.DisplayName;
            _implantDescription.text = implant.Description;
            _implantStatus.text = available
                ? "<color=#4DFFF2>УСТАНОВЛЕН НА ЭТУ МИССИЮ</color>"
                : $"<color=#FF5566>ЗАБЛОКИРОВАН  //  МИССИЯ {implant.Mission}</color>";
        }

        private void SetVisible(float alpha)
        {
            _group.alpha = alpha;
        }

        private static void FillItem(RectTransform item, Sprite icon, string label, Color tint)
        {
            Image image = item.Find("Icon").GetComponent<Image>();
            image.sprite = icon;
            image.color = tint;
            item.Find("Name").GetComponent<TMP_Text>().text = label;
        }
    }
}
