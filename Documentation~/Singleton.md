# Singleton

Namespace: `DataKeeper.SingletonPattern`

## `Singleton<T>`

Lazy singleton for plain C# classes (`where T : new()`):

```csharp
public class GameRules
{
    public int MaxPlayers = 4;
}

var rules = Singleton<GameRules>.Instance; // created on first access
```

## `MonoSingleton<T>`

Scene-based singleton for components. Access `Instance` to find-or-create the instance:

```csharp
public class AudioManager : MonoSingleton<AudioManager>
{
    public void Play(AudioClip clip) { /* ... */ }
}

AudioManager.Instance.Play(clip);
```

Instances created on demand are parented into a shared `MonoSingletonContainer` object so they persist and stay grouped in the hierarchy.

## When to prefer ServiceLocator

Singletons are convenient for genuinely global, single-implementation services (audio, input). For anything you may want to swap, mock, or scope per scene/GameObject, prefer the [ServiceLocator](ServiceLocator.md).
