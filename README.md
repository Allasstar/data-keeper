# **DataKeeper**

**DataKeeper** is a comprehensive package/Unity extension that enhances the functionality and convenience of Unity development. It includes a collection of scripts designed to streamline common tasks and improve efficiency. From reactive variables and preferences to data serialization and registration systems, DataKeeper offers a wide range of tools to simplify your workflow.


# [OpenUPM](https://openupm.com/packages/com.micrarriors.data-keeper/)
[![openupm](https://img.shields.io/npm/v/com.micrarriors.data-keeper?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.micrarriors.data-keeper/)

# **Install via git URL**

**Latest:**
`https://github.com/Allasstar/data-keeper.git`

**Specific Version:**
`https://github.com/Allasstar/data-keeper.git#0.90.0`


# **Install via Package Manager**

Please follow the instrustions:

-   open  **Edit/Project Settings/Package Manager**
-   add a new Scoped Registry (or edit the existing OpenUPM entry)
    
    Name
    `package.openupm.com`
    
    URL
    `https://package.openupm.com`
    
    Scope(s)
    `com.micrarriors.data-keeper`
    
-   click  **Save**  or  **Apply**
-   open  **Window/Package Manager**
-   click  **+**
-   select  **Add package by name...**  or  **Add package from git URL...**
-   paste  `com.micrarriors.data-keeper`  into name
-   paste  `#.#.#`  into version (example: 0.6.0)
-   click  **Add**


# --- Documentation ---

## Settings
`Edit > Preferences > Data Keeper`

## [Initializator](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Initializator.md)

The `Initializator` class is a static utility located in the `DataKeeper.Init` namespace. It serves as an initialization helper that loads and initializes all `SO` (Scriptable Object) resources at a specific moment during runtime. This can be particularly useful to set up and prepare resources before a scene is loaded.

## [Reactive](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md)

The `Reactive<T>` class, located within the `DataKeeper.Generic` namespace, provides a generic reactive data type that can track and trigger events when its value changes. This feature is useful in scenarios where you want to maintain and observe the state of a value reactively.

## [ReactivePref](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/ReactivePref.md)

The `ReactivePref<T>` class, located in the `DataKeeper.Generic` namespace, offers a generic mechanism for storing and managing reactive preferences in Unity. Built with `PlayerPrefs` as the underlying storage, this class enables seamless handling of different data types in a reactive way. It includes features such as event-driven updates on changes, auto-saving, and serialization support for custom data types.

## [ReactiveList](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/ReactiveList.md)

The `ReactiveList<T>` class, located in the `DataKeeper.Generic` namespace, is a reactive list implementation that allows tracking changes to its elements and triggering events. This class is particularly useful for scenarios in reactive programming, where you need to observe or respond to changes in the list dynamically.

## [ReactiveDictionary](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/ReactiveDictionary.md)

The `ReactiveDictionary<TKey, TValue>` class, located within the `DataKeeper.Generic` namespace, provides a generic dictionary with reactive capabilities. This dictionary triggers events when changes are made to its contents, such as elements being added, removed, updated, or cleared. This is especially useful in scenarios where observation patterns are necessary for synchronization, dynamic updates in UI, or other reactive programming use cases.


## [ServiceLocator](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/ServiceLocator.md)

The `ServiceLocator` system, located in the `DataKeeper.ServiceLocatorPattern` namespace, is designed to facilitate dependency injection and service management across global, scene-specific, and GameObject-specific contexts. It allows for services to be registered and resolved dynamically, adhering to the `Service Locator` design pattern.

## [Pool<T>](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Pool.md)

The `Pool<T>` class, located within the `DataKeeper.PoolSystem` namespace, is a generic implementation of an object pooling system. It provides the functionality to manage, reuse, and recycle instances of a given `Component`. This class is designed for efficient runtime object management, which is particularly useful in scenarios like Unity game development.

# [UIToolkit](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/UIToolkit.md)

The `DataKeeper.UIToolkit` namespace is a comprehensive extension library for Unity's UI Toolkit system, providing fluent API extensions and utilities to streamline UI development. This namespace contains various extension classes that make it easier to work with Unity's `VisualElement` system by providing method chaining capabilities and simplified styling operations.


## [Signals](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Signals.md)

The `DataKeeper.Signals` namespace provides a set of utilities and abstractions for implementing a signal-based event-driven system. Signals enable communication between different objects or parts of the system in a decoupled manner. This namespace is designed for scenarios where event management, persistent signals, and runtime callbacks are crucial.
It includes foundational `Signal` classes for event invocation and listener management, as well as classes tailored for Unity integration via `ScriptableObjects`. These components are suitable for building reusable and extendable event systems.


## GameTag

The `DataKeeper.GameTagSystem` namespace is a port of Unreal Engine's **GameplayTags** — hierarchical, path-based tags (e.g. `Damage/Elemental/Fire`) used to classify and query gameplay state in a decoupled, data-driven way.

A `GameTag` is a **lightweight, zero-GC struct** that stores only a stable `int` id into a `GameTagRegistry` asset (the source of truth for the tag tree). Because references hold the id and not the path, **renaming or moving a tag never breaks existing references**, and deleting a tag can redirect old references to a replacement.

### Concepts

- **Tree** — tags form a path-separated hierarchy. A leaf carries its whole ancestry, so `Damage/Elemental/Fire` belongs to both the `Damage/Elemental` and `Damage` branches.
- **Registry** — `GameTagRegistry` (a `ScriptableObject`) bakes the authored tag list into runtime caches (paths, depth, parent/child links, per-node ancestor chains for O(1) matching, redirects). The active one is `GameTagRegistry.Default`, loaded from `Resources`.
- **Redirects** — when a tag is deleted/merged, its old id can resolve to a replacement, so stored references keep working.

### Matching (`GameTag`)

All matching mirrors Unreal's semantics and is redirect-aware. Tree used below: `Damage/Elemental/{Fire,Ice}`, `Damage/Physical`, `Status/Burning`.

```csharp
var fire = GameTag.Find("Damage/Elemental/Fire");

// Hierarchical — true for the tag itself or any ancestor branch
fire.MatchesTag(GameTag.Find("Damage/Elemental")); // true  — immediate parent
fire.MatchesTag(GameTag.Find("Damage"));           // true  — deeper ancestor
fire.MatchesTag(GameTag.Find("Damage/Physical"));  // false — cousin branch
GameTag.Find("Damage").MatchesTag(fire);           // false — a parent never matches its child

// Exact — same node only (ignores hierarchy), redirect-aware
fire.MatchesTagExact(GameTag.Find("Damage/Elemental")); // false

// Strictly under (descendant, not self)
fire.IsChildOf(GameTag.Find("Damage"));            // true

// Match closeness — shared ancestor count (0 = unrelated)
fire.MatchesTagDepth(GameTag.Find("Damage/Elemental/Ice")); // 2 (share Damage/Elemental)
```

| Method | Meaning | Unreal equivalent |
| --- | --- | --- |
| `MatchesTag(other)` | this tag, or any ancestor, equals `other` | `MatchesTag` |
| `MatchesTagExact(other)` | same node only (redirect-aware) | `MatchesTagExact` |
| `MatchesAny(container)` / `MatchesAnyExact(container)` | matches any tag in a set | `MatchesAny` / `MatchesAnyExact` |
| `MatchesTagDepth(other)` | shared ancestor count (`int`) | `MatchesTagDepth` |
| `IsChildOf(other)` | strict descendant (extension) | — |
| `Find(path)` / `TryFind(path, out tag)` | resolve an existing tag by path (`TryFind` reports a miss) | `RequestGameplayTag` |
| `GetGameTagParents()` / `GetSingleTagContainer()` | project into a container (self + ancestors / just this) | `GetGameplayTagParents` / `GetSingleTagContainer` |
| `CompareTo(other)` / `GameTag.None` | ordering by raw id (`IComparable`); canonical invalid handle | — |
| `==` / `Equals` / `GetHashCode` | structural identity by raw id (collection key) | `operator==` |

> `==`/`Equals` compare the raw id (a stable, zero-GC dictionary key) and are **not** redirect-aware; use `MatchesTagExact` for "is this the same tag" after a rename/merge.
>
> `Find` returns an invalid tag on an unknown path; prefer `TryFind` when a typo should be caught rather than silently propagated. `Name`/`Path` return an empty string (never `null`) for an unknown/invalid tag.

### Containers (`GameTagContainer`)

A set of tags with the same hierarchical queries — the port of `FGameplayTagContainer`. Hierarchical queries (`HasTag`, `HasAny`, `HasAll`, …) match against the parent branches of contained tags; the `*Exact` variants only match literal members.

```csharp
var tags = new GameTagContainer();
tags.AddTag(GameTag.Find("Damage/Elemental/Fire"));

tags.HasTag(GameTag.Find("Damage"));      // true  — covered via hierarchy
tags.HasTagExact(GameTag.Find("Damage")); // false — "Damage" was never added explicitly
```

Key methods (Unreal parity): `HasTag`/`HasTagExact`, `HasAny`/`HasAnyExact`, `HasAll`/`HasAllExact`, `AddTag`/`AddTagFast`/`AddLeafTag`, `RemoveTag`/`RemoveTags`, `Reset`, `AppendTags`, `Num`/`IsEmpty`/`IsValid`, `GetByIndex`/`First`/`Last`, `Filter`, `GetGameTagParents`.

- **Canonical ids** — mutations resolve rename/merge redirects on entry, so the container stores only canonical ids: a retired handle and its replacement behave as the same tag for add, remove, and listener registration.
- **Performance** — hierarchical matching is O(1) (the registry bakes each node's root-to-self ancestor chain). Queries are zero-GC in steady state; a container holding 8+ tags lazily builds a cached expanded-ancestor set on its first hierarchical query, after which `HasTag` (and `HasAny`/`HasAll` on top of it) is a single hash probe per tag. The set is maintained incrementally and rebuilt automatically if the registry re-bakes.

### Observing a container

A `GameTagContainer` is **observable**: every mutation raises change events, built on the package's zero-GC `Signal`. All event infrastructure is created lazily on the first subscription and released again when the last listener is removed, so an unobserved container keeps its plain-data allocation profile and pays only constant work per mutation. Firing is allocation-free. Changes are reported as `GameTagChangeType` (`Added` / `Removed`).

There are four subscription granularities:

```csharp
var tags = new GameTagContainer();

// 1. Any change — receives the changed tag and the kind.
tags.AddListener((tag, change) => Debug.Log($"{tag.Path} {change}"));

// 2. A specific tag, exact — descendants are ignored.
tags.AddTagListener(GameTag.Find("Stunned"),
    change => animator.SetBool("stunned", change == GameTagChangeType.Added));

// 3. A branch — fires for the tag OR any descendant (raw, one event per matching change).
tags.AddBranchListener(GameTag.Find("Damage"),
    (tag, change) => Debug.Log($"{change}: {tag.Path}")); // e.g. Damage/Elemental/Fire

// 4. A branch's presence — deduped present/absent transitions only.
tags.AddBranchPresenceListener(GameTag.Find("Buff/Shield"), shield.SetActive);

tags.AddTag(GameTag.Find("Damage/Elemental/Fire"));
// → any-listener fires (Fire, Added)
// → branch-listener on "Damage" fires (Fire, Added)   [ancestor walk]
// → exact-listener on "Damage" would NOT fire
```

| Subscribe with | Fires when | Callback |
| --- | --- | --- |
| `AddListener` | any tag added/removed | `Action<GameTag, GameTagChangeType>` |
| `AddTagListener(tag, …)` | *that* tag added/removed (exact) | `Action<GameTagChangeType>` |
| `AddBranchListener(parent, …)` | the tag **or any descendant** changes (raw) | `Action<GameTag, GameTagChangeType>` |
| `AddBranchPresenceListener(parent, …)` | branch goes present ↔ absent (deduped) | `Action<bool>` |

Each has a matching `Remove…` method, plus `RemoveAllListeners()`. Listener keys are redirect-resolved (subscribing with a retired handle observes its replacement), and removing the last listener releases the observation infrastructure.

- **How the hierarchy works** — every mutator routes through one internal notify that fires the global signal, the exact-keyed signal, then walks the changed tag's `self → parent → …root` chain firing any branch/presence listener registered at each level. The walk only runs when a branch or presence listener exists.
- **Raw vs presence** — with both `Damage/Fire` and `Damage/Ice` present, a `Branch` listener on `Damage` fires `Removed(Fire)` even though the branch is still present via `Ice`; a `BranchPresence` listener fires only on the flip to empty. Presence is backed by a per-branch ref-count, so it stays correct even with duplicate entries from `AddTagFast`.
- **Notes** — `AddBranchPresenceListener` does not fire on subscribe, but seeds its count from the current state so the next transition is correct (read `HasTag(parent)` for the initial value). `Reset`/`Clear` emit a `Removed` per tag (and drive present branches to `absent`). Mutators are safe against listeners that mutate the container from the callback.

### Authoring

Edit the tag tree on a `GameTagRegistry` asset (`Create > DataKeeper > GameTagRegistry`); use the picker/drawer in the inspector. The registry can regenerate a static `GameTags` C# class so tags are available as compile-time constants. Every type member is documented with XML doc comments (hover in your IDE for per-case examples).



# DataKeeper Namespace Documentation

The `DataKeeper` namespace provides a suite of tools and utilities designed to enhance Unity development, offering solutions for reactive programming, data management, service location, object pooling, and event signaling.

## Sub-Namespaces

-   **`DataKeeper.Attributes`**: Contains custom attributes to extend the functionality of the Unity Inspector.
-   **`DataKeeper.Editor`**: Includes editor-related scripts and extensions for improving the Unity editor experience.
-   **`DataKeeper.Editor.Enhance`**: Contains scripts to enhance the editor, such as adding icons to the hierarchy.
-   **`DataKeeper.Editor.Settings`**: Includes settings providers for the DataKeeper package, allowing users to configure preferences via the Unity settings window.
-   **`DataKeeper.FSM`**: Provides classes for implementing Finite State Machines (FSM).
-   **`DataKeeper.GameTagSystem`**: A port of Unreal's GameplayTags — hierarchical, path-based tags with zero-GC handles and hierarchical queries.
-   **`DataKeeper.Generic`**: Offers generic data structures and classes, including reactive variables, data files, and fixed-size queues.
-   **`DataKeeper.Helpers`**: Contains helper classes and utility functions.
-   **`DataKeeper.Init`**: Includes the `Initializator` class for initializing Scriptable Objects.
-   **`DataKeeper.PoolSystem`**: Provides a generic object pooling system.
-   **`DataKeeper.ServiceLocatorPattern`**: Implements the Service Locator pattern for dependency injection.
-   **`DataKeeper.Signals`**: Offers a signal-based event-driven system.

## Key Classes

### `DataKeeper.Attributes`

-   **`StaticClassInspectorAttribute`**: An attribute used to mark static classes for custom inspector display.
    -   `Category`: Specifies the category in which the static class should be displayed in the inspector.
-   **`ReadOnlyInspectorAttribute`**: An attribute to mark properties as read-only in the inspector.

### `DataKeeper.Editor`

-   **`SerializedPropertyExtensions`**: Provides extension methods for `SerializedProperty` to retrieve the instance of the property.
    -   `GetPropertyInstance(this SerializedProperty property)`: Gets the object instance that the serialized property represents.

### `DataKeeper.Editor.Settings`

-   **`DataKeeperPreferences`**: Provides a settings provider for the DataKeeper package, allowing users to configure preferences.
    -   `CreateDataKeeperPreferences()`: Creates the settings provider.

### `DataKeeper.FSM`

-   **`FSM`**: Base class for creating Finite State Machines.
    -   `ChangeState(FSMState nextState)`: Changes the current state of the FSM.
    -   `Update()`: Updates the current state.
-   **`FSMHistory`**: Manages the history of states in a Finite State Machine.
    -   `RegisterState(FSMState state)`: Registers a state in the history.
    -   `GetLastState()`: Gets the last state from the history.
    -   `Clear()`: Clears the state history.
-   **`FSMState`**: Abstract base class for FSM states.
    -   `OnEnter()`: Called when the state is entered.
    -   `OnExit()`: Called when the state is exited.
    -   `OnUpdate()`: Called every frame while the state is active.

### `DataKeeper.GameTagSystem`

-   **`GameTag`**: A zero-GC, ordered (`IComparable`) struct handle to a node in the tag tree; resolves name/path/hierarchy through the registry.
    -   `MatchesTag(other)` / `MatchesTagExact(other)`: Hierarchical / exact (redirect-aware) match.
    -   `MatchesAny(container)` / `MatchesAnyExact(container)`: Match against any tag in a set.
    -   `MatchesTagDepth(other)`: Shared-ancestor count (match closeness).
    -   `IsChildOf(other)`: Strict descendant test.
    -   `Find(path)` / `TryFind(path, out tag)` / `FromId(id)`: Look up an existing tag by path or raw id; `GameTag.None` is the invalid handle.
    -   `GetGameTagParents()` / `GetSingleTagContainer()`: Project the tag into a container.
-   **`GameTagContainer`**: An observable set of `GameTag`s with hierarchical queries (port of `FGameplayTagContainer`).
    -   `HasTag` / `HasTagExact`, `HasAny` / `HasAll` (+ `*Exact`): Membership queries.
    -   `AddTag` / `AddLeafTag` / `RemoveTag` / `Filter` / `GetGameTagParents`: Mutation and derivation.
    -   `AddListener` / `AddTagListener` / `AddBranchListener` / `AddBranchPresenceListener`: Subscribe to changes at global, exact-tag, branch (raw), or branch-presence (deduped) granularity.
-   **`GameTagRegistry`**: The `ScriptableObject` source of truth that bakes the authored tag tree (paths, hierarchy, ancestor chains, redirects) into runtime caches for O(1) hierarchical matching.

### `DataKeeper.Generic`

-   **`DataFile<T>`**: A generic class for saving and loading data to a file.
    -   `Data`: The data to be saved or loaded.
    -   `SaveData()`: Saves the data to a file.
    -   `LoadData()`: Loads the data from a file.
    -   `IsFileExist()`: Checks if the data file exists.
-   **`DataKeeperStorage`**: Static holder of the global storage defaults (`Files`, `Prefs`) and the active save slot — swap the providers here to store everything in the cloud (Steam, Unity Cloud Save, ...).
-   **`IStorageProvider`** / **`LocalFileStorage`**: Pluggable key-based storage backend used by `DataFile<T>` and `SaveManager` (default writes files under `persistentDataPath`). Swap globally via `DataKeeperStorage.Files` or per file via the `DataFile<T>` constructor.
-   **`IPrefsStorage`** / **`PlayerPrefsStorage`**: Pluggable key-value backend used by `ReactivePref<T>` (default: Unity `PlayerPrefs`). Swap globally via `DataKeeperStorage.Prefs` or per pref via the constructor.
-   **`QueueFixedSized<T>`**: A fixed-size queue based on `ConcurrentQueue`.
    -   `Size`: The maximum size of the queue.
    -   `Enqueue(T obj)`: Enqueues an object, removing the oldest object if the queue is full.

### `DataKeeper.Helpers`

-   **`FolderHelper`**: Provides helper methods for creating and managing folders.
    -   `CreateFolders(string path)`: Creates all directories in the specified path.
    -   `AllFoldersExist(string path)`: Checks if all folders in the specified path exist.

### `DataKeeper.Init`

-   **`Initializator`**: A static utility class for loading and initializing Scriptable Object resources.

### `DataKeeper.PoolSystem`

-   **`Pool<T>`**: A generic object pooling system for managing and reusing instances of a given component.

### `DataKeeper.ServiceLocatorPattern`

-   **`ServiceLocator`**: Implements the Service Locator pattern for dependency injection and service management.

### `DataKeeper.Signals`

-   **`Signal`**: A basic signal class for event invocation and listener management.
    -   `AddListener(Action listener)`: Adds a listener to the signal.
    -   `RemoveListener(Action listener)`: Removes a listener from the signal.
    -   `Invoke()`: Invokes all listeners.
-   **`Signal<T0>`**: A generic signal class that passes one parameter to its listeners.
    -   `AddListener(Action<T0> listener)`: Adds a listener to the signal.
    -   `RemoveListener(Action<T0> listener)`: Removes a listener from the signal.
    -   `Invoke(T0 arg0)`: Invokes all listeners with the specified argument.
-   **`SignalBase`**: Abstract base class for signals, providing core functionality for listener management.
    -   `Listeners`: A list of listeners (delegates) attached to the signal.
    -   `AddListener(Delegate listener)`: Adds a delegate to the list of listeners.
    -   `RemoveListener(Delegate listener)`: Removes a delegate from the list of listeners.
    -   `InvokeListeners(params object[] parameters)`: Invokes all listeners with the given parameters.

