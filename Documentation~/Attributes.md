# Attributes

Namespace: `DataKeeper.Attributes`

Inspector attributes with drawers supplied by the package's editor assembly.

## Field / property attributes

| Attribute | Effect |
| --- | --- |
| `[ReadOnlyInspector]` | Shows the field greyed-out (visible but not editable) |
| `[ShowIf("member")]` | Shows the field only when the named bool field/property/method is true |
| `[SearchableEnum]` | Replaces the enum popup with a searchable dropdown (great for large enums) |
| `[SerializeReferenceSelector]` | Type-picker dropdown for `[SerializeReference]` interface/abstract fields — powers [BeeTween](BeeTween.md), [ValueProviders](ValueProviders.md), and [Blackboard](Blackboard.md) entries |
| `[RequireInterface(typeof(IFoo))]` | Object field that only accepts objects implementing the given interface |
| `[ObjectComponentPicker]` | Object field with a picker for choosing a specific component on the assigned object |
| `[StaticPicker]` | Pick a value from static members |
| `[Preview]` / `[PreviewDictionary]` | Renders an asset preview (sprite/texture/prefab) under the field |
| `[Table]` | Renders a list of structs/classes as an editable table |

## Method attributes

| Attribute | Effect |
| --- | --- |
| `[Button]` / `[Button("Label")]` | Draws a button in the inspector that invokes the method. Optional parameters control order, enabled state (`ButtonEnabledState`: always / play mode / edit mode), and a group header |

```csharp
public class Example : MonoBehaviour
{
    [SerializeField, ReadOnlyInspector] private int _computed;

    [SerializeField] private bool _useOverride;
    [SerializeField, ShowIf(nameof(_useOverride))] private float _override;

    [SerializeField, SearchableEnum] private KeyCode _key;

    [SerializeField, RequireInterface(typeof(IDamageable))] private Object _target;

    [Button("Recalculate", 0, ButtonEnabledState.InEditMode)]
    private void Recalculate() => _computed++;
}
```

## Class attributes

| Attribute | Effect |
| --- | --- |
| `[StaticClassInspector("Category")]` | Registers a static class for live inspection in `Tools > Windows > Static Class Inspector` |
