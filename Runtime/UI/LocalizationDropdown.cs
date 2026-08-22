#if DATAKEEPER_LOCALIZATION

using System.Linq;
using DataKeeper.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace DataKeeper.UI
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class LocalizationDropdown : MonoBehaviour
    {
        private TMP_Dropdown _dropdown;

        private void Awake()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
            _dropdown.options.Clear();
            _dropdown.AddOptions(LocalizationSettings.AvailableLocales.Locales.Select(s => s.LocaleName).ToList());
        }

        private void OnEnable()
        {
            _dropdown.value = Localization.SelectedLocale.Value;
            _dropdown.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable()
        {
            _dropdown.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(int option)
        {
            Localization.SelectedLocale.Value = option;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[option];
        }
    }
}

public static class Localization
{
    public static ReactivePref<int> SelectedLocale { get; private set; } = new ReactivePref<int>(0, "selected_locale");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[Localization.SelectedLocale.Value];
    }
}

#endif
