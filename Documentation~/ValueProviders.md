# ValueProviders

Namespace: `DataKeeper.ValueProviders`

ValueProviders decouple "*a component needs a value*" from "*where that value comes from*". A field typed as a provider interface can be fed by a constant, a ScriptableObject asset, a random range, a computed conversion, a [Blackboard](Blackboard.md) entry, or a [ServiceLocator](ServiceLocator.md) lookup — swappable in the inspector without code changes.

## The contract

```csharp
public interface IValueProvider<T>
{
    T GetValue();
}
```

Two implementation families:

1. **Asset providers** (`ScriptableObject`) — e.g. `FloatProvider`, `ColorProvider`, `SpriteProvider`, `GameObjectProvider`, … (`Create > DataKeeper > ...`). Reference them like any asset.
2. **Inline strategy providers** (`[Serializable]` classes) — picked directly on a field via `[SerializeReference]` + the type-selector dropdown. These implement per-type marker interfaces (`IFloatProvider`, `IIntProvider`, `IBoolProvider`, `IStringProvider`, `IVector2Provider`, `IVector3Provider`, `IColorProvider`, `ITransformProvider`, `IRectTransformProvider`, `IImageProvider`) so the dropdown lists exactly the right options.

## Quick start

```csharp
public class Spawner : MonoBehaviour
{
    // Inspector shows a dropdown of every IFloatProvider implementation:
    // constant, random range, blackboard value, converted int, asset reference, ...
    [SerializeReference, SerializeReferenceSelector]
    private IFloatProvider _spawnInterval;

    private float NextDelay() => _spawnInterval.GetValue();
}
```

## Built-in provider groups

| Group | Examples |
| --- | --- |
| Constant | Fixed inline values per type |
| Computed | Conversions and derived values (e.g. int→float), vector component composition |
| Random | Random ranges / random selection |
| Asset | One `ScriptableObject` asset per common Unity type (float, int, bool, string, vectors, `Color`, `ColorBlock`, `Sprite`, `Texture`, `Material`, `AudioClip`, `GameObject`, `Transform`, `RectTransform`, `LayerMask`, colliders, rigidbodies) |
| Integration | Read from a `Blackboard`, a `ScriptableObject` field, or resolve via `ServiceLocator` |

## Writing your own

Implement the marker interface for the type you provide and mark it `[Serializable]`:

```csharp
[Serializable]
public class HealthPercentProvider : IFloatProvider
{
    [SerializeField] private Health _health;
    public float GetValue() => _health.Current / _health.Max;
}
```

It appears automatically in every `IFloatProvider` selector dropdown (discovered via `TypeCache`).
