# GameTags

Namespace: `DataKeeper.GameTagSystem`

A port of Unreal Engine's **GameplayTags** — hierarchical, path-based tags (e.g. `Damage/Elemental/Fire`) used to classify and query gameplay state in a decoupled, data-driven way.

A `GameTag` is a **lightweight, zero-GC struct** that stores only a stable `int` id into a `GameTagRegistry` asset (the source of truth for the tag tree). Because references hold the id and not the path, **renaming or moving a tag never breaks existing references**, and deleting a tag can redirect old references to a replacement.

## Concepts

- **Tree** — tags form a path-separated hierarchy. A leaf carries its whole ancestry, so `Damage/Elemental/Fire` belongs to both the `Damage/Elemental` and `Damage` branches.
- **Registry** — `GameTagRegistry` (a `ScriptableObject`) bakes the authored tag list into runtime caches (paths, depth, parent/child links, per-node ancestor chains for O(1) matching, redirects). The active one is `GameTagRegistry.Default`, loaded from `Resources`.
- **Redirects** — when a tag is deleted/merged, its old id can resolve to a replacement, so stored references keep working.

## Matching (`GameTag`)

All matching mirrors Unreal's semantics and is redirect-aware. Tree used below: `Damage/Elemental/{Fire,Ice}`, `Damage/Physical`, `Status/Burning`.

```csharp
var fire = GameTag.Find("Damage/Elemental/Fire");

// Hierarchical — true for the tag itself or any ancestor branch
fire.MatchesTag(GameTag.Find("Damage/Elemental")); // true  — immediate parent
fire.MatchesTag(GameTag.Find("Damage"));           // true  — deeper ancestor
fire.MatchesTag(GameTag.Find("Damage/Physical"));  // false — cousin branch
GameTag.Find("Damage").MatchesTag(fire);           // false — a parent never matches its child

// Exact — same node only (ignores hierarchy), redirect-aware
fire.MatchesTagExact(GameTag.Find("Damage/Elemental")); // false

// Strictly under (descendant, not self)
fire.IsChildOf(GameTag.Find("Damage"));            // true

// Match closeness — shared ancestor count (0 = unrelated)
fire.MatchesTagDepth(GameTag.Find("Damage/Elemental/Ice")); // 2 (share Damage/Elemental)
```

| Method | Meaning | Unreal equivalent |
| --- | --- | --- |
| `MatchesTag(other)` | this tag, or any ancestor, equals `other` | `MatchesTag` |
| `MatchesTagExact(other)` | same node only (redirect-aware) | `MatchesTagExact` |
| `MatchesAny(container)` / `MatchesAnyExact(container)` | matches any tag in a set | `MatchesAny` / `MatchesAnyExact` |
| `MatchesTagDepth(other)` | shared ancestor count (`int`) | `MatchesTagDepth` |
| `IsChildOf(other)` | strict descendant (extension) | — |
| `Find(path)` / `TryFind(path, out tag)` | resolve an existing tag by path (`TryFind` reports a miss) | `RequestGameplayTag` |
| `GetGameTagParents()` / `GetSingleTagContainer()` | project into a container (self + ancestors / just this) | `GetGameplayTagParents` / `GetSingleTagContainer` |
| `CompareTo(other)` / `GameTag.None` | ordering by raw id (`IComparable`); canonical invalid handle | — |
| `==` / `Equals` / `GetHashCode` | structural identity by raw id (collection key) | `operator==` |

> `==`/`Equals` compare the raw id (a stable, zero-GC dictionary key) and are **not** redirect-aware; use `MatchesTagExact` for "is this the same tag" after a rename/merge.
>
> `Find` returns an invalid tag on an unknown path; prefer `TryFind` when a typo should be caught rather than silently propagated. `Name`/`Path` return an empty string (never `null`) for an unknown/invalid tag.

## Containers (`GameTagContainer`)

A set of tags with the same hierarchical queries — the port of `FGameplayTagContainer`. Hierarchical queries (`HasTag`, `HasAny`, `HasAll`, …) match against the parent branches of contained tags; the `*Exact` variants only match literal members.

```csharp
var tags = new GameTagContainer();
tags.AddTag(GameTag.Find("Damage/Elemental/Fire"));

tags.HasTag(GameTag.Find("Damage"));      // true  — covered via hierarchy
tags.HasTagExact(GameTag.Find("Damage")); // false — "Damage" was never added explicitly
```

Key methods (Unreal parity): `HasTag`/`HasTagExact`, `HasAny`/`HasAnyExact`, `HasAll`/`HasAllExact`, `AddTag`/`AddTagFast`/`AddLeafTag`, `RemoveTag`/`RemoveTags`, `Reset`, `AppendTags`, `Num`/`IsEmpty`/`IsValid`, `GetByIndex`/`First`/`Last`, `Filter`, `GetGameTagParents`.

- **Canonical ids** — mutations resolve rename/merge redirects on entry, so the container stores only canonical ids: a retired handle and its replacement behave as the same tag for add, remove, and listener registration.
- **Performance** — hierarchical matching is O(1) (the registry bakes each node's root-to-self ancestor chain). Queries are zero-GC in steady state; a container holding 8+ tags lazily builds a cached expanded-ancestor set on its first hierarchical query, after which `HasTag` (and `HasAny`/`HasAll` on top of it) is a single hash probe per tag. The set is maintained incrementally and rebuilt automatically if the registry re-bakes.

## Observing a container

A `GameTagContainer` is **observable**: every mutation raises change events, built on the package's zero-GC `Signal`. All event infrastructure is created lazily on the first subscription and released again when the last listener is removed, so an unobserved container keeps its plain-data allocation profile and pays only constant work per mutation. Firing is allocation-free. Changes are reported as `GameTagChangeType` (`Added` / `Removed`).

There are four subscription granularities:

```csharp
var tags = new GameTagContainer();

// 1. Any change — receives the changed tag and the kind.
tags.AddListener((tag, change) => Debug.Log($"{tag.Path} {change}"));

// 2. A specific tag, exact — descendants are ignored.
tags.AddTagListener(GameTag.Find("Stunned"),
    change => animator.SetBool("stunned", change == GameTagChangeType.Added));

// 3. A branch — fires for the tag OR any descendant (raw, one event per matching change).
tags.AddBranchListener(GameTag.Find("Damage"),
    (tag, change) => Debug.Log($"{change}: {tag.Path}")); // e.g. Damage/Elemental/Fire

// 4. A branch's presence — deduped present/absent transitions only.
tags.AddBranchPresenceListener(GameTag.Find("Buff/Shield"), shield.SetActive);
```

| Subscribe with | Fires when | Callback |
| --- | --- | --- |
| `AddListener` | any tag added/removed | `Action<GameTag, GameTagChangeType>` |
| `AddTagListener(tag, …)` | *that* tag added/removed (exact) | `Action<GameTagChangeType>` |
| `AddBranchListener(parent, …)` | the tag **or any descendant** changes (raw) | `Action<GameTag, GameTagChangeType>` |
| `AddBranchPresenceListener(parent, …)` | branch goes present ↔ absent (deduped) | `Action<bool>` |

Each has a matching `Remove…` method, plus `RemoveAllListeners()`. Listener keys are redirect-resolved (subscribing with a retired handle observes its replacement), and removing the last listener releases the observation infrastructure.

- **Raw vs presence** — with both `Damage/Fire` and `Damage/Ice` present, a `Branch` listener on `Damage` fires `Removed(Fire)` even though the branch is still present via `Ice`; a `BranchPresence` listener fires only on the flip to empty. Presence is backed by a per-branch ref-count, so it stays correct even with duplicate entries from `AddTagFast`.
- **Notes** — `AddBranchPresenceListener` does not fire on subscribe, but seeds its count from the current state so the next transition is correct (read `HasTag(parent)` for the initial value). `Reset`/`Clear` emit a `Removed` per tag (and drive present branches to `absent`). Mutators are safe against listeners that mutate the container from the callback.

## Authoring

Edit the tag tree on a `GameTagRegistry` asset (`Create > DataKeeper > GameTagRegistry`); use the picker/drawer in the inspector or the `Tools > Windows > Game Tags Editor` window. The registry can regenerate a static `GameTags` C# class so tags are available as compile-time constants. Every type member is documented with XML doc comments (hover in your IDE for per-case examples).
