using UnityEngine;

namespace DataKeeper.Tests.Spatial
{
    // Deliberately not Transform: Transform implements IEnumerable, so NUnit's
    // equality comparer treats childless Transforms as equal empty collections,
    // which makes CollectionAssert on them meaningless.
    internal class SpatialTestMarker : MonoBehaviour
    {
    }
}
