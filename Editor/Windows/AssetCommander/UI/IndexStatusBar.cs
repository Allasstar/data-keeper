using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Polls ProjectIndex on the editor tick rather than listening for progress events —
    // the workers only bump a counter, and a signal per indexed asset would be tens of
    // thousands of main-thread invocations for a bar that redraws 60 times a second anyway.
    public sealed class IndexStatusBar : IDisposable
    {
        private const string ProgressVisibleClass = "ac-index-progress--visible";
        private const string NoticeVisibleClass = "ac-notice--visible";

        private readonly Label _status;
        private readonly ProgressBar _progress;
        private readonly ToolbarButton _rebuild;
        private readonly VisualElement _notice;

        private string _lastText;
        private bool _lastBuilding;
        private bool _lastDegraded;
        private bool _initialised;

        public IndexStatusBar(VisualElement root)
        {
            _status = root.Q<Label>("index-status");
            _progress = root.Q<ProgressBar>("index-progress");
            _rebuild = root.Q<ToolbarButton>("rebuild-index");
            _notice = root.Q<VisualElement>("notice");

            if (_rebuild != null)
            {
                _rebuild.tooltip = "Re-scan every asset from disk, ignoring the cache.";
                _rebuild.clicked += OnRebuildClicked;
            }

            Refresh();
            EditorApplication.update += Refresh;
        }

        public void Dispose()
        {
            EditorApplication.update -= Refresh;
            if (_rebuild != null) _rebuild.clicked -= OnRebuildClicked;
        }

        private static void OnRebuildClicked() => ProjectIndex.RequestRebuild();

        private void Refresh()
        {
            bool building = ProjectIndex.State == IndexState.Building;
            bool degraded = !ProjectIndex.TextScanningEnabled;
            var text = ProjectIndex.StatusText;

            if (building && _progress != null) _progress.value = ProjectIndex.Progress01 * 100f;

            if (_initialised && text == _lastText && building == _lastBuilding && degraded == _lastDegraded) return;

            _initialised = true;
            _lastText = text;
            _lastBuilding = building;
            _lastDegraded = degraded;

            if (_status != null) _status.text = text;
            if (_rebuild != null) _rebuild.SetEnabled(!building);

            if (_progress != null) _progress.EnableInClassList(ProgressVisibleClass, building);

            UpdateNotice(degraded);
        }

        private void UpdateNotice(bool degraded)
        {
            if (_notice == null) return;

            if (degraded && _notice.childCount == 0)
                _notice.Add(new HelpBox(
                    "Asset Serialization is not set to Force Text, so the index cannot read "
                    + "dependencies from disk. It is falling back to AssetDatabase.GetDependencies, "
                    + "which is slower and cannot detect missing scripts. "
                    + "Project Settings > Editor > Asset Serialization > Force Text.",
                    HelpBoxMessageType.Warning));

            _notice.EnableInClassList(NoticeVisibleClass, degraded);
        }
    }
}
