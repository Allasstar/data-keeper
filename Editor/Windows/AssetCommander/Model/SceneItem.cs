using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    public sealed class GameObjectItem : ICommanderItem
    {
        private readonly GameObject _gameObject;
        private readonly int _componentCount;

        private string _subLabel;
        private string _badge;
        private string _badgeOverride;
        private bool _badgeOverrideIsAlert;
        private Texture _icon;
        private GlobalObjectId? _sceneId;
        private int _missingScripts = -1;
        private int _assetReferences = -1;

        public GameObjectItem(GameObject gameObject, bool showComponents)
        {
            _gameObject = gameObject;
            _componentCount = gameObject.GetComponentCount();

            Id = CommanderItemIds.ForSceneObject(gameObject.GetInstanceID());
            Name = gameObject.name;
            ChildCount = gameObject.transform.childCount;
            HasChildren = ChildCount > 0 || (showComponents && _componentCount > 0);
        }

        public int Id { get; }
        public string Name { get; }
        public int ChildCount { get; }
        public bool HasChildren { get; }

        public GameObject GameObject => _gameObject;

        public CommanderItemKind Kind => CommanderItemKind.GameObject;
        public string Guid => null;
        public string AssetPath => null;
        public long Size => 0;
        public long ModifiedTicks => 0;

        public Texture Icon => _icon ??= AssetPreview.GetMiniThumbnail(_gameObject);

        // Everything below is pulled on first bind, not at level-build time: a scene level can
        // be hundreds of objects wide, and only the rows actually on screen are worth a
        // SerializedObject walk or a GlobalObjectId.
        public string SubLabel => _subLabel ??= BuildSubLabel();

        // A flat result set loses the hierarchy that told the user where the object was, so a
        // mode puts the path back in place of the component summary.
        public void SetSubLabel(string value) => _subLabel = value;

        public GlobalObjectId? SceneId => _sceneId ??= GlobalObjectId.GetGlobalObjectIdSlow(_gameObject);

        public string Badge => _badgeOverride ?? (_badge ??= BuildBadge());

        // Whether the badge is an alert falls out of building it, so the build has to have run.
        public bool BadgeIsAlert
        {
            get
            {
                if (_badgeOverride != null) return _badgeOverrideIsAlert;

                _ = Badge;
                return _missingScripts > 0;
            }
        }

        // An analysis mode says something more specific about the object than the default
        // reference count, and its answer wins.
        public void SetBadge(string text, bool alert = false)
        {
            _badgeOverride = text;
            _badgeOverrideIsAlert = alert;
        }

        private string BuildSubLabel()
        {
            if (_gameObject == null) return "";

            for (int i = 0; i < _componentCount; i++)
            {
                var component = _gameObject.GetComponentAtIndex(i);
                if (component == null || component is Transform) continue;

                var type = component.GetType().Name;
                int rest = _componentCount - 2;
                return rest > 0
                    ? type + " +" + rest.ToString(CultureInfo.InvariantCulture)
                    : type;
            }

            return "GameObject";
        }

        private string BuildBadge()
        {
            if (_gameObject == null) return "";

            _missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(_gameObject);
            if (_missingScripts > 0)
                return _missingScripts == 1
                    ? "missing script"
                    : _missingScripts.ToString(CultureInfo.InvariantCulture) + " missing scripts";

            _assetReferences = CountAssetReferences();
            return _assetReferences == 0
                ? ""
                : _assetReferences.ToString(CultureInfo.InvariantCulture) + " refs";
        }

        // Distinct project assets this object's own components point at — children are their own
        // rows and carry their own counts. m_Script is skipped: every MonoBehaviour references
        // its own MonoScript asset, so counting it would add a constant nobody can act on.
        private int CountAssetReferences()
        {
            var seen = new HashSet<int>();

            for (int i = 0; i < _componentCount; i++)
            {
                var component = _gameObject.GetComponentAtIndex(i);
                if (component == null) continue;

                using var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (property.propertyPath == "m_Script") continue;

                    var reference = property.objectReferenceValue;
                    if (reference == null || !EditorUtility.IsPersistent(reference)) continue;

                    seen.Add(reference.GetInstanceID());
                }
            }

            return seen.Count;
        }
    }

    public sealed class ComponentItem : ICommanderItem
    {
        private readonly Component _component;

        private Texture _icon;
        private GlobalObjectId? _sceneId;

        public ComponentItem(Component component, GameObject owner, int index)
        {
            _component = component;

            // A missing script has no component to key on, so its row is identified by the slot
            // it occupies on its owner.
            Id = component == null
                ? CommanderItemIds.For("c:" + owner.GetInstanceID().ToString(CultureInfo.InvariantCulture)
                                       + ":" + index.ToString(CultureInfo.InvariantCulture))
                : CommanderItemIds.ForSceneObject(component.GetInstanceID());

            if (component == null)
            {
                Name = "Missing Script";
                SubLabel = "";
                return;
            }

            var type = component.GetType();
            Name = type.Name;
            SubLabel = type.Namespace ?? "";
        }

        public int Id { get; }
        public string Name { get; }
        public string SubLabel { get; }

        public Component Component => _component;

        public CommanderItemKind Kind => CommanderItemKind.Component;
        public string Guid => null;
        public string AssetPath => null;
        public bool HasChildren => false;
        public long Size => 0;
        public long ModifiedTicks => 0;

        public string Badge => _component == null ? "missing" : null;
        public bool BadgeIsAlert => _component == null;

        public Texture Icon => _component == null
            ? EditorGUIUtility.FindTexture("console.warnicon.sml")
            : _icon ??= AssetPreview.GetMiniThumbnail(_component);

        public GlobalObjectId? SceneId => _component == null
            ? null
            : _sceneId ??= GlobalObjectId.GetGlobalObjectIdSlow(_component);
    }
}
