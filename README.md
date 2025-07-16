# **DataKeeper**

**DataKeeper** is a comprehensive package/Unity extension that enhances the functionality and convenience of Unity development. It includes a collection of scripts designed to streamline common tasks and improve efficiency. From reactive variables and preferences to data serialization and registration systems, DataKeeper offers a wide range of tools to simplify your workflow.


# [OpenUPM](https://openupm.com/packages/com.micrarriors.data-keeper/)
[![openupm](https://img.shields.io/npm/v/com.micrarriors.data-keeper?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.micrarriors.data-keeper/)

# **Install via Git URL**

**Latest**
`https://github.com/Allasstar/data-keeper.git`

**Specific Version**
`https://github.com/Allasstar/data-keeper.git#0.19.0`


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

## [BootstrapSO](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/BootstrapSO.md)

The `BootstrapSO` class, located within the `DataKeeper.Extra` namespace, is a specialized Scriptable Object designed for managing scene loading and object instantiation during the initialization phase of a Unity project. It acts as a bootstrapper, managing the loading of scenes, initialization of objects, and setup of required resources at the start of a game or application.


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

## [Signals](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Signals.md)

The `DataKeeper.Signals` namespace provides a set of utilities and abstractions for implementing a signal-based event-driven system. Signals enable communication between different objects or parts of the system in a decoupled manner. This namespace is designed for scenarios where event management, persistent signals, and runtime callbacks are crucial.
It includes foundational `Signal` classes for event invocation and listener management, as well as classes tailored for Unity integration via `ScriptableObjects`. These components are suitable for building reusable and extendable event systems.



# DataKeeper Namespace Documentation

The `DataKeeper` namespace provides a suite of tools and utilities designed to enhance Unity development, offering solutions for reactive programming, data management, service location, object pooling, and event signaling.

## Sub-Namespaces

-   **`DataKeeper.Attributes`**: Contains custom attributes to extend the functionality of the Unity Inspector.
-   **`DataKeeper.Editor`**: Includes editor-related scripts and extensions for improving the Unity editor experience.
-   **`DataKeeper.Editor.Enhance`**: Contains scripts to enhance the editor, such as adding icons to the hierarchy.
-   **`DataKeeper.Editor.Settings`**: Includes settings providers for the DataKeeper package, allowing users to configure preferences via the Unity settings window.
-   **`DataKeeper.FSM`**: Provides classes for implementing Finite State Machines (FSM).
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

### `DataKeeper.Generic`

-   **`DataFile<T>`**: A generic class for saving and loading data to a file.
    -   `Data`: The data to be saved or loaded.
    -   `SaveData()`: Saves the data to a file.
    -   `LoadData()`: Loads the data from a file.
    -   `IsFileExist()`: Checks if the data file exists.
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

