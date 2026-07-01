# Blackboard

Namespace: `DataKeeper.BlackboardSystem`

A typed key/value store keyed by [GameTags](GameTags.md), for sharing gameplay/AI state between systems without direct references. Lookups are zero-GC: keys are `GameTag` handles (stable int ids) and each value type lives in its own dictionary, so there is no boxing.

## Ways to use it

- **`Blackboard`** — the plain `[Serializable]` class; embed it in any of your own types.
- **`BlackboardBehaviour`** — a `MonoBehaviour` wrapper (`Add Component > DataKeeper > Blackboard > Blackboard`) with very early execution order, so its values are ready before other scripts' `Awake`.
- **`BlackboardAsset`** — a `ScriptableObject` wrapper (`Create > DataKeeper > Blackboard > Blackboard Asset`) for project-wide shared state.

Both wrappers implement `IBlackboardOwner`, so systems can accept "anything that owns a blackboard".

## Quick start

```csharp
var speedTag = GameTag.Find("Stats/Speed");
var targetTag = GameTag.Find("AI/Target");

blackboard.SetFloat(speedTag, 5.5f);
blackboard.SetObject(targetTag, playerTransform);

float speed = blackboard.GetFloat(speedTag);              // default(T) when missing
Transform target = blackboard.GetTransform(targetTag);    // null when missing
```

## Supported value types

Value types each have `Get`/`Set` pairs: `float`, `int`, `bool`, `string`, `Vector2/3/4`, `Vector2Int/3Int`, `Quaternion` (missing → `Quaternion.identity`), `Color`, `Rect`, `Bounds`.

Reference types share one `UnityEngine.Object` store:

- `SetObject(tag, obj)` / `GetObject<T>(tag)` / `HasObject(tag)`
- Convenience getters: `GetGameObject`, `GetTransform`, `GetRectTransform`, `GetImage`, `GetText` (TMP), `GetSprite`.

## Authoring initial values

`Blackboard.Entries` is a `[SerializeReference]` list of `IBlackboardEntry` shown with a type-picker in the inspector — author initial key/value pairs there and call `Initialize()` (the `BlackboardBehaviour`/`BlackboardAsset` wrappers do this for you) to apply them to the runtime stores.

## Notes

- Reads for missing keys return `default` rather than throwing — check `HasObject` for reference types when the distinction matters.
- `Clear()` empties all stores.
- Because keys are `GameTag`s, key renames/merges in the tag registry keep working via tag redirects.
