# Documentation for `ServiceLocator` System
The `ServiceLocator` system, located in the `DataKeeper.ServiceLocatorPattern` namespace, is designed to facilitate dependency injection and service management across global, scene-specific, and GameObject-specific contexts. It allows for services to be registered and resolved dynamically, adhering to the `Service Locator` design pattern. Below is the detailed documentation.
## Table of Contents
- [Namespace]()
- [Purpose]()
- [Core Classes]()
  - [ServiceLocator]()
  - [ServiceLocatorRegister]()

- [Usage]()
- [Service Context Types]()
- [Examples]()

## Namespace
The `ServiceLocator` system resides in the following namespace:
``` c#
namespace DataKeeper.ServiceLocatorPattern
```
## Purpose
The `ServiceLocator` system provides:
- **Centralized service management**:
  - Globally shared services.
  - Scene-specific services (services scoped per Unity scene).
  - GameObject-specific services (services scoped to individual GameObjects).

- **Dynamic registration and cleanup**:
  - Automatic cleanup when a scene is unloaded.
  - Automatic cleanup when a GameObject is destroyed.

- **Ease of access** to registered services via the global `ServiceLocator`.

It is suitable for scenarios where you want to decouple dependencies and manage object lifetimes in a modular way.
## Core Classes
### **1. ServiceLocator**
The static class `ServiceLocator` acts as the central point for accessing service registries for global, scene-specific, and GameObject-specific contexts.
#### Key Members of `ServiceLocator`
##### **Global Register**
Provides access to a globally shared service registry.
``` c#
public static Register<object> ForGlobal()
```
##### **Scene-Specific Register**
Provides access to a service registry specific to a given scene. Accepts `Component`, `GameObject`, or `string` (scene name) as input.
``` c#
public static Register<object> ForSceneOf(Component component)
public static Register<object> ForSceneOf(GameObject go)
public static Register<object> ForSceneOf(string sceneName)
```
- **Scene Cleanup**: Automatically removes associated services from the registry when a scene is unloaded.

##### **GameObject-Specific Register**
Provides access to a service registry specific to a given `GameObject`.
``` c#
public static Register<object> ForGameObjectOf(Component component)
public static Register<object> ForGameObjectOf(GameObject go)
```
- **GameObject Cleanup**: Automatically removes associated services from the registry when its associated `GameObject` is destroyed.

### **2. ServiceLocatorRegister**
`ServiceLocatorRegister` is a `MonoBehaviour` component used to register services via the Unity Inspector.
#### Overview
This class simplifies service registration by allowing developers to configure services and their context types (global, scene, or GameObject) directly in the Unity Editor. It then auto-registers them during the `Awake` lifecycle event.
#### Serialized Fields
``` c#
[SerializeField] private List<ComponentInContext> _register;
```
- A list of `ComponentInContext` objects.
- Configured via the Unity Inspector.

#### Nested Class: `ComponentInContext`
Represents a service and its associated context.
``` c#
[Serializable]
public class ComponentInContext
{
    public ServiceLocatorContextType contextType;
    public Component component;
}
```
- **`contextType`**: Specifies the context for the service (Global, Scene, GameObject).
- **`component`**: The Unity `Component` to be registered as a service.

#### Automatic Registration Logic
During the `Awake` event, services are registered based on their configured `contextType`:
``` c#
switch (c.contextType)
{
    case ServiceLocatorContextType.Global:
        ServiceLocator.ForGlobal().Reg(c.component);
        break;
    case ServiceLocatorContextType.Scene:
        ServiceLocator.ForSceneOf(this).Reg(c.component);
        break;
    case ServiceLocatorContextType.GameObject:
        ServiceLocator.ForGameObjectOf(this).Reg(c.component);
        break;
}
```
## Service Context Types
The `ServiceLocator` supports three levels of service scoping through the `ServiceLocatorContextType`:
### **1. Global (`ServiceLocator.ForGlobal`)**
- Services are shared across the entire application.
- Suitable for singletons or utilities required anywhere in the game.

### **2. Scene (`ServiceLocator.ForSceneOf(sceneName)`)**
- Services are scoped to specific scenes.
- Automatically cleaned when the associated scene is unloaded.
- Useful for scene-specific managers or resources.

### **3. GameObject (`ServiceLocator.ForGameObjectOf(gameObject)`)**
- Services are scoped to a specific `GameObject`.
- Automatically cleaned when the `GameObject` is destroyed.
- Ideal for object-specific behaviors or services tied to a single object.

## Usage
### Registering Services Dynamically
You can register services manually via the `ServiceLocator` API.
#### Example: Register a Global Service
``` c#
ServiceLocator.ForGlobal().Reg(myServiceInstance);
```
#### Example: Register a Scene-Specific Service
``` c#
ServiceLocator.ForSceneOf(myGameObject).Reg(sceneServiceInstance);
```
#### Example: Register a GameObject-Specific Service
``` c#
ServiceLocator.ForGameObjectOf(myGameObject).Reg(goServiceInstance);
```
#### Example: Cleanup on Scene Unload or GameObject Destruction
- Scene services are automatically cleaned up when their scene is unloaded.
- GameObject services are automatically cleaned up when the `GameObject` is destroyed.

## Examples
### **Example 1: Using `ServiceLocatorRegister` in Unity**
Attach the `ServiceLocatorRegister` component to a GameObject and configure its `_register` field from the Unity Inspector.
1. Add the desired `Component` (service) to the `_register` list.
2. Set its `contextType` to one of:
  - **Global**: The service will be globally available.
  - **Scene**: The service will be available within the scene.
  - **GameObject**: The service will be tied to the GameObject.

Unity will automatically register these services during the `Awake` lifecycle event.
### **Example 2: Accessing Registered Services**
Once registered, you can retrieve services from the appropriate register:
``` c#
var myGlobalService = ServiceLocator.ForGlobal().Get<MyServiceType>();
var mySceneService = ServiceLocator.ForSceneOf(currentGameObject).Get<MySceneServiceType>();
var myGameObjectService = ServiceLocator.ForGameObjectOf(currentGameObject).Get<MyGameObjectServiceType>();
```
## Notes
- The `ServiceLocator` system is tightly integrated with Unity's `SceneManager` and `GameObject` lifecycle events for automatic cleanup.
- The `ServiceLocatorRegister` provides a designer-friendly way to configure and initialize services directly in the Unity Inspector.
