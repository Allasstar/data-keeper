# Documentation for `ReactiveDictionary<TKey, TValue>` Class
The `ReactiveDictionary<TKey, TValue>` class, located within the `DataKeeper.Generic` namespace, provides a generic dictionary with reactive capabilities. This dictionary triggers events when changes are made to its contents, such as elements being added, removed, updated, or cleared. This is especially useful in scenarios where observation patterns are necessary for synchronization, dynamic updates in UI, or other reactive programming use cases.
## Table of Contents
1. [Namespace]()
2. [Purpose]()
3. [Type Parameters]()
4. [Events]()
5. [Constructors]()
6. [Properties]()
7. [Methods]()
8. [Example Usage]()

## Namespace
**`DataKeeper.Generic`**
## Purpose
The `ReactiveDictionary<TKey, TValue>` class is a reactive implementation of the `IDictionary<TKey, TValue>` interface. Key features include:
- Automatic invocation of events when dictionary changes occur (additions, deletions, updates, or clearing).
- Maintaining reactive synchronization through event listeners.
- A dynamic and flexible alternative to `Dictionary<TKey, TValue>`, with the same core functionality.

## Type Parameters
- **`TKey`**: The type of the keys in the dictionary.
- **`TValue`**: The type of the values in the dictionary.

## Events
### 1. **`OnDictionaryChanged`**
``` c#
public Signal<TKey, TValue, DictionaryChangedEvent> OnDictionaryChanged
```
- **Description**: This is the primary event, triggered when any operation modifies the dictionary.
- **Event Parameters**:
  - `TKey`: The key affected by the change.
  - `TValue`: The value affected by the change.
  - `DictionaryChangedEvent`: The type of event (Added, Removed, Updated, Cleared).

- **Event Types**:
  - `DictionaryChangedEvent.Added`
  - `DictionaryChangedEvent.Removed`
  - `DictionaryChangedEvent.Updated`
  - `DictionaryChangedEvent.Cleared`

## Constructors
### 1. **Default Constructor**
``` c#
public ReactiveDictionary()
```
- **Description**: Creates an empty `ReactiveDictionary`.

### 2. **Parameterized Constructor (Capacity)**
``` c#
public ReactiveDictionary(int capacity)
```
- **Parameters**:
  - `capacity`: The initial allocation size for the dictionary.

- **Description**: Creates a `ReactiveDictionary` with a predefined capacity.

### 3. **Parameterized Constructor (Existing Collection)**
``` c#
public ReactiveDictionary(IDictionary<TKey, TValue> collection)
```
- **Parameters**:
  - `collection`: The input dictionary to initialize the `ReactiveDictionary`.

- **Description**: Creates a `ReactiveDictionary` initialized with the key-value pairs from another dictionary.

## Properties
### 1. **Indexer**
``` c#
public TValue this[TKey key] { get; set; }
```
- **Description**: Access or modify the value associated with the specified key.
- **Behavior**:
  - **Get**: Retrieves the value associated with `key`.
  - **Set**:
    - If `key` exists, modifies the value and triggers an `Updated` event.
    - If `key` does not exist, adds the key-value pair and triggers an `Added` event.

### 2. **Keys**
``` c#
public ICollection<TKey> Keys { get; }
```
- **Description**: A collection of all keys in the dictionary.

### 3. **Values**
``` c#
public ICollection<TValue> Values { get; }
```
- **Description**: A collection of all values in the dictionary.

### 4. **Count**
``` c#
public int Count { get; }
```
- **Description**: Gets the number of key-value pairs in the dictionary.

### 5. **IsReadOnly**
``` c#
public bool IsReadOnly { get; }
```
- **Description**: Always `false`, indicating the dictionary is not read-only.

## Methods
### 1. **Add**
``` c#
public void Add(TKey key, TValue value)
public void Add(KeyValuePair<TKey, TValue> item)
```
- **Description**: Adds a key-value pair to the dictionary. Triggers the `Added` event.

### 2. **Remove**
``` c#
public bool Remove(TKey key)
public bool Remove(KeyValuePair<TKey, TValue> item)
```
- **Parameters**:
  - `key`: The key to remove.

- **Description**: Removes a key-value pair by key. Triggers the `Removed` event if successful.
- **Returns**: `true` if the removal was successful, `false` otherwise.

### 3. **ContainsKey**
``` c#
public bool ContainsKey(TKey key)
```
- **Description**: Checks if the dictionary contains an element with the specified key.

### 4. **Clear**
``` c#
public void Clear()
```
- **Description**: Removes all elements from the dictionary. Triggers the `Cleared` event.

### 5. **TryGetValue**
``` c#
public bool TryGetValue(TKey key, out TValue value)
```
- **Description**: Attempts to fetch the value associated with the specified key.

### 6. **Contains**
``` c#
public bool Contains(KeyValuePair<TKey, TValue> item)
```
- **Description**: Checks if a key-value pair exists in the dictionary.

### 7. **CopyTo**
``` c#
public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
```
- **Description**: Copies dictionary values to an array starting from the specified index.

### 8. **InvokeUpdateEvent**
``` c#
public void InvokeUpdateEvent(TKey key)
```
- **Description**: Manually triggers the `Updated` event for the specified key.

### 9. **AddListener**
``` c#
public void AddListener(Action<TKey, TValue, DictionaryChangedEvent> call)
```
- **Parameters**:
  - `call`: The callback to register for the `OnDictionaryChanged` event.

- **Description**: Registers a listener to be invoked when the dictionary changes.

### 10. **RemoveListener**
``` c#
public void RemoveListener(Action<TKey, TValue, DictionaryChangedEvent> call)
```
- **Description**: Removes a specific listener from the event.

### 11. **RemoveAllListeners**
``` c#
public void RemoveAllListeners()
```
- **Description**: Clears all listeners from the `OnDictionaryChanged` event.

### 12. **GetEnumerator**
``` c#
public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
IEnumerator IEnumerable.GetEnumerator()
```
- **Description**: Enumerates through the dictionary.

## Example Usage
### Basic Example
``` c#
ReactiveDictionary<string, int> reactiveDict = new ReactiveDictionary<string, int>();

// Add a listener to log changes
reactiveDict.AddListener((key, value, changeType) =>
{
    Debug.Log($"Key: {key}, Value: {value}, ChangeType: {changeType}");
});

// Add a new key-value pair
reactiveDict.Add("Score", 100); // Logs: Key: Score, Value: 100, ChangeType: Added

// Update an existing key
reactiveDict["Score"] = 200; // Logs: Key: Score, Value: 200, ChangeType: Updated

// Remove a key
reactiveDict.Remove("Score"); // Logs: Key: Score, Value: 200, ChangeType: Removed

// Clear the dictionary
reactiveDict.Clear(); // Logs: Key: <null>, Value: <null>, ChangeType: Cleared
```
### Manually Triggering Update Event
``` c#
reactiveDict.Add("Speed", 50);
reactiveDict.InvokeUpdateEvent("Speed"); // Logs: Key: Speed, Value: 50, ChangeType: Updated
```
