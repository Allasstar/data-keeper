# Documentation for `BootstrapSO`
The `BootstrapSO` class, located within the `DataKeeper.Extra` namespace, is a specialized Scriptable Object designed for managing scene loading and object instantiation during the initialization phase of a Unity project. It acts as a bootstrapper, managing the loading of scenes, initialization of objects, and setup of required resources at the start of a game or application.
## Table of Contents
- [Namespace]()
- [Purpose]()
- [Features]()
- [Properties]()
- [Methods]()
  - [Initialize]()
  - [Boot]()
  - [OnSceneLoadComplete]()
  - [Init]()
  - [InstantiatePrefabs]()

- [Attributes]()
- [Usage Notes]()
- [Example Usage]()

## Namespace
`DataKeeper.Extra`
## Purpose
The `BootstrapSO` class is designed to:
- Manage the initialization process during the game's startup phase.
- Handle the following operations:
  1. Loading specified bootstrap scenes.
  2. Unloading bootstrap scenes upon successful loading.
  3. Loading an initial main scene.
  4. Instantiating predefined GameObjects and ensuring they persist across scenes.

## Features
The key features of `BootstrapSO` include:
1. **Bootstrap Scene Handling**:
  - It supports loading multiple scenes additively for initial setup and automatically unloads them after they are processed.

2. **Main Scene Initialization**:
  - Automatically loads the specified "main" (or initial) scene to kick off the core gameplay.

3. **Persistent Game Objects**:
  - Instantiates a specified list of `GameObjects` at runtime and ensures that they persist across scene changes.

4. **Configurable Using the Unity Inspector**:
  - Includes serializable properties that allow easy configuration via the Unity Editor.

## Properties
### 1. **_initialScene** (Serialized Field)
``` csharp
[SerializeField] private SceneReference _initialScene;
```
- **Description**: Specifies the main scene to load after the bootstrap phase is complete.
- **Inspector Configuration**: Set this to the primary scene for your project.

### 2. **_bootstrapSceneList** (Serialized Field)
``` csharp
[SerializeField, Tooltip("Load as Additive and automatically unload.")]
private List<SceneReference> _bootstrapSceneList;
```
- **Description**: A list containing `SceneReference` objects to be loaded additively during the bootstrap phase. These scenes are unloaded automatically once loaded.
- **Inspector Configuration**: Add scenes required for one-time initialization here.

### 3. **_dontDestroyOnLoadList** (Serialized Field)
``` csharp
[SerializeField, Space(20)] 
private List<GameObject> _dontDestroyOnLoadList;
```
- **Description**: A list of `GameObjects` to be instantiated and marked as persistent across all scenes.
- **Inspector Configuration**: Add prefabs or GameObjects that should persist throughout the game (e.g., managers, UI roots).

## Methods
### 1. **Initialize**
``` csharp
public override void Initialize()
```
- **Description**: Acts as the entry point for operations triggered during initialization. It executes the following internal methods in sequence:
  1. [`Boot`]()
  2. [`Init`]()
  3. [`InstantiatePrefabs`]()

### 2. **Boot**
``` csharp
private void Boot()
```
- **Description**: Loads all scenes listed in `_bootstrapSceneList` via `SceneManager.LoadSceneAsync` using additive mode. Upon successful loading, it automatically unloads each scene (`OnSceneLoadComplete`).

### 3. **OnSceneLoadComplete**
``` csharp
private void OnSceneLoadComplete(string sceneName)
```
- **Description**: Triggered when a scene listed in `_bootstrapSceneList` finishes loading. This method handles:
  - Unloading the bootstrap scene to free memory/resources.

### 4. **Init**
``` csharp
private void Init()
```
- **Description**: Loads the `_initialScene` using `SceneManager.LoadScene`. This ensures that the main gameplay scene is loaded after the bootstrap phase.

### 5. **InstantiatePrefabs**
``` csharp
private void InstantiatePrefabs()
```
- **Description**: Instantiates all `GameObjects` listed in `_dontDestroyOnLoadList` and ensures they persist across scenes by marking them with `DontDestroyOnLoad`.
- **Key Operations**:
  - Creates a parent GameObject titled `[BootstrapSO]` for organizing the instantiated objects hierarchy.
  - Ensures persistence by attaching all instantiated objects to the `[BootstrapSO]` parent, and marking it with `DontDestroyOnLoad`.

## Attributes
### 1. `[CreateAssetMenu]`
``` csharp
[CreateAssetMenu(menuName = "DataKeeper/Bootstrap SO", fileName = "Bootstrap SO")]
```
- **Description**: This attribute allows developers to create a `BootstrapSO` asset directly from the Unity Editor via **Assets > Create > DataKeeper > Bootstrap SO**.

## Usage Notes
- All bootstrap-related scenes should be added to the **Build Settings** in Unity to ensure they can be loaded during runtime.
- The `GameObjects` referenced in `_dontDestroyOnLoadList` should be prefabs or objects meant for global use (e.g., singletons, core managers).
- Ensure that `_initialScene` is properly configured, as failure to load it might stop further gameplay progression.

## Example Usage
### 1. **Asset Creation**
- Create a `BootstrapSO` asset via Unity Editor: **Assets > Create > DataKeeper > Bootstrap SO**.

### 2. **Configuration**
- Set up `_initialScene` with your main gameplay scene (e.g., "MainScene").
- Add bootstrap setup scenes to `_bootstrapSceneList` (e.g., loading screens, setup scenes).
- Add GameObjects to `_dontDestroyOnLoadList` that should persist across all scenes, such as:
  - Game Managers
  - Persistent UI canvases

### Example Configuration
``` plaintext
BootstrapSO Settings:
- Initial Scene: "MainScene"
- Bootstrap Scenes: ["Bootstrap1", "Bootstrap2"]
- Persistent GameObjects:
    - "GameManager"
    - "AudioManager"
    - "UIRoot"
```
### 3. **Initialization**
- Link the `BootstrapSO` initialization into your game's startup flow using frameworks like `Initializator` or via a custom init procedure. When triggered:
  1. Bootstrap scenes load additively and unload automatically.
  2. The main gameplay scene initializes.
  3. Persistent GameObjects are instantiated and set to persist.

## Key Benefits
- Centralized management of scene initialization and setup.
- Easily configurable via Unity Inspector.
- Automatically handles common setup operations like loading scenes and managing persistent objects.
