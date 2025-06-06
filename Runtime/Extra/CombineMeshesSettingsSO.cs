using UnityEngine;

[CreateAssetMenu(menuName = "DataKeeper/Combine Meshes Settings SO", fileName = "Combine Meshes Settings SO")]
public class CombineMeshesSettingsSO : ScriptableObject
{
    [field: SerializeField] public bool CombineOnStart { get; private set; } = true;
    [field: SerializeField] public bool WaitOneFrame { get; private set; } = true;
    [field: SerializeField] public bool IncludeInactive { get; private set; } = false;
    [field: SerializeField] public bool CombineSubmeshes { get; private set; } = true;
    [field: SerializeField] public bool MergeSubmeshes { get; private set; } = false;
}
