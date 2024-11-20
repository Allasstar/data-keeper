using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "DataKeeper/Selectable Color Palette", fileName = "Selectable Color Palette")]
public class SelectableColorPalette : ScriptableObject
{
    [field: SerializeField]
    public ColorBlock ColorBlock { get; private set; } = ColorBlock.defaultColorBlock;
}
