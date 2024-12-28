# Documentation for `Pool<T>` Class
The `Pool<T>` class, located within the `DataKeeper.PoolSystem` namespace, is a generic implementation of an object pooling system. It provides the functionality to manage, reuse, and recycle instances of a given `Component`. This class is designed for efficient runtime object management, which is particularly useful in scenarios like Unity game development.
## Table of Contents
- [Namespace]()
- [Purpose]()
- [Type Parameters]()
- [Fields]()
- [Properties]()
- [Constructors]()
- [Methods]()
- [Example Usage]()
- [Remarks]()

## Namespace
`DataKeeper.PoolSystem`
## Purpose
The primary purpose of the `Pool<T>` class is to:
- Reduce performance overhead caused by frequent allocation and destruction of objects.
- Provide mechanisms to prewarm a pool of inactive objects.
- Limit the number of active objects in the scene.

### Key Features:
- Object pooling for Unity `Component`-derived objects.
- Pre-warming option for object pools.
- Option to limit the maximum number of active objects.
- Automatic reuse and recycling of objects.

## Type Parameters
- **`T`**: The type of object to pool. Must inherit from `Component`.

## Fields
### 1. **_poolPrefab**
``` c#
[SerializeField] private T _poolPrefab;
```
- **Description**: The prefab which will be instantiated and pooled.
- **Access**: Serialized for configuration in the Unity Inspector.

### 2. **_prewarm**
``` c#
[SerializeField] private Optional<int> _prewarm = new Optional<int>();
```
- **Description**: Determines the number of objects to prewarm (i.e., initialize and pool before use).
- **Access**: Serialized, optional.

### 3. **_maxActive**
``` c#
[SerializeField] private Optional<int> _maxActive = new Optional<int>();
```
- **Description**: The maximum allowed number of active objects. When exceeded, the oldest active object is released.
- **Access**: Serialized, optional.

### 4. **Internal Fields**
``` c#
private Transform _poolContainer;
private List<T> _poolInactive;
private List<T> _poolActive;
private bool _isInitialized;
```
- **_poolContainer**: A parent `Transform` under which pooled objects are organized.
- **_poolInactive**: A list to store inactive objects.
- **_poolActive**: A list to track currently active objects.
- **_isInitialized**: A flag indicating whether the pool has been initialized.

## Properties
The class includes no public property fields.
## Constructors
There are no explicit constructors defined. The class relies on default initialization by Unity or other classes.
## Methods
### 1. **Initialize**
``` c#
public virtual void Initialize()
```
- **Description**: Initializes the pool by creating the necessary data structures and prewarming the pool if specified.
- **Behavior**:
  - Ensures initialization is done once by checking `_isInitialized`.
  - Creates a `PoolContainer` to structure the pool.
  - Pre-warms the specified number of objects by invoking `Create`.

- **Usage**: Call this method before using the pool.

### 2. **Create**
``` c#
public virtual T Create()
```
- **Description**: Instantiates a new object from the prefab and adds it to the inactive pool.
- **Return Value**: The newly created object.
- **Behavior**:
  - Creates a new instance of `_poolPrefab`.
  - Deactivates the object.
  - Adds the object to `_poolInactive`.

### 3. **Get**
#### Overload 1
``` c#
public virtual T Get()
```
- **Description**: Retrieves an object from the pool, either by reactivating an inactive object or creating a new one.
- **Return Value**: A pooled object ready for use.
- **Behavior**:
  - If `_maxActive` is enabled and its value is exceeded, releases the oldest active object.
  - Prioritizes retrieving objects from `_poolInactive`.

#### Overload 2
``` c#
public T Get(out Action releaseAction)
```
- **Description**: Retrieves a pooled object and provides a callback to release it back to the pool.
- **Parameters**:
  - `releaseAction`: A callback action to release the object.

- **Return Value**: A pooled object ready for use.

### 4. **Release**
``` c#
public virtual void Release(T poolObject)
```
- **Description**: Deactivates an active object and returns it to the inactive pool.
- **Parameters**:
  - `poolObject`: The object to release.

- **Behavior**:
  - Deactivates the object.
  - Sets its parent to `_poolContainer`.
  - Removes the object from `_poolActive`.
  - Adds the object back to `_poolInactive`.

### 5. **ReleaseAll**
``` c#
public virtual void ReleaseAll()
```
- **Description**: Deactivates all active objects and returns them to the inactive pool.
- **Behavior**:
  - Iterates through all active objects.
  - Releases each object individually.

### 6. **GetPoolPrefabID**
``` c#
public int GetPoolPrefabID()
```
- **Description**: Retrieves the unique identifier of the prefab used by the pool.
- **Return Value**: An integer hash code.

### 7. **GetPoolPrefabName**
``` c#
public string GetPoolPrefabName()
```
- **Description**: Retrieves the name of the prefab used by the pool.
- **Return Value**: A string representing the prefab name.

### 8. **IsInitialized**
``` c#
public bool IsInitialized()
```
- **Description**: Checks whether the pool has been initialized.
- **Return Value**: `true` if initialized; otherwise, `false`.

## Example Usage
### Example 1: Initializing and Using a Pool
``` c#
Pool<MyComponent> pool = new Pool<MyComponent>();
pool.Initialize();

// Prewarm objects
pool.Create();
pool.Create();

// Retrieve an object from the pool
MyComponent obj = pool.Get();

// Release the object back to the pool
pool.Release(obj);
```
### Example 2: Limiting Active Objects
``` c#
[SerializeField] private Optional<int> maxActive = new Optional<int>(10);

Pool<MyComponent> pool = new Pool<MyComponent>();
pool.Initialize();
pool.Get();
```
## Remarks
- Ensure the prefab (`_poolPrefab`) is set before calling `Initialize`.
- The `Optional<T>` type allows flexible configuration from the Unity Editor.
- Use `Get` with `releaseAction` to simplify object release logic.
- Always call `Release` to avoid memory leaks or inactive objects persisting in unexpected states.
