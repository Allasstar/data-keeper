# Documentation for `ReactiveList<T>` Class
The `ReactiveList<T>` class, located in the `DataKeeper.Generic` namespace, is a reactive list implementation that allows tracking changes to its elements and triggering events. This class is particularly useful for scenarios in reactive programming, where you need to observe or respond to changes in the list dynamically.
## Table of Contents
- [Namespace]()
- [Purpose]()
- [Type Parameters]()
- [Properties]()
- [Constructors]()
- [Methods]()
  - [Event-Handling Methods]()
  - [List Operations]()

- [Events]()
- [Interfaces Implemented]()
- [Enumerations]()
- [Example Usage]()

## Namespace
**`DataKeeper.Generic`**
## Purpose
The `ReactiveList<T>` class provides:
- A reactive list container that allows adding, removing, and updating elements while triggering events for each operation.
- Mechanisms for explicit invocation of update events.
- Event-driven programming through the `OnListChanged` signal to observe changes in list state.

## Type Parameters
- **`T`**: The type of elements in the `ReactiveList`.

## Properties
### 1. **Indexer**
``` csharp
public T this[int index] { get; set; }
```
- **Description**: Provides access to elements in the list using a zero-based index.
  - Setting a value triggers a `Removed` event for the old value and an `Added` event for the new value.

### 2. **Count**
``` csharp
public int Count { get; }
```
- **Description**: Gets the number of elements currently in the list.

### 3. **IsReadOnly**
``` csharp
public bool IsReadOnly { get; }
```
- **Description**: Always returns `false`—the list is modifiable.

## Constructors
### 1. **Default Constructor**
``` csharp
public ReactiveList()
```
- **Description**: Initializes an empty `ReactiveList<T>` with a default capacity.

### 2. **Parameterized Constructor (Capacity)**
``` csharp
public ReactiveList(int capacity)
```
- **Parameters**:
  - `capacity`: The initial capacity for the list.

- **Description**: Initializes a `ReactiveList<T>` with the specified initial capacity.

### 3. **Parameterized Constructor (Collection)**
``` csharp
public ReactiveList(IEnumerable<T> collection)
```
- **Parameters**:
  - `collection`: The collection of initial items.

- **Description**: Initializes a `ReactiveList<T>` with elements from the given collection.

## Methods
### Event-Handling Methods
#### 1. **AddListener**
``` csharp
public void AddListener(Action<int, T, ListChangedEvent> call)
```
- **Parameters**:
  - `call`: The event handler to be invoked on a list change.

- **Description**: Registers a listener for the `OnListChanged` signal.

#### 2. **RemoveListener**
``` csharp
public void RemoveListener(Action<int, T, ListChangedEvent> call)
```
- **Parameters**:
  - `call`: The event handler to be removed.

- **Description**: Removes a registered listener from the `OnListChanged` signal.

#### 3. **RemoveAllListeners**
``` csharp
public void RemoveAllListeners()
```
- **Description**: Removes all event listeners from the `OnListChanged` signal.

### List Operations
#### 1. **Add**
``` csharp
public void Add(T item)
public void Add(params T[] items)
```
- **Parameters**:
  - `item`: The element to add.
  - `items`: An array of elements to add.

- **Description**: Adds one or more elements to the list, triggering a `ListChangedEvent.Added` event for each.

#### 2. **Insert**
``` csharp
public void Insert(int index, T item)
```
- **Parameters**:
  - `index`: The index at which to insert the element.
  - `item`: The element to insert.

- **Description**: Inserts an element at the specified index, triggering a `ListChangedEvent.Added` event.

#### 3. **Remove**
``` csharp
public bool Remove(T item)
public int Remove(params T[] items)
```
- **Parameters**:
  - `item`: The element to remove.
  - `items`: An array of elements to remove.

- **Description**: Removes one or more elements, triggering a `ListChangedEvent.Removed` event for each.

#### 4. **RemoveAt**
``` csharp
public void RemoveAt(int index)
```
- **Parameters**:
  - `index`: The position of the element to remove.

- **Description**: Removes the element at the specified index, triggering a `ListChangedEvent.Removed` event.

#### 5. **Clear**
``` csharp
public void Clear()
```
- **Description**: Removes all elements from the list and triggers a `ListChangedEvent.Cleared` event.

#### 6. **CopyTo**
``` csharp
public void CopyTo(T[] array, int arrayIndex)
```
- **Parameters**:
  - `array`: An array to copy elements to.
  - `arrayIndex`: The index in the target array where copying begins.

- **Description**: Copies the elements of the list to the specified array, starting at a particular array index.

#### 7. **Contains**
``` csharp
public bool Contains(T item)
```
- **Parameters**:
  - `item`: The element to search for.

- **Returns**: `true` if the element is in the list; otherwise `false`.

#### 8. **IndexOf**
``` csharp
public int IndexOf(T item)
```
- **Parameters**:
  - `item`: The element to locate.

- **Returns**: The index of the first occurrence of the element, or `-1` if not found.

#### 9. **InvokeUpdateEvent**
``` csharp
public void InvokeUpdateEvent(int index)
public void InvokeUpdateEvent(T value)
```
- **Parameters**:
  - `index`: The index of the element to update.
  - `value`: The element whose update event should be triggered.

- **Description**: Manually triggers a `ListChangedEvent.Updated` event for the specified index or element.

## Events
### OnListChanged
``` csharp
public Signal<int, T, ListChangedEvent> OnListChanged
```
- **Description**: Triggers whenever an element is added, removed, updated, or the list is cleared.
- **Parameters**:
  - `int`: The index affected.
  - `T`: The element affected.
  - `ListChangedEvent`: The type of change.

## Interfaces Implemented
- **`IList<T>`**
- **`IEnumerable<T>`**
- **`IEnumerable`**

## Enumerations
### ListChangedEvent
Defines the types of changes that trigger events.
- **`Added`**: An element was added.
- **`Removed`**: An element was removed.
- **`Updated`**: An element was updated.
- **`Cleared`**: The list was cleared.

## Example Usage
### Basic Example
``` csharp
ReactiveList<int> numbers = new ReactiveList<int>();

numbers.AddListener((index, value, changeType) =>
{
    Debug.Log($"Change detected: Index {index}, Value {value}, Type {changeType}");
});

// Add items
numbers.Add(1, 2, 3); 
numbers[1] = 5;  // Triggers Removed and Added events
numbers.Remove(1); // Removes 1 and triggers event
numbers.Clear(); // Clears the list triggering a Cleared event
```
### Manually Trigger Update
``` csharp
var list = new ReactiveList<string>(new[] { "Alpha", "Beta", "Gamma" });
list.InvokeUpdateEvent(1); // Manually triggers an update event for "Beta"
list.InvokeUpdateEvent("Gamma"); // Triggers an update event for "Gamma"
```
