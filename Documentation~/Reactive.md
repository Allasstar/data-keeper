# Documentation for `Reactive<T>` Class
The `Reactive<T>` class, located within the `DataKeeper.Generic` namespace, provides a generic reactive data type that can track and trigger events when its value changes. This feature is useful in scenarios where you want to maintain and observe the state of a value reactively.
## Table of Contents
- [Namespace](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md#namespace)
- [Purpose](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md#purpose)
- [Type Parameters](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md#type-parameters)
- [Properties](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md#properties)
- [Constructors](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md#constructors)
- [Methods](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md#methods)
- [Implicit Conversion](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md#implicit-conversion)
- [Interfaces Implemented](https://github.com/Allasstar/data-keeper/blob/main/Documentation~/Reactive.md#interfaces-implemented)

## Namespace
`DataKeeper.Generic`
## Purpose
The `Reactive<T>` class is designed to:
- Maintain and observe changes to a value of type `T`.
- Trigger events through listeners when the value changes, allowing for reactive programming patterns.

## Type Parameters
- **`T` **: The type of the value being tracked reactively.

## Properties
### 1. **Value**
``` c#
public T Value { get; set; }
```
- **Description**: The main property for accessing and modifying the value of the reactive variable. Changing this property fires the `OnValueChanged` event.
- **Get Behavior**: Returns the current stored value.
- **Set Behavior**: Updates the value and invokes all registered listeners with the new value.

### 2. **SilentValue**
``` c#
[JsonIgnore]
public T SilentValue { get; set; }
```
- **Description**: An alternative property for accessing and modifying the value without firing the `OnValueChanged` event.
- **Get Behavior**: Returns the current stored value.
- **Set Behavior**: Updates the value silently (no event triggered).
- **Attributes**: `JsonIgnore`

## Constructors
### 1. **Default Constructor**
``` c#
public Reactive()
```
- **Description**: Creates a `Reactive<T>` instance with the value set to the default of type `T`.

### 2. **Parameterized Constructor**
``` c#
public Reactive(T value)
```
- **Parameters**:
    - `value`: The initial value of type `T`.

- **Description**: Creates a `Reactive<T>` instance with the initial value set to the provided value of type `T`.

## Methods
### 1. **Invoke**
``` c#
public void Invoke()
```
- **Description**: Manually triggers the `OnValueChanged` event with the current value even if the value has not changed.

### 2. **SilentChange**
``` c#
public void SilentChange(T value)
```
- **Parameters**:
    - `value`: The new value to set silently.

- **Description**: Updates the value without firing the `OnValueChanged` event.

### 3. **AddListener**
``` c#
public void AddListener(Action<T> call, bool callOnAddListener = false)
```
- **Parameters**:
    - `call`: The callback to be invoked when the value changes.
    - `callOnAddListener` (optional, default `false`): If `true`, invokes the callback immediately with the current value when registered.

- **Description**: Registers a listener to the `OnValueChanged` event.

### 4. **RemoveListener**
``` c#
public void RemoveListener(Action<T> call)
```
- **Parameters**:
    - `call`: The callback to be removed.

- **Description**: Unregisters a listener from the `OnValueChanged` event.

### 5. **RemoveAllListeners**
``` c#
public void RemoveAllListeners()
```
- **Description**: Removes all listeners from the `OnValueChanged` event.

### 6. **Clear**
``` c#
public void Clear()
```
- **Description**: Resets the value to the default of type `T`.

### 7. **ToString**
``` c#
public override string ToString()
```
- **Description**: Returns the string representation of the current value.
- **Return Value**: The `ToString` result of the current value.

## Implicit Conversion
### Implicit Operator
``` c#
public static implicit operator T(Reactive<T> instance)
```
- **Description**: Allows implicit conversion of a `Reactive<T>` instance to type `T`, returning the current value.
- **Usage**:
``` c#
  Reactive<int> reactiveInt = new Reactive<int>(5);
  int plainInt = reactiveInt; // Implicit conversion
```
## Events
### `OnValueChanged`
``` c#
public Signal<T> OnValueChanged
```
- **Description**: Represents an event-like mechanism that triggers when the value is modified through the `Value` property or `Invoke` method.
- **Type**: `Signal<T>` (used for managing custom event listeners).

## Interfaces Implemented
### IReactive
- **Methods**:
    - `Invoke()`: Triggers the associated events manually.

## Example Usage
### Basic Example
``` c#
Reactive<int> reactiveInt = new Reactive<int>(10);

// Add a listener to the value change.
reactiveInt.AddListener(newValue => Debug.Log($"Value changed to: {newValue}"));

// Change the value (triggers the listener).
reactiveInt.Value = 50;

// Print current value using ToString().
Debug.Log(reactiveInt.ToString());

// Access the value implicitly as an int.
int plainInt = reactiveInt;
```
### Silent Operations Example
``` c#
Reactive<float> reactiveFloat = new Reactive<float>(3.14f);

// Modify the value silently
reactiveFloat.SilentValue = 6.28f;

// No listener will be triggered, even after silent change
reactiveFloat.SilentChange(9.42f);
```
