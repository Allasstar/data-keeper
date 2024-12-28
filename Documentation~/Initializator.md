# Documentation for `Initializator`
The `Initializator` class is a static utility located in the `DataKeeper.Init` namespace. It serves as an initialization helper that loads and initializes all `SO` (Scriptable Object) resources at a specific moment during runtime. This can be particularly useful to set up and prepare resources before a scene is loaded.
## Table of Contents
- [Namespace]()
- [Purpose]()
- [Features]()
- [Methods]()
  - [OnBeforeSceneLoadRuntimeMethod]()

- [Attributes]()
- [Dependencies]()
- [Example Usage]()

## Namespace
`DataKeeper.Init`
## Purpose
The `Initializator` class is designed to:
- Automatically load and initialize all instances of the `SO` class (or its derived types) available as resources in the project.
- Ensure that all `SO` objects are prepared before any scene starts.

## Features
1. **Runtime Initialization**: This utility automatically triggers during the Unity runtime phase: `BeforeSceneLoad`.
2. **Batch Processing**: It processes all `SO` Scriptable Objects located in the `Resources` folder simultaneously.

## Methods
### 1. **OnBeforeSceneLoadRuntimeMethod**
``` csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void OnBeforeSceneLoadRuntimeMethod()
```
#### **Description**:
- This method is invoked automatically by Unity **before any scene is loaded** once the game runtime starts.
- It ensures the following:
  - Loads all `SO` instances from the project's `Resources` folder using the `Resources.LoadAll<SO>("")` method.
  - Iterates through all loaded `SO` instances and invokes their `Initialize()` method.

#### **Key Operations**:
1. **Resource Loading**: It uses `Resources.LoadAll<SO>("")` to find all instances of the `SO` class or derived classes.
2. **Initialization Loop**:
  - Iterates through the loaded instances and calls `so.Initialize()` on each instance.

## Attributes
### `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`
#### Description:
- This attribute attached to `OnBeforeSceneLoadRuntimeMethod` ensures that the method is executed **before the first scene of the game is loaded**.
- It is part of Unity's `RuntimeInitializeOnLoadMethod` mechanism.

## Dependencies
1. **`SO` Class**: The method expects that all objects loaded are of type `SO` (or a class derived from `SO`) and have a method `Initialize()` implemented.
2. Unity's **`Resources` System**:
  - Scriptable Objects must be stored in the `Resources` folder, as the method uses `Resources.LoadAll<SO>("")` for discovery.

## Example Usage
### **Using the `Initializator` Class**
1. **Place SO Scriptable Objects in Resources**:
  - All `SO` objects that should be initialized need to reside in the `Resources` directory in your Unity project.

2. **Automatic Initialization**:
  - The initialization logic is automatically triggered before any scene starts loading, as long as the `Initializator` class is included in your project build.
``` plaintext
Assets/Resources/MyScriptableObjects
 |
 |-- Object1.asset
 |-- Object2.asset
 |-- DerivedSOObject.asset
```
1. **Initialization Example**:
  - If the `SO` class (or its derived classes) contain initialization logic in the `Initialize` method, it will be executed automatically:
``` csharp
     public class MySO : SO
     {
         public override void Initialize()
         {
             Debug.Log("MySO Initialized!");
         }
     }
```
1. **Outcome**:
  - When the game starts, all the Scriptable Objects in the `Resources` folder will automatically call their `Initialize()` method.

## Notes
- This script relies on Unity's **Scriptable Object** pattern.
- Ensure all Scriptable Objects referenced should properly implement the `SO.Initialize()` method to avoid runtime errors.
