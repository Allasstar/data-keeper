# Pity

Namespace: `DataKeeper.Pity`

`PitySystem<T>` is a weighted random drop system with per-entry pity (bad-luck protection) and hard guarantee thresholds. It works with any item type — `int`, `string`, enums, or custom classes — and is `[Serializable]` for inspector authoring.

## How a roll works

1. **Guarantee check** — any entry whose miss count means this roll hits its `guaranteedDropThreshold` is forced to drop (highest effective weight wins ties).
2. **Weighted pick** — otherwise a weighted random selection runs over each entry's *effective weight* (base weight + accumulated pity bonus + `luck × luckInfluence`).
3. **Reset winner** — the dropped entry's miss counter and pity bonus reset.
4. **Accumulate pity** — every other entry gains a miss; once its misses reach `pityActivationThreshold`, each miss also adds `pityWeightIncrement` to its weight bonus.

Every roll always produces exactly one drop, and pity is per-entry — dropping one item never resets the others.

## Quick start

```csharp
var pity = new PitySystem<string>(new List<PityDropEntry<string>>
{
    //                item,        weight, luckInfl, pityAt, pityInc, guaranteedAt
    new PityDropEntry<string>("Common",    10f, 0f,   0,  0f,   0),
    new PityDropEntry<string>("Rare",       3f, 1f,  10,  1f,  40),
    new PityDropEntry<string>("Legendary",  0f, 3f,  20,  3f,  80), // weight 0: only pity/guarantee can drop it
});

string drop = pity.Roll();          // plain roll
string lucky = pity.Roll(0.25f);    // +25% luck, scaled per entry by luckInfluence
```

## Entry fields (`PityDropEntry<T>`)

| Field | Meaning |
| --- | --- |
| `item` | The value returned when this entry drops |
| `baseWeight` | Base selection weight (0 = never dropped by weight alone) |
| `luckInfluence` | Weight bonus per unit of luck (`luck × luckInfluence`, unclamped; negative luck penalizes) |
| `pityActivationThreshold` | Miss number at which pity starts accumulating (0 = from the first miss) |
| `pityWeightIncrement` | Weight added per miss once pity is active |
| `guaranteedDropThreshold` | Hard cap — guaranteed to drop by this roll number (0 = off) |

## System API (`PitySystem<T>`)

| Member | Description |
| --- | --- |
| `Roll(luck = 0)` | Perform one roll; always returns a drop |
| `Drops` | Read-only entry list |
| `AddDrop(entry)` / `RemoveDrop(index)` | Mutate the table at runtime |
| `GetState(index)` | Inspect an entry's misses/weight bonus |
| `ResetAll()` / `ResetDrop(index)` | Clear pity state |
| `SaveState()` / `LoadState(json)` | Persist pity progress as JSON (e.g. into `ReactivePref` or `DataFile`) |

`PityDropEntry<T>.GetEffectiveWeightAt(misses)` computes the weight after N misses without touching state — useful for probability displays and balancing simulations.
