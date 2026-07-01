# Generic Utilities

Namespace: `DataKeeper.Generic`

Small self-contained data types used throughout the package and useful on their own.

## `Optional<T>`

A serializable value + enabled flag, drawn as a single inspector row with a checkbox:

```csharp
[SerializeField] private Optional<float> _overrideSpeed = new Optional<float>(5f, false);

float speed = _overrideSpeed.ValueOr(_defaultSpeed);   // fallback when disabled
float or0   = _overrideSpeed.ValueOrDefault;           // default(T) when disabled
```

Fluent copies: `WithValue(v)`, `Enable()`, `Disable()`. Implicit conversion from `T` creates an enabled optional.

## `UnityLazy<T>`

Lazy component/reference resolution with Unity-aware null handling:

```csharp
private UnityLazy<Rigidbody> _rb;

private void Awake() => _rb = new UnityLazy<Rigidbody>(gameObject);       // GetComponent on first use
// or: new UnityLazy<Renderer>(gameObject, includeChildren: true);
// or: new UnityLazy<IEnemy>(() => FindAnyObjectByType<Enemy>());

void Jump() => _rb.Value.AddForce(Vector3.up);
```

`Reset()` clears the cache; implicit conversions to `T` and `bool` (initialized check) are provided.

## `Register<TValue>`

A string-keyed registry (type name is the default key) with typed lookup — the container behind [ServiceLocator](ServiceLocator.md):

```csharp
var reg = new Register<IService>();
reg.Reg(new SaveService());              // key = "SaveService"
var save = reg.Get<SaveService>();
var byPredicate = reg.Find<SaveService>(s => s.IsReady);
```

## `DirtyTracker<T>`

Like [Reactive](Reactive.md) but with a pluggable equality comparer: setting `Value` fires `OnValueChanged` only when the comparer says the value actually changed. `SilentValue`/`SilentChange` update without notifying; `Invoke()` re-fires manually.

## `DeferredList<T>`

An `IList<T>` whose add/remove operations are queued and applied when `ApplyChanges()` is called — safe to mutate while iterating (e.g. subscribe/unsubscribe during event dispatch).

## `QueueFixedSized<T>`

A `ConcurrentQueue<T>` with a maximum size; enqueueing past capacity silently drops the oldest element. Useful for rolling histories (recent damage events, log tails).

## `JsonData<T>`

A value wrapper with Json.NET helpers (`ToJSON(formatting)` / `FromJSON(json)`), for embedding arbitrary serializable payloads in strings/prefs.

## `Option<TValue, TProvider>`

Inspector-selectable choice between a direct value and a [ValueProvider](ValueProviders.md) ScriptableObject asset (`OptionMode` picks which source is used).
