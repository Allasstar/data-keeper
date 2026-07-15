using System.Collections.Generic;
using System.Linq;
using DataKeeper.Spatial;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Tests.Spatial
{
    public class QuadtreeTests
    {
        private readonly List<GameObject> _gos = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _gos)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _gos.Clear();
        }

        private SpatialTestMarker CreateAt(Vector2 pos)
        {
            var go = new GameObject("QuadtreeTest");
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            _gos.Add(go);
            return go.AddComponent<SpatialTestMarker>();
        }

        private Quadtree<SpatialTestMarker> BuildWith(IEnumerable<Vector2> positions, int maxDepth = 5, int maxPerNode = 10)
        {
            var tree = new Quadtree<SpatialTestMarker>(maxDepth, maxPerNode);
            foreach (var pos in positions)
            {
                tree.AddComponent(CreateAt(pos));
            }
            tree.BuildTree();
            return tree;
        }

        // --- Build ---

        [Test]
        public void BuildTree_Empty_RootIsNull()
        {
            var tree = new Quadtree<SpatialTestMarker>();
            tree.BuildTree();
            Assert.IsNull(tree.Root);
            Assert.IsEmpty(tree.GetComponentsInRadius(Vector2.zero, 100f));
        }

        [Test]
        public void BuildTree_AllInactive_RootIsNull()
        {
            var tree = new Quadtree<SpatialTestMarker>();
            var t = CreateAt(new Vector2(1, 1));
            t.gameObject.SetActive(false);
            tree.AddComponent(t);
            tree.BuildTree();
            Assert.IsNull(tree.Root);
        }

        [Test]
        public void BuildTree_InactiveComponents_ExcludedFromQueries()
        {
            var tree = new Quadtree<SpatialTestMarker>();
            var active = CreateAt(new Vector2(0, 0));
            var inactive = CreateAt(new Vector2(1, 0));
            inactive.gameObject.SetActive(false);
            tree.AddComponent(active);
            tree.AddComponent(inactive);
            tree.BuildTree();

            var result = tree.GetComponentsInRadius(Vector2.zero, 10f);
            CollectionAssert.AreEquivalent(new[] { active }, result);
        }

        // --- Add / Remove / Clear ---

        [Test]
        public void AddComponent_Duplicate_AddedOnce()
        {
            var tree = new Quadtree<SpatialTestMarker>();
            var t = CreateAt(Vector2.zero);
            tree.AddComponent(t);
            tree.AddComponent(t);
            Assert.AreEqual(1, tree.AllComponents.Count);
        }

        [Test]
        public void AddComponent_Null_Ignored()
        {
            var tree = new Quadtree<SpatialTestMarker>();
            tree.AddComponent(null);
            Assert.AreEqual(0, tree.AllComponents.Count);
        }

        [Test]
        public void RemoveComponent_ThenRebuild_NotReturned()
        {
            var a = CreateAt(new Vector2(0, 0));
            var b = CreateAt(new Vector2(1, 0));
            var tree = new Quadtree<SpatialTestMarker>();
            tree.AddComponent(a);
            tree.AddComponent(b);
            tree.BuildTree();

            tree.RemoveComponent(a);
            tree.BuildTree();

            var result = tree.GetComponentsInRadius(Vector2.zero, 10f);
            CollectionAssert.AreEquivalent(new[] { b }, result);
        }

        [Test]
        public void Clear_ResetsTreeAndComponents()
        {
            var tree = BuildWith(new[] { new Vector2(0, 0), new Vector2(1, 1) });
            tree.Clear();
            Assert.IsNull(tree.Root);
            Assert.AreEqual(0, tree.AllComponents.Count);
            Assert.IsEmpty(tree.GetComponentsInRadius(Vector2.zero, 100f));
        }

        // --- Radius queries ---

        [Test]
        public void GetComponentsInRadius_ReturnsOnlyWithinRadius()
        {
            var inside = CreateAt(new Vector2(1, 1));
            var outside = CreateAt(new Vector2(20, 20));
            var tree = new Quadtree<SpatialTestMarker>();
            tree.AddComponent(inside);
            tree.AddComponent(outside);
            tree.BuildTree();

            var result = tree.GetComponentsInRadius(Vector2.zero, 5f);
            CollectionAssert.AreEquivalent(new[] { inside }, result);
        }

        [Test]
        public void GetComponentsInRadius_PointOnRadiusBoundary_Included()
        {
            // Distance from origin to (3,4) is exactly 5
            var boundary = CreateAt(new Vector2(3, 4));
            var tree = new Quadtree<SpatialTestMarker>();
            tree.AddComponent(boundary);
            tree.AddComponent(CreateAt(new Vector2(-10, -10)));
            tree.BuildTree();

            var result = tree.GetComponentsInRadius(Vector2.zero, 5f);
            CollectionAssert.Contains(result, boundary);
        }

        [Test]
        public void GetComponentsInRadius_CornerOfQueryRectButOutsideCircle_Excluded()
        {
            // (4,4) is inside the 5x5 query rect around origin, but at distance ~5.66 > 5
            var corner = CreateAt(new Vector2(4, 4));
            var tree = new Quadtree<SpatialTestMarker>();
            tree.AddComponent(corner);
            tree.AddComponent(CreateAt(Vector2.zero));
            tree.BuildTree();

            var result = tree.GetComponentsInRadius(Vector2.zero, 5f);
            CollectionAssert.DoesNotContain(result, corner);
        }

        [Test]
        public void GetComponentsInRadius_NonAlloc_ClearsAndFillsProvidedList()
        {
            var t = CreateAt(Vector2.zero);
            var tree = new Quadtree<SpatialTestMarker>();
            tree.AddComponent(t);
            tree.BuildTree();

            var result = new List<SpatialTestMarker> { null, null };
            tree.GetComponentsInRadius(Vector2.zero, 5f, result);
            CollectionAssert.AreEquivalent(new[] { t }, result);
        }

        // --- Bounds queries ---

        [Test]
        public void GetComponentsInBounds_ReturnsOnlyInside()
        {
            var inside = CreateAt(new Vector2(2, 2));
            var outside = CreateAt(new Vector2(50, 50));
            var tree = new Quadtree<SpatialTestMarker>();
            tree.AddComponent(inside);
            tree.AddComponent(outside);
            tree.BuildTree();

            var result = tree.GetComponentsInBounds(new Rect(0, 0, 10, 10));
            CollectionAssert.AreEquivalent(new[] { inside }, result);
        }

        // --- Subdivision correctness ---

        [Test]
        public void Subdivision_NoComponentsLostOrDuplicated()
        {
            var rng = new System.Random(12345);
            var positions = new List<Vector2>();
            for (int i = 0; i < 100; i++)
            {
                positions.Add(new Vector2(
                    (float)(rng.NextDouble() * 200 - 100),
                    (float)(rng.NextDouble() * 200 - 100)));
            }

            var tree = BuildWith(positions, maxDepth: 5, maxPerNode: 4);
            var result = tree.GetComponentsInBounds(new Rect(-10000, -10000, 20000, 20000));

            Assert.AreEqual(100, result.Count, "components were lost or duplicated during subdivision");
            Assert.AreEqual(100, result.Distinct().Count(), "query returned duplicates");
        }

        [Test]
        public void Subdivision_PointAtSplitPlane_ReturnedExactlyOnce()
        {
            // Symmetric corners put the root center at the origin, so the
            // origin point lies exactly on both split planes after subdivision.
            var positions = new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(5, 5), new Vector2(-5, 5),
                new Vector2(5, -5), new Vector2(-5, -5),
            };
            var tree = BuildWith(positions, maxDepth: 5, maxPerNode: 2);

            var result = tree.GetComponentsInBounds(new Rect(-100, -100, 200, 200));
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(5, result.Distinct().Count());
        }

        [Test]
        public void Subdivision_ClusteredPoints_RespectsMaxDepth_AllQueryable()
        {
            // 20 points in a tight cluster force subdivision down to maxDepth
            var positions = new List<Vector2>();
            for (int i = 0; i < 20; i++)
            {
                positions.Add(new Vector2(i * 0.001f, 0));
            }

            var tree = BuildWith(positions, maxDepth: 3, maxPerNode: 2);
            var result = tree.GetComponentsInRadius(Vector2.zero, 10f);
            Assert.AreEqual(20, result.Count);
        }

        // --- Destroyed components ---

        [Test]
        public void Query_DestroyedComponent_SkippedWithoutRebuild()
        {
            var a = CreateAt(new Vector2(0, 0));
            var b = CreateAt(new Vector2(1, 0));
            var tree = new Quadtree<SpatialTestMarker>();
            tree.AddComponent(a);
            tree.AddComponent(b);
            tree.BuildTree();

            Object.DestroyImmediate(a.gameObject);

            var result = tree.GetComponentsInRadius(Vector2.zero, 10f);
            CollectionAssert.AreEquivalent(new[] { b }, result);
        }
    }
}
