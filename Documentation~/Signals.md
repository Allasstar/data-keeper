# Documentation for `namespace DataKeeper.Signals`
The `DataKeeper.Signals` namespace provides a set of utilities and abstractions for implementing a signal-based event-driven system. Signals enable communication between different objects or parts of the system in a decoupled manner. This namespace is designed for scenarios where event management, persistent signals, and runtime callbacks are crucial.
It includes foundational `Signal` classes for event invocation and listener management, as well as classes tailored for Unity integration via `ScriptableObjects`. These components are suitable for building reusable and extendable event systems.
## Table of Contents
- [Purpose]()
- [Classes]()
  - [SignalBase]()
  - [Signal]()
  - [Signal (Generic Variants)]()
  - [SignalChanel]()
  - [SignalChanelBase]()

- [Features]()
- [Example Usage]()

## Purpose
The `DataKeeper.Signals` namespace aims to:
- Enable robust handling of decoupled events using Signals.
- Provide generic support for different types and numbers of parameters on callbacks.
- Facilitate Unity-specific integration with ScriptableObjects to create persistent data-driven signaling.

Signals can be used as stand-alone objects or integrated into Unity's `MonoBehaviour` and `ScriptableObject` systems.
## Classes
### SignalBase
#### Description
An abstract base class for all `Signal` types. It provides the foundational logic for managing listeners, invoking events, and handling concurrency issues when invoking callbacks.
#### Key Features
- Maintains a thread-safe list of listeners (`List<Delegate>`).
- Provides methods for adding, removing, and invoking listeners.
- Handles runtime exceptions during event invocation to ensure stability.

#### Methods
- **`AddListener(Delegate listener)`**: Adds a listener to the signal.
- **`RemoveListener(Delegate listener)`**: Removes a specific listener from the signal.
- **`InvokeListeners(params object[] parameters)`**: Invokes all registered listeners with the provided parameters.
- **`RemoveAllListeners()`**: Clears all listeners.

### Signal
#### Description
A concrete signal implementation for parameterless callbacks, extending `SignalBase`. Ideal for events that do not require arguments.
#### Methods
- **`AddListener(Action listener)`**: Registers a no-argument callback as a listener.
- **`RemoveListener(Action listener)`**: Removes a previously registered listener.
- **`Invoke()`**: Triggers all registered callbacks.
- Inherits `RemoveAllListeners()` from `SignalBase`.

#### Use Case
- Basic event notifications without arguments, such as UI interactions or simple global events.

### Signal (Generic Variants)
The `Signal` class has several generic variants for supporting events with different numbers of parameters:
1. **`Signal<T0>`**:
  - Used for events that take a single argument.
  - Provides methods like `AddListener(Action<T0> listener)` and `Invoke(T0 value)`.

2. **`Signal<T0, T1>`**:
  - Supports events with two arguments.
  - Example: `AddListener(Action<T0, T1> listener)`.

3. **`Signal<T0, T1, T2>`**:
  - Supports events with three arguments.

4. **`Signal<T0, T1, T2, T3>`**:
  - Supports events with four arguments.

5. **`Signal<T0, T1, T2, T3, T4>`**:
  - Supports events with five arguments.

#### Use Case
- Events that require one or more arguments, such as passing data between systems or components.

### SignalChanel
#### Description
A `ScriptableObject` implementation of a parameterless signal (`Signal`), specifically developed for integration with Unity. It allows creating persistent signal channels that can be referenced across the Unity project.
#### Key Features
- Persistent storage in Unity through ScriptableObject.
- Provides Unity-specific debugging features, such as logging listeners.

#### Methods
- **`AddListener(Action call)`**: Adds a no-argument listener.
- **`RemoveListener(Action call)`**: Removes a specific listener.
- **`RemoveAllListeners()`**: Clears all listeners associated with the channel.
- **`Invoke()`**: Triggers callbacks for all listeners.

#### Unity-Specific Features
- **`[CreateAssetMenu(menuName = "DataKeeper/Signals/Signal Chanel")]`**: Enables easy creation of signal channels via Unity's Asset menu.
- **Context Menu Debugging**:
  - **`Log Listeners()`**: Logs all registered listeners for debugging purposes in the Unity Editor.

### SignalChanelBase
#### Description
An abstract base class for creating Unity-specific `ScriptableObject` signal channels that handle events with one parameter (`Signal<T>`).
#### Features
- Fully integrated with Unity's `ScriptableObject` system.
- Inherits basic signal management from `Signal<T>`.

#### Methods
- **`AddListener(Action<T> call)`**: Adds a single-parameter callback as a listener.
- **`RemoveListener(Action<T> call)`**: Removes a specific listener.
- **`RemoveAllListeners()`**: Clears all listeners from the signal channel.
- **`Invoke(T value)`**: Invokes all listeners with a single parameter.

#### Use Case
- Highly reusable signal channels for Unity projects requiring persistence and parameter passing through events.
- For concrete implementations, a derived class should replace the `T` placeholder with the desired type.

## Features
### General
- Thread-safe, concurrency-aware listener management.
- Stable event invocation with error handling for individual listeners.
- Generic support for signals with up to five arguments.

### Unity-Specific
- `ScriptableObject` Integration:
  - Persistent `SignalChanel` and `SignalChanelBase` for longer-term lifecycle management.
  - Editor features like debugging and centralized data usage.

- Easy creation through Unity's asset workflow.

## Example Usage
### Parameterless Signal
``` c#
Signal signal = new Signal();
signal.AddListener(() => Console.WriteLine("Event Triggered!"));
signal.Invoke();  // Output: "Event Triggered!"
signal.RemoveAllListeners();
```
### Signal with Generic Parameters
``` c#
Signal<int> signalWithParam = new Signal<int>();

signalWithParam.AddListener(value => Console.WriteLine($"Received Value: {value}"));
signalWithParam.Invoke(42);  // Output: "Received Value: 42"

signalWithParam.RemoveAllListeners();
```
### Unity-Specific SignalChanel
``` c#
[CreateAssetMenu(menuName = "Custom/MySignalChanel")]
public class MySignalChanel : SignalChanel { }

// In Unity Editor, create an instance via "Assets -> Create -> Custom -> MySignalChanel"
public class ExampleUsage : MonoBehaviour
{
    public MySignalChanel signalChanel;

    private void Start()
    {
        signalChanel.AddListener(OnEventTriggered);
    }

    private void OnEventTriggered()
    {
        Debug.Log("SignalChanel Event Triggered!");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            signalChanel.Invoke();  // Triggers all registered listeners.
        }
    }

    private void OnDestroy()
    {
        signalChanel.RemoveAllListeners();  // Clean up listeners.
    }
}
```
### Unity-Specific SignalChanelBase with Parameters
``` c#
public class StringSignalChanel : SignalChanelBase<string> { }
```
## Conclusion
The `DataKeeper.Signals` namespace provides powerful and flexible tools for event-driven systems both within and outside of Unity. With support for parameterized events, listener management, and Unity-specific persistence, it serves as a comprehensive foundation for building event management solutions.
