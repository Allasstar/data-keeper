# **DataKeeper**

**DataKeeper** is a comprehensive package/Unity extension that enhances the functionality and convenience of Unity development. It includes a collection of scripts designed to streamline common tasks and improve efficiency. From reactive variables and preferences to data serialization and registration systems, DataKeeper offers a wide range of tools to simplify your workflow.


# [OpenUPM](https://openupm.com/packages/com.micrarriors.data-keeper/)
`https://openupm.com/packages/com.micrarriors.data-keeper/`


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

## [Initializator](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Initializator.md)

The `Initializator` class is a static utility located in the `DataKeeper.Init` namespace. It serves as an initialization helper that loads and initializes all `SO` (Scriptable Object) resources at a specific moment during runtime. This can be particularly useful to set up and prepare resources before a scene is loaded.

## [BootstrapSO](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/BootstrapSO.md)

The `BootstrapSO` class, located within the `DataKeeper.Extra` namespace, is a specialized Scriptable Object designed for managing scene loading and object instantiation during the initialization phase of a Unity project. It acts as a bootstrapper, managing the loading of scenes, initialization of objects, and setup of required resources at the start of a game or application.

## [Reactive<T>](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md)

The `Reactive<T>` class, located within the `DataKeeper.Generic` namespace, provides a generic reactive data type that can track and trigger events when its value changes. This feature is useful in scenarios where you want to maintain and observe the state of a value reactively.

## [ReactivePref<T>](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/ReactivePref.md)

The `ReactivePref<T>` class, located in the `DataKeeper.Generic` namespace, offers a generic mechanism for storing and managing reactive preferences in Unity. Built with `PlayerPrefs` as the underlying storage, this class enables seamless handling of different data types in a reactive way. It includes features such as event-driven updates on changes, auto-saving, and serialization support for custom data types.


## [ReactiveList<T>](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/ReactiveList.md)

The `ReactiveList<T>` class, located in the `DataKeeper.Generic` namespace, is a reactive list implementation that allows tracking changes to its elements and triggering events. This class is particularly useful for scenarios in reactive programming, where you need to observe or respond to changes in the list dynamically.

## [ReactiveDictionary<TKey, TValue>](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/ReactiveDictionary.md)


The `ReactiveDictionary<TKey, TValue>` class, located within the `DataKeeper.Generic` namespace, provides a generic dictionary with reactive capabilities. This dictionary triggers events when changes are made to its contents, such as elements being added, removed, updated, or cleared. This is especially useful in scenarios where observation patterns are necessary for synchronization, dynamic updates in UI, or other reactive programming use cases.
