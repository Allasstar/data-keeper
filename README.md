# **DataKeeper**

**DataKeeper** is a comprehensive package/Unity extension that enhances the functionality and convenience of Unity development. It includes a collection of scripts designed to streamline common tasks and improve efficiency. From reactive variables and preferences to data serialization and registration systems, DataKeeper offers a wide range of tools to simplify your workflow.

# **Install Newtonsoft’s Json.NET Package**
On Unity go to Windows->Package Manager, once the Package Manager window opens, go to Add package from git URL, type com.unity.nuget.newtonsoft-json press Add and done.

# --- SCRIPTS ---

## [Reactive<T>](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Generic/Reactive.cs)

The `Reactive<T>` class is a generic class that represents a reactive variable of type `T`. It allows you to track changes to its value and invoke corresponding events when the value is modified.

## [ReactivePref<T>](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Generic/ReactivePref.cs)

The `ReactivePref<T>` class is a generic class that represents a reactive preference variable of type `T` in Unity. It provides a convenient way to store and retrieve preferences using PlayerPrefs while also supporting automatic saving and loading.

## [DataFile<T>](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Generic/DataFile.cs)

The `DataFile<T>` class is a generic class that provides functionality to save and load data of type `T` to a file using different serialization formats such as Binary, XML, or JSON.

## [Optional<T>](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Generic/Optional.cs)

The `Optional<T>` struct represents an optional value of type T. It allows you to store a value of type `T` along with a flag indicating whether the value is enabled or not.

## [Register<TValue>](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Generic/Register.cs)
The `Register<TValue>` class is a generic class that inherits from the Container<TValue> class. It provides functionality to register and store values of type TValue using a string identifier or the type name.

## [RegisterActivator<TValue>](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Generic/RegisterActivator.cs)
The `RegisterActivator<TValue>` class is a generic class that inherits from the Container<TValue> class. It provides additional functionality to instantiate and register values of type TValue using either Activator.CreateInstance<T>() or by instantiating a Component in a GameObject.

## [Registrar](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Components/Registrar.cs)
The `Registrar` class is a MonoBehaviour that serves as a registration system for components. It uses the Register<Component> class to store and manage registered components.

## [SelectUIElementEditor](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Editor/SellectUIElementEditor.cs)
The `SelectUIElementEditor` class is an editor script that allows you to select UI elements in the Unity Editor by pressing the Tab key.

## [SO (ScriptableObject)](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Base/SO.cs)
The `SO` class is an abstract class that extends the ScriptableObject class provided by Unity. It serves as a base class for creating custom ScriptableObject classes.

The SO class is a base class for ScriptableObjects in Unity, providing two important functionalities: the Initialize() method and the Save() method.

The `Initialize()` method is meant to be overridden in derived classes. It allows you to define custom initialization logic for your ScriptableObjects. This method will be automatically called when the game starts or when the ScriptableObject is created in the Unity Editor. You can use the Initialize() method to instantiate objects, register the ScriptableObject with some data, or perform any other setup tasks specific to your ScriptableObject.

The `Save()` method is only available in the Unity Editor and is marked with the [ContextMenu("Save")] attribute. This method is designed to be used during development to manually save changes made to a ScriptableObject. When you right-click on an instance of the ScriptableObject in the Unity Editor, a context menu will appear, and selecting the "Save" option will invoke the Save() method. Inside the Save() method, the ScriptableObject is marked as dirty using EditorUtility.SetDirty(this), indicating that it has been modified. Then, AssetDatabase.SaveAssets() is called to save the changes to disk. This allows you to control when and how the changes to the ScriptableObject are saved without relying solely on automatic serialization.

In summary, the SO script provides a way to initialize ScriptableObjects with custom logic using the Initialize() method, and it allows manual saving of changes made to ScriptableObjects using the Save() method in the Unity Editor during development.

## [BootstrapSO (SO)](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/Extra/BootstrapSO.cs)
The purpose of this SO for managing the initialization and bootstrapping of scenes in a Unity project. 

## [ApplyPreset](https://github.com/Allasstar/DataKeeper/blob/main/Assets/DataKeeper/UI/ApplyPreset.cs)
The `ApplyPreset` class is a MonoBehaviour script designed to apply presets to any MonoBehaviour Components in the Unity editor if they are valid and applicable.

## [ActEngine](https://github.com/Allasstar/DataKeeper/tree/main/Assets/DataKeeper/Extra/ActCore)

The group of scripts revolves around the Act class, which serves as the main class for managing various game-related events and actions. Let's break down each script and its functionality:

Act.cs: This script defines the Act class, which provides a static interface to access and control the ActEngine. It includes properties and events for various application and scene-related events, such as application quit, focus, pause, scene loaded, scene unloaded, and update events. It also provides methods for initializing the ActEngine, starting and stopping coroutines, and executing different types of actions with timing and easing.

ActEngine.cs: The ActEngine is a MonoBehaviour that handles the execution of events and callbacks related to application and scene events. It includes UnityEvents for application quit, focus, pause, scene loaded, scene unloaded, and update events. It registers event handlers for SceneManager's sceneLoaded and sceneUnloaded events. It also provides the implementation for the MonoBehaviour's callbacks such as OnApplicationQuit, OnApplicationFocus, and OnApplicationPause.

ActEnumerator.cs: This script defines a static class called ActEnumerator, which contains coroutine-based utility methods for different types of actions and timing. These methods include waiting for a condition to be met, executing an action repeatedly at a specific interval, delaying a callback, interpolating integer and float values over time, and executing an action with custom easing.

ActEase.cs: This script defines the ActEase class, which provides different easing functions for use with the ActEnumerator. It includes easing functions such as linear, sine, cosine, and various combinations of ease-in, ease-out, and ease-in-out.

Overall, these scripts provide a convenient and centralized way to manage and execute game-related events, actions, and timings using coroutines and easing functions. The Act class serves as the main entry point for accessing and utilizing these functionalities.
