using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DataKeeper.UI
{
    [AddComponentMenu("DataKeeper/UI/Tabs UI")]
    public class TabsUI : MonoBehaviour
    {
        public enum TabMode
        {
            /// <summary>Only one tab can be active at a time.</summary>
            Single = 0,
            /// <summary>Multiple tabs can be active simultaneously.</summary>
            Multiple = 1,
        }

        [Serializable]
        public struct Tab
        {
            public ToggleUI toggle;
            public GameObject panel;
        }

        // Stored per-tab delegates so we RemoveListener on exactly ours —
        // never RemoveAllListeners, which would destroy external subscribers.
        private struct TabListeners
        {
            public UnityAction<bool> onValueChanged;
            public UnityAction       onBecameInteractable;
            public UnityAction       onBecameNonInteractable;
        }

        // ── Inspector ────────────────────────────────────────────────────
        [SerializeField] private TabMode _tabMode         = TabMode.Single;

        [Tooltip("When true, all tabs can be turned off (no tab is forced active).")]
        [SerializeField] private bool _allowNone          = false;

        [SerializeField] private int  _defaultTabIndex    = 0;
        [SerializeField] private Tab[] _tabs;

        // ── Runtime state ────────────────────────────────────────────────
        private readonly List<Tab>          _runtimeTabs   = new();
        private readonly List<TabListeners> _listeners     = new();
        private readonly List<bool>         _activeStates  = new(); // authoritative for all modes
        private readonly List<bool>         _enabledStates = new();

        // Index of the single active tab (Single mode) or the last opened tab (Multiple mode).
        private int _activeIndex = -1;
        public  int  ActiveIndex  => _activeIndex;

        public IReadOnlyList<Tab>  Tabs          => _runtimeTabs;
        public IReadOnlyList<bool> ActiveStates  => _activeStates;
        public IReadOnlyList<bool> EnabledStates => _enabledStates;

        // ── Unity lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            foreach (var tab in _tabs)
                RegisterTab(tab);

            ApplyDefault();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _runtimeTabs.Count; i++)
                Unsubscribe(_runtimeTabs[i], _listeners[i]);
        }

        // ── Subscribe / Unsubscribe ──────────────────────────────────────

        private TabListeners Subscribe(Tab tab, int index)
        {
            var l = new TabListeners();
            if (tab.toggle == null) return l;

            l.onValueChanged          = isOn => OnToggleValueChanged(index, isOn);
            l.onBecameInteractable    = ()   => OnToggleBecameInteractable(index);
            l.onBecameNonInteractable = ()   => OnToggleBecameNonInteractable(index);

            tab.toggle.onValueChanged.AddListener(l.onValueChanged);
            tab.toggle.onBecameInteractable.AddListener(l.onBecameInteractable);
            tab.toggle.onBecameNonInteractable.AddListener(l.onBecameNonInteractable);

            return l;
        }

        private void Unsubscribe(Tab tab, TabListeners l)
        {
            if (tab.toggle == null) return;

            if (l.onValueChanged          != null) tab.toggle.onValueChanged.RemoveListener(l.onValueChanged);
            if (l.onBecameInteractable    != null) tab.toggle.onBecameInteractable.RemoveListener(l.onBecameInteractable);
            if (l.onBecameNonInteractable != null) tab.toggle.onBecameNonInteractable.RemoveListener(l.onBecameNonInteractable);
        }

        // ── Toggle callbacks ─────────────────────────────────────────────

        private void OnToggleValueChanged(int index, bool isOn)
        {
            if (!IsValidIndex(index))   return;
            if (!_enabledStates[index]) return;

            if (_tabMode == TabMode.Single)
            {
                if (isOn)
                {
                    OpenTab(index);
                }
                else
                {
                    // User clicked the active toggle, trying to turn it off.
                    if (_allowNone)
                    {
                        // Permitted: close it.
                        ForceCloseTab(index);
                    }
                    else
                    {
                        // Not permitted: snap the toggle back on without re-firing callbacks.
                        var t = _runtimeTabs[index];
                        t.toggle?.SetIsOnWithoutNotify(true);
                        t.toggle?.UpdateUI();
                    }
                }
            }
            else // Multiple
            {
                if (!isOn && !_allowNone)
                {
                    // Block close if it would leave zero active tabs.
                    if (ActiveCount() <= 1 && _activeStates[index])
                    {
                        var t = _runtimeTabs[index];
                        t.toggle?.SetIsOnWithoutNotify(true);
                        t.toggle?.UpdateUI();
                        return;
                    }
                }
                SetTabActive(index, isOn);
            }
        }

        private void OnToggleBecameInteractable(int index)
        {
            if (!IsValidIndex(index)) return;
            _enabledStates[index] = true;

            // In Single mode, auto-open only when nothing is currently active.
            if (_tabMode == TabMode.Single && _activeIndex < 0)
                OpenTab(index);
        }

        private void OnToggleBecameNonInteractable(int index)
        {
            if (!IsValidIndex(index)) return;

            _enabledStates[index] = false;
            int closedAt = index; // capture before ForceClose clears _activeIndex
            ForceCloseTab(index);
            TryFillActive(closedAt);
        }

        // ── Internal helpers ─────────────────────────────────────────────

        private void ApplyDefault()
        {
            // Always start from a fully clean visual slate regardless of scene state.
            SilentCloseAll();

            if (_tabMode == TabMode.Single)
            {
                // Try the nominated default; fall back to nearest enabled tab.
                if (IsValidIndex(_defaultTabIndex) && _enabledStates[_defaultTabIndex])
                    OpenTab(_defaultTabIndex);
                else if (!_allowNone)
                    TryFillActive(_defaultTabIndex);
                // If _allowNone and default is disabled: leave everything closed — valid.
            }
            else // Multiple
            {
                // Open the default tab if valid and enabled.
                if (IsValidIndex(_defaultTabIndex) && _enabledStates[_defaultTabIndex])
                    SetTabActive(_defaultTabIndex, true);
                else if (!_allowNone)
                    TryFillActive(_defaultTabIndex);
            }
        }

        /// <summary>
        /// Syncs all toggle and panel visuals to off without firing any callbacks.
        /// Used exclusively during initialization.
        /// </summary>
        private void SilentCloseAll()
        {
            for (int i = 0; i < _runtimeTabs.Count; i++)
            {
                _activeStates[i] = false;
                var tab = _runtimeTabs[i];
                tab.toggle?.SetIsOnWithoutNotify(false);
                tab.toggle?.UpdateUI();
                if (tab.panel != null) tab.panel.SetActive(false);
            }
            _activeIndex = -1;
        }

        /// <summary>
        /// Closes a tab's panel and syncs its toggle silently — no callbacks fired.
        /// Callers handle finding a replacement if needed.
        /// </summary>
        private void ForceCloseTab(int index)
        {
            if (!IsValidIndex(index)) return;

            _activeStates[index] = false;
            if (index == _activeIndex) _activeIndex = -1;

            var tab = _runtimeTabs[index];
            tab.toggle?.SetIsOnWithoutNotify(false);
            tab.toggle?.UpdateUI();
            if (tab.panel != null) tab.panel.SetActive(false);
        }

        /// <summary>
        /// When _allowNone is false: if no tab is active, find the nearest enabled tab
        /// searching forward then backward from <paramref name="fromIndex"/>.
        /// In Single mode only (Multiple mode doesn't enforce one-always-active).
        /// Skips silently if all tabs are disabled.
        /// </summary>
        private void TryFillActive(int fromIndex)
        {
            // Only auto-fill when losing a tab is not allowed.
            if (_allowNone) return;

            // In Multiple mode we don't enforce a mandatory single active tab.
            if (_tabMode == TabMode.Multiple) return;

            // Already have a valid active tab.
            if (_activeIndex >= 0 && _enabledStates[_activeIndex]) return;

            int count = _runtimeTabs.Count;
            if (count == 0) return;

            int start = Mathf.Clamp(fromIndex, 0, count - 1);

            for (int d = 1; d < count; d++)
            {
                int fwd = (start + d) % count;
                if (_enabledStates[fwd]) { OpenTab(fwd); return; }

                int bwd = ((start - d) % count + count) % count;
                if (_enabledStates[bwd]) { OpenTab(bwd); return; }
            }
            // All tabs disabled → leave _activeIndex = -1. Nothing we can do.
        }

        /// <summary>Returns how many tabs are currently active.</summary>
        private int ActiveCount()
        {
            int count = 0;
            for (int i = 0; i < _activeStates.Count; i++)
                if (_activeStates[i]) count++;
            return count;
        }

        private bool IsValidIndex(int index) => index >= 0 && index < _runtimeTabs.Count;

        // ── Dynamic registration ─────────────────────────────────────────

        private void RegisterTab(Tab tab)
        {
            int index = _runtimeTabs.Count;
            _runtimeTabs.Add(tab);
            _activeStates.Add(false);

            // Use .interactable (the serialized field on Selectable) rather than
            // IsInteractable(), which also factors in CanvasGroup state that may
            // not be fully initialized during Awake.
            bool interactable = tab.toggle != null && tab.toggle.interactable;
            _enabledStates.Add(interactable);

            _listeners.Add(Subscribe(tab, index));
        }

        // ── Public: Open / Close ─────────────────────────────────────────

        /// <summary>
        /// Opens a tab by index. Respects TabMode and AllowNone.
        /// Silently skips disabled or invalid indices.
        /// </summary>
        public void OpenTab(int index)
        {
            if (!IsValidIndex(index))   return;
            if (!_enabledStates[index]) return;

            if (_tabMode == TabMode.Multiple)
            {
                SetTabActive(index, true);
                return;
            }

            // Single mode
            if (index == _activeIndex) return;

            if (_activeIndex >= 0) ForceCloseTab(_activeIndex);

            _activeIndex         = index;
            _activeStates[index] = true;

            var tab = _runtimeTabs[index];
            tab.toggle?.SetIsOnWithoutNotify(true);
            tab.toggle?.UpdateUI();
            if (tab.panel != null) tab.panel.SetActive(true);
        }

        public void OpenTab(ToggleUI toggle) => OpenTab(IndexOf(toggle));
        public void OpenTab(Tab tab)         => OpenTab(IndexOf(tab.toggle));

        /// <summary>
        /// Closes a tab by index.
        /// In Single mode: blocked when AllowNone is false.
        /// In Multiple mode: blocked when AllowNone is false and it is the last active tab.
        /// </summary>
        public void CloseTab(int index)
        {
            if (!IsValidIndex(index)) return;

            if (_tabMode == TabMode.Single)
            {
                if (!_allowNone) return; // closing not permitted
                if (_activeIndex != index) return; // not the active tab
                ForceCloseTab(index);
                return;
            }

            // Multiple mode
            if (!_allowNone && ActiveCount() <= 1 && _activeStates[index]) return;
            SetTabActive(index, false);
        }

        public void CloseTab(ToggleUI toggle) => CloseTab(IndexOf(toggle));
        public void CloseTab(Tab tab)         => CloseTab(IndexOf(tab.toggle));

        // ── Public: helpers ──────────────────────────────────────────────

        /// <summary>Explicitly sets a tab on or off, routing through the current TabMode.</summary>
        public void SetTabActive(int index, bool active)
        {
            if (!IsValidIndex(index)) return;
            if (active && !_enabledStates[index]) return;

            if (_tabMode != TabMode.Multiple)
            {
                if (active) OpenTab(index);
                else        CloseTab(index);
                return;
            }

            // Multiple mode: block close if it would leave zero active tabs.
            if (!active && !_allowNone && ActiveCount() <= 1 && _activeStates[index]) return;

            _activeStates[index] = active;
            if (active)                     _activeIndex = index;
            else if (_activeIndex == index) _activeIndex = -1;

            var tab = _runtimeTabs[index];
            tab.toggle?.SetIsOnWithoutNotify(active);
            tab.toggle?.UpdateUI();
            if (tab.panel != null) tab.panel.SetActive(active);
        }

        /// <summary>Returns true if the tab at index is currently open. Authoritative for all modes.</summary>
        public bool IsTabActive(int index) => IsValidIndex(index) && _activeStates[index];

        /// <summary>
        /// Closes all tabs. Blocked when AllowNone is false (would violate the invariant).
        /// </summary>
        public void CloseAllTabs()
        {
            if (!_allowNone) return;
            SilentCloseAll();
        }

        /// <summary>Opens all enabled tabs. Multiple mode only.</summary>
        public void OpenAllTabs()
        {
            if (_tabMode != TabMode.Multiple) return;
            for (int i = 0; i < _runtimeTabs.Count; i++)
                if (_enabledStates[i]) SetTabActive(i, true);
        }

        // ── Public: Dynamic add / remove ─────────────────────────────────

        /// <summary>Adds a new tab at runtime.</summary>
        /// <param name="tab">The tab to register.</param>
        /// <param name="openImmediately">If true, opens the tab immediately after adding.</param>
        public void AddTab(Tab tab, bool openImmediately = false)
        {
            RegisterTab(tab);

            int newIndex = _runtimeTabs.Count - 1;
            if (openImmediately)
                OpenTab(newIndex);
            else if (tab.panel != null)
                tab.panel.SetActive(false);
        }

        /// <summary>Removes a tab by index. Only this component's listeners are removed.</summary>
        public void RemoveTab(int index)
        {
            if (!IsValidIndex(index)) return;

            bool wasActive = _activeStates[index];

            Unsubscribe(_runtimeTabs[index], _listeners[index]);

            _runtimeTabs.RemoveAt(index);
            _activeStates.RemoveAt(index);
            _enabledStates.RemoveAt(index);
            _listeners.RemoveAt(index);

            // Correct _activeIndex before searching for a replacement.
            if (_activeIndex == index)     _activeIndex = -1;
            else if (_activeIndex > index) _activeIndex--;

            // Re-subscribe remaining tabs at their new (shifted) indices.
            for (int i = index; i < _runtimeTabs.Count; i++)
            {
                Unsubscribe(_runtimeTabs[i], _listeners[i]);
                _listeners[i] = Subscribe(_runtimeTabs[i], i);
            }

            if (wasActive)
                TryFillActive(index); // search from where the removed tab was
        }

        /// <summary>Removes a tab by its ToggleUI reference.</summary>
        public void RemoveTab(ToggleUI toggle)
        {
            int i = IndexOf(toggle);
            if (i >= 0) RemoveTab(i);
        }

        /// <summary>Removes a tab matched by its toggle reference.</summary>
        public void RemoveTab(Tab tab) => RemoveTab(IndexOf(tab.toggle));

        // ── Utility ──────────────────────────────────────────────────────

        private int IndexOf(ToggleUI toggle)
        {
            for (int i = 0; i < _runtimeTabs.Count; i++)
                if (_runtimeTabs[i].toggle == toggle) return i;
            return -1;
        }
    }
}