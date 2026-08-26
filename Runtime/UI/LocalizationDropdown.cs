#if DATAKEEPER_LOCALIZATION

using System;
using System.Collections.Generic;
using System.Linq;
using DataKeeper.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DataKeeper.UI
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class LocalizationDropdown : MonoBehaviour
    {
        private TMP_Dropdown _dropdown;

        private void Awake() => _dropdown = GetComponent<TMP_Dropdown>();

        private void OnEnable()
        {
            _dropdown.onValueChanged.AddListener(OnValueChanged);
            Localization.WhenLocalesReady(Fill);
        }

        private void OnDisable()
        {
            _dropdown.onValueChanged.RemoveListener(OnValueChanged);
        }

        // Locales load through Addressables, so this can arrive a frame later than OnEnable —
        // or never, when the project has no localization data built.
        private void Fill(IList<Locale> locales)
        {
            if (_dropdown == null) return;

            _dropdown.options.Clear();
            _dropdown.AddOptions(locales.Select(locale => locale.LocaleName).ToList());
            _dropdown.SetValueWithoutNotify(Mathf.Clamp(Localization.SelectedLocale.Value, 0, locales.Count - 1));
            _dropdown.RefreshShownValue();
        }

        private void OnValueChanged(int option) => Localization.SetSelectedLocale(option);
    }
}

public static class Localization
{
    public static ReactivePref<int> SelectedLocale { get; private set; } = new ReactivePref<int>(0, "selected_locale");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad() => WhenLocalesReady(Apply);

    public static void SetSelectedLocale(int index)
    {
        if (SelectedLocale.Value != index) SelectedLocale.Value = index;

        WhenLocalesReady(Apply);
    }

    // Waits on the initialization operation instead of reading AvailableLocales.Locales directly:
    // that getter forces a synchronous Addressables load, which throws InvalidKeyException in a
    // project where the localization package is installed but no locale data has been built yet.
    public static void WhenLocalesReady(Action<IList<Locale>> onReady)
    {
        if (!LocalizationSettings.HasSettings) return;

        LocalizationSettings.InitializationOperation.Completed += operation =>
        {
            if (operation.Status != AsyncOperationStatus.Succeeded) return;

            IList<Locale> locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales.Count > 0) onReady(locales);
        };
    }

    private static void Apply(IList<Locale> locales)
    {
        int index = Mathf.Clamp(SelectedLocale.Value, 0, locales.Count - 1);

        // A stored index can outlive the locale it pointed at, so write the clamp back.
        if (SelectedLocale.Value != index) SelectedLocale.Value = index;

        LocalizationSettings.SelectedLocale = locales[index];
    }
}

#endif
