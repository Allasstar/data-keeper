# DataKeeper

DataKeeper is a Unity extension package that bundles reactive data types, event systems, gameplay utilities, and editor tooling into a single UPM package.

## Installation

Via OpenUPM scoped registry or git URL — see the [README](https://github.com/Allasstar/data-keeper#readme) for step-by-step instructions.

### Optional dependencies

| Package | Enables | Define |
| --- | --- | --- |
| `com.unity.addressables` | Dynamic Scene streaming (`SubScene`, `AddressableLoader`, `SubSceneManager`) | `DATAKEEPER_ADDRESSABLES` (set automatically when the package is installed) |

## Feature documentation

### Data & reactivity
- [Reactive](Reactive.md) — observable value type `Reactive<T>`
- [ReactivePref](ReactivePref.md) — reactive `PlayerPrefs`-backed preferences
- [ReactiveList](ReactiveList.md) — observable list
- [ReactiveDictionary](ReactiveDictionary.md) — observable dictionary
- [DataFile](DataFile.md) — save/load `DataFile<T>` to disk (JSON / XML / Binary)
- [SaveManager](SaveManager.md) — unified save pipeline: slots, versioning/migrations, save/load-all
- [Generic Utilities](GenericUtilities.md) — `Optional<T>`, `UnityLazy<T>`, `Register<T>`, `DirtyTracker<T>`, `DeferredList<T>`, `QueueFixedSized<T>`, `JsonData<T>`

### Events & flow
- [Signals](Signals.md) — zero-GC signal/event system with ScriptableObject channels
- [FSM](FSM.md) — generic finite state machine with transitions, cooldowns, and history
- [Act](Act.md) — static coroutine runner, timers, interpolation, and chains
- [BeeTween](BeeTween.md) — node-based, inspector-authorable tween/sequence player

### Gameplay systems
- [GameTags](GameTags.md) — hierarchical gameplay tags (port of Unreal's GameplayTags)
- [Blackboard](Blackboard.md) — tag-keyed typed data store for AI/gameplay state
- [ValueProviders](ValueProviders.md) — pluggable "where does this value come from" strategy assets
- [Pity](Pity.md) — weighted random drops with pity and guaranteed thresholds
- [Spatial](Spatial.md) — `Octree<T>` / `Quadtree<T>` spatial queries
- [Pool](Pool.md) — component object pooling
- [ServiceLocator](ServiceLocator.md) — service registration/resolution across contexts
- [Singleton](Singleton.md) — `Singleton<T>` and `MonoSingleton<T>`
- [Initializator](Initializator.md) — bootstrap `SO` resources before scene load
- [BootstrapSO](BootstrapSO.md) — self-initializing ScriptableObjects
- [DynamicScene](DynamicScene.md) — distance-based Addressables streaming *(requires `com.unity.addressables`)*

### UI
- [UI Components](UI.md) — uGUI extensions: `ButtonUI`, `ToggleUI`, `TabsUI`, `SafeAreaUI`, layout groups, drag/resize handles, reactive UI bindings (`BindTo`)
- [UIToolkit](UIToolkit.md) — fluent extensions for UI Toolkit

### Debugging & editor
- [Debugger](Debugger.md) — `DebugLog`, `DebugDraw`, `DebugPrint` runtime debug helpers
- [Attributes](Attributes.md) — inspector attributes (`[Button]`, `[ShowIf]`, `[ReadOnlyInspector]`, …)
- [Editor Tools](EditorTools.md) — editor windows and menu items under `Tools/`

## Settings

Package preferences live under `Edit > Preferences > Data Keeper`.
