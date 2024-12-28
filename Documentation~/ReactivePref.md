# Documentation for `ReactivePref<T>` Class
The `ReactivePref<T>` class, located in the `DataKeeper.Generic` namespace, offers a generic mechanism for storing and managing reactive preferences in Unity. Built with `PlayerPrefs` as the underlying storage, this class enables seamless handling of different data types in a reactive way. It includes features such as event-driven updates on changes, auto-saving, and serialization support for custom data types.
## Table of Contents
- [Namespace]()
- [Purpose]()
- [Dependencies]()
- [Type Parameters]()
- [Properties]()
- [Constructors]()
- [Methods]()
- [Implicit Behavior]()
- [Interfaces Implemented]()
- [Example Usage]()

## Namespace
The class resides in the namespace:
`DataKeeper.Generic`
## Purpose
The purpose of the `ReactivePref<T>` class is to:
- **Persistently store values in Unity's `PlayerPrefs`.**
- **Reactively update listeners whenever the value changes.**
- **Automatically serialize/deserialize complex types into PlayerPrefs.**
- Offer compatibility with Unity-specific and custom data types like `Vector3`, `Color`, and more.

## Dependencies
This class relies on the following dependencies:
- `UnityEngine.PlayerPrefs` for persistent storage.
- `Newtonsoft.Json` for serialization of custom and complex types.
- `DataKeeper.Signals.Signal` for event-based reactive programming.

## Type Parameters
- **`T`**: The type of value being managed by the reactive preference. Supported types include primitive types (`int`, `float`, `string`, `bool`) as well as Unity-specific types (`Vector2`, `Vector3`, `Color`, `Rect`) and custom objects (via JSON serialization).

## Properties
### 1. **Key**
``` c#
public string Key { get; private set; }
```
- **Description**: The key used to store the value in `PlayerPrefs`.
- **Access Modifier**: Read-only.

### 2. **DefaultValue**
``` c#
public T DefaultValue { get; private set; }
```
- **Description**: The default value of the preference, provided during instantiation.

### 3. **Value**
``` c#
public T Value { get; set; }
```
- **Description**: The reactive property that:
    - Triggers events (`OnValueChanged`) when modified.
    - Automatically loads the value from `PlayerPrefs` when accessed for the first time.

- **Behavior**:
    - Getter: Loads from `PlayerPrefs` if not already loaded.
    - Setter: Updates value, triggers listeners, and optionally saves to `PlayerPrefs`.

### 4. **SilentValue**
``` c#
[JsonIgnore]
public T SilentValue { get; set; }
```
- **Description**: Analogous to `Value` but does **not** trigger the `OnValueChanged` event.

## Constructors
### 1. **Parameterized Constructor**
``` c#
public ReactivePref(T defaultValue, string key, bool autoSave = true)
```
- **Parameters**:
    - `defaultValue` (`T`): The default value for uninitialized preferences.
    - `key` (`string`): Unique identifier used in `PlayerPrefs` for storage.
    - `autoSave` (`bool`, default: `true`): Whether changes to `Value` should automatically save to `PlayerPrefs`.

- **Description**: Initializes a new instance of the `ReactivePref<T>` class with a specified default value and key.

## Methods
### Loading & Saving
#### 1. **Load**
``` c#
public void Load()
```
- **Description**: Loads the stored value from `PlayerPrefs` into the reactive property.
- **Behavior**:
    - Handles specific Unity and primitive types.
    - Custom objects are deserialized using JSON.

#### 2. **Save**
``` c#
public void Save()
```
- **Description**: Saves the current value to `PlayerPrefs`.

#### 3. **Reset**
``` c#
public void Reset()
```
- **Description**: Resets the value to the `DefaultValue` without saving it.

### Event Handling
#### 4. **Invoke**
``` c#
public void Invoke()
```
- **Description**: Manually triggers the `OnValueChanged` event with the **current value**.

#### 5. **SilentChange**
``` c#
public void SilentChange(T value)
```
- **Parameters**: `value` (`T`) - The new value.
- **Description**: Updates the value without triggering any reactive listeners or saving.

#### 6. **AddListener**
``` c#
public void AddListener(Action<T> listener, bool callOnAddListener = false)
```
- **Parameters**:
    - `listener` (`Action<T>`): A callback function that executes when the value changes.
    - `callOnAddListener` (`bool`, default: `false`): If `true`, the listener is invoked immediately with the current value.

- **Description**: Adds a listener to the `OnValueChanged` event.

#### 7. **RemoveListener**
``` c#
public void RemoveListener(Action<T> listener)
```
- **Parameters**: `listener` (`Action<T>`) - The previously registered callback.
- **Description**: Removes the specified listener from the `OnValueChanged` event.

#### 8. **RemoveAllListeners**
``` c#
public void RemoveAllListeners()
```
- **Description**: Removes all listeners from the `OnValueChanged` event.

### ToString Override
#### 9. **ToString**
``` c#
public override string ToString()
```
- **Description**: Returns the string representation of the current value.

## Implicit Behavior
### Lazy Loading
- Values are **not loaded** from `PlayerPrefs` until accessed for the first time.
- This behavior ensures efficient memory usage and avoids unnecessary operations.

## Interfaces Implemented
### 1. `IReactivePref`
Defines the following additional methods:
- `Save()`
- `Load()`

### 2. `IReactive`
Allows reactive behavior including invoking events and managing listeners.
## Example Usage
### Basic Example
``` c#
// Initialize a reactive preference for an integer.
ReactivePref<int> scorePref = new ReactivePref<int>(0, "player_score");

// Attach a listener to notify when the score changes.
scorePref.AddListener(newScore => Debug.Log($"Score updated to: {newScore}"));

// Change value (listener will be invoked).
scorePref.Value = 100;

// Save current value to PlayerPrefs.
scorePref.Save();
```
### Complex Data Example
``` c#
// Initialize a reactive preference for a Vector3 type.
ReactivePref<Vector3> positionPref = new ReactivePref<Vector3>(Vector3.zero, "player_position");

// Sync value from PlayerPrefs.
positionPref.Load();

// Modify position and save the value silently.
positionPref.SilentChange(new Vector3(5.0f, 10.0f, 15.0f));
positionPref.Save();
```
