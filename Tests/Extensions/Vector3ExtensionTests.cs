using System.Collections.Generic;
using DataKeeper.Extensions;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DataKeeper.Tests.Extensions
{
    public class Vector3ExtensionTests
    {
        private const float Eps = 1e-4f;

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

        private Transform MakeTransform(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject("Vec3ExtTest");
            _gos.Add(go);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            return go.transform;
        }

        // Component-wise assert so we don't test the extension with the extension.
        private static void AssertVec(Vector3 expected, Vector3 actual, float eps = Eps)
        {
            Assert.AreEqual(expected.x, actual.x, eps, "x");
            Assert.AreEqual(expected.y, actual.y, eps, "y");
            Assert.AreEqual(expected.z, actual.z, eps, "z");
        }

        private static void AssertRot(Quaternion expected, Quaternion actual, float epsDeg = 0.01f)
            => Assert.LessOrEqual(Quaternion.Angle(expected, actual), epsDeg);

        // --- WithX / WithY / WithZ ---

        [Test]
        public void With_ReplacesSingleComponent()
        {
            var v = new Vector3(1, 2, 3);
            AssertVec(new Vector3(9, 2, 3), v.WithX(9));
            AssertVec(new Vector3(1, 9, 3), v.WithY(9));
            AssertVec(new Vector3(1, 2, 9), v.WithZ(9));
        }

        // --- Transform position setters ---

        [Test]
        public void SetPos_SetsOnlyTargetAxis()
        {
            var tr = MakeTransform(new Vector3(1, 2, 3), Quaternion.identity, Vector3.one);

            tr.SetPosX(10);
            AssertVec(new Vector3(10, 2, 3), tr.position);

            tr.SetPosY(20);
            AssertVec(new Vector3(10, 20, 3), tr.position);

            tr.SetPosZ(30);
            AssertVec(new Vector3(10, 20, 30), tr.position);
        }

        [Test]
        public void SetLocalPos_SetsOnlyTargetAxis()
        {
            var parent = MakeTransform(new Vector3(5, 0, 0), Quaternion.identity, Vector3.one);
            var tr = MakeTransform(Vector3.zero, Quaternion.identity, Vector3.one);
            tr.SetParent(parent);
            tr.localPosition = new Vector3(1, 2, 3);

            tr.SetLocalPosX(10);
            AssertVec(new Vector3(10, 2, 3), tr.localPosition);

            tr.SetLocalPosY(20);
            AssertVec(new Vector3(10, 20, 3), tr.localPosition);

            tr.SetLocalPosZ(30);
            AssertVec(new Vector3(10, 20, 30), tr.localPosition);
        }

        // --- Area checks ---

        [Test]
        public void IsInsideCube_TrueForCenterAndCorner_FalseOutside()
        {
            var center = new Vector3(5, 5, 5);
            var size = new Vector3(2, 2, 2); // half-extent = 1

            Assert.IsTrue(center.IsInsideCube(center, size));
            Assert.IsTrue(new Vector3(6, 6, 6).IsInsideCube(center, size));   // on the corner (inclusive)
            Assert.IsFalse(new Vector3(6.01f, 5, 5).IsInsideCube(center, size));
        }

        [Test]
        public void IsInsideCube_TransformOverload_UsesPosition()
        {
            var tr = MakeTransform(new Vector3(0.5f, 0, 0), Quaternion.identity, Vector3.one);
            Assert.IsTrue(tr.IsInsideCube(Vector3.zero, Vector3.one * 2f));
            Assert.IsFalse(tr.IsInsideCube(Vector3.zero, Vector3.one * 0.5f));
        }

        [Test]
        public void IsInsideSphere_RespectsRadius()
        {
            var center = new Vector3(1, 1, 1);
            Assert.IsTrue(new Vector3(1, 1, 3).IsInsideSphere(center, 2f));   // dist == 2, inclusive
            Assert.IsFalse(new Vector3(1, 1, 3.01f).IsInsideSphere(center, 2f));
        }

        [Test]
        public void IsInsideSphere_TransformOverload_UsesPosition()
        {
            var tr = MakeTransform(new Vector3(0, 0, 1.5f), Quaternion.identity, Vector3.one);
            Assert.IsTrue(tr.IsInsideSphere(Vector3.zero, 2f));
            Assert.IsFalse(tr.IsInsideSphere(Vector3.zero, 1f));
        }

        [Test]
        public void IsInsideSphereSqrRadius_TakesSquaredRadius()
        {
            var center = Vector3.zero;
            var point = new Vector3(3, 0, 0); // sqrMagnitude = 9
            Assert.IsTrue(point.IsInsideSphereSqrRadius(center, 9f));
            Assert.IsTrue(point.IsInsideSphereSqrRadius(center, 10f));
            Assert.IsFalse(point.IsInsideSphereSqrRadius(center, 8.99f));
        }

        // --- Math helpers ---

        [Test]
        public void Abs_MakesAllComponentsPositive()
            => AssertVec(new Vector3(1, 2, 3), new Vector3(-1, 2, -3).Abs());

        [Test]
        public void Clamp_ClampsPerComponent()
        {
            var v = new Vector3(-5, 5, 0.5f);
            var min = new Vector3(0, 0, 0);
            var max = new Vector3(1, 1, 1);
            AssertVec(new Vector3(0, 1, 0.5f), v.Clamp(min, max));
        }

        [Test]
        public void MaxMinComponent_ReturnExtremes()
        {
            var v = new Vector3(-3, 7, 2);
            Assert.AreEqual(7f, v.MaxComponent(), Eps);
            Assert.AreEqual(-3f, v.MinComponent(), Eps);
        }

        [Test]
        public void Multiply_And_Divide_AreComponentWise()
        {
            var a = new Vector3(2, 4, 6);
            var b = new Vector3(2, 2, 3);
            AssertVec(new Vector3(4, 8, 18), a.Multiply(b));
            AssertVec(new Vector3(1, 2, 2), a.Divide(b));
        }

        // --- Transformations ---

        [Test]
        public void RotateAround_RotatesPointAboutPivot()
        {
            // point one unit +X of pivot, rotate 90deg about Y -> ends up one unit -Z of pivot
            var pivot = new Vector3(0, 0, 0);
            var point = new Vector3(1, 0, 0);
            var result = point.RotateAround(pivot, new Vector3(0, 90, 0));
            AssertVec(new Vector3(0, 0, -1), result);
        }

        [Test]
        public void SetMagnitude_RescalesToTargetLength()
        {
            var v = new Vector3(0, 3, 0);
            var r = v.SetMagnitude(5f);
            AssertVec(new Vector3(0, 5, 0), r);
            Assert.AreEqual(5f, r.magnitude, Eps);
        }

        [Test]
        public void SetMagnitude_ZeroVector_StaysZero()
            => AssertVec(Vector3.zero, Vector3.zero.SetMagnitude(5f));

        [Test]
        public void ClampMagnitude_LongVector_IsCapped()
        {
            var v = new Vector3(10, 0, 0);
            var r = v.ClampMagnitude(3f);
            Assert.AreEqual(3f, r.magnitude, Eps);
            AssertVec(new Vector3(3, 0, 0), r);
        }

        [Test]
        public void ClampMagnitude_ShortVector_PassesThrough()
        {
            var v = new Vector3(1, 0, 0);
            AssertVec(v, v.ClampMagnitude(5f));
        }

        [Test]
        public void ClampMagnitude_ZeroVector_StaysZero()
            => AssertVec(Vector3.zero, Vector3.zero.ClampMagnitude(5f));

        [Test]
        public void RandomPointInSphereAround_StaysWithinRadius()
        {
            var center = new Vector3(10, -5, 3);
            const float radius = 4f;
            for (int i = 0; i < 200; i++)
            {
                var p = center.RandomPointInSphereAround(radius);
                Assert.LessOrEqual((p - center).magnitude, radius + Eps);
            }
        }

        [Test]
        public void LerpUnclamped_ExtrapolatesBeyondOne()
        {
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(2, 0, 0);
            AssertVec(new Vector3(1, 0, 0), a.LerpUnclamped(b, 0.5f));
            AssertVec(new Vector3(4, 0, 0), a.LerpUnclamped(b, 2f));   // unclamped
            AssertVec(new Vector3(-2, 0, 0), a.LerpUnclamped(b, -1f));
        }

        [Test]
        public void ApproximatelyEqual_RespectsTolerance()
        {
            var a = new Vector3(1, 1, 1);
            Assert.IsTrue(a.ApproximatelyEqual(new Vector3(1.005f, 1, 1)));       // within default 0.01
            Assert.IsFalse(a.ApproximatelyEqual(new Vector3(1.5f, 1, 1)));
            Assert.IsTrue(a.ApproximatelyEqual(new Vector3(1.4f, 1, 1), 0.5f));   // custom tolerance
        }

        [Test]
        public void ApproximatelyEqual_TransformOverload_UsesPosition()
        {
            var tr = MakeTransform(new Vector3(2, 0, 0), Quaternion.identity, Vector3.one);
            Assert.IsTrue(tr.ApproximatelyEqual(new Vector3(2.005f, 0, 0)));
            Assert.IsFalse(tr.ApproximatelyEqual(Vector3.zero));
        }

        // --- Vector2 conversions ---

        [Test]
        public void ToVector2_DropsExpectedAxis()
        {
            var v = new Vector3(1, 2, 3);
            Assert.AreEqual(new Vector2(1, 3), v.ToVector2XZ());
            Assert.AreEqual(new Vector2(1, 2), v.ToVector2XY());
            Assert.AreEqual(new Vector2(2, 3), v.ToVector2YZ());
        }

        // --- Local / world position ---

        [Test]
        public void ToWorldPosition_AppliesTranslationRotationScale()
        {
            // translate +10 X, rotate 90 about Y, scale 2
            var tr = MakeTransform(new Vector3(10, 0, 0), Quaternion.Euler(0, 90, 0), Vector3.one * 2f);
            // local (1,0,0) -> scaled (2,0,0) -> rotated to (0,0,-2) -> translated (10,0,-2)
            AssertVec(new Vector3(10, 0, -2), new Vector3(1, 0, 0).ToWorldPosition(tr));
        }

        [Test]
        public void ToWorldPosition_ToLocalPosition_RoundTrip()
        {
            var tr = MakeTransform(new Vector3(3, -2, 7), Quaternion.Euler(15, 40, -25), new Vector3(1.5f, 2f, 0.5f));
            var local = new Vector3(1, 2, 3);
            var world = local.ToWorldPosition(tr);
            AssertVec(local, world.ToLocalPosition(tr));
        }

        // --- Local / world direction ---

        [Test]
        public void ToWorldDirection_AppliesRotationOnly()
        {
            // scale should NOT affect direction, translation should NOT affect direction
            var tr = MakeTransform(new Vector3(100, 100, 100), Quaternion.Euler(0, 90, 0), Vector3.one * 5f);
            var world = new Vector3(1, 0, 0).ToWorldDirection(tr);
            AssertVec(new Vector3(0, 0, -1), world);
            Assert.AreEqual(1f, world.magnitude, Eps); // length preserved
        }

        [Test]
        public void ToWorldDirection_ToLocalDirection_RoundTrip()
        {
            var tr = MakeTransform(new Vector3(3, -2, 7), Quaternion.Euler(15, 40, -25), Vector3.one * 3f);
            var localDir = new Vector3(0.3f, 0.6f, -0.2f);
            var world = localDir.ToWorldDirection(tr);
            AssertVec(localDir, world.ToLocalDirection(tr));
        }

        // --- Local / world rotation ---

        [Test]
        public void ToWorldRotation_ComposesWithTransformRotation()
        {
            var tr = MakeTransform(Vector3.zero, Quaternion.Euler(0, 90, 0), Vector3.one);
            var worldEuler = new Vector3(0, 45, 0).ToWorldRotation(tr);
            AssertRot(Quaternion.Euler(0, 135, 0), Quaternion.Euler(worldEuler));
        }

        [Test]
        public void ToWorldRotation_ToLocalRotation_RoundTrip()
        {
            var tr = MakeTransform(Vector3.zero, Quaternion.Euler(20, -50, 33), Vector3.one);
            var localEuler = new Vector3(10, 25, -40);
            var world = localEuler.ToWorldRotation(tr);
            var back = world.ToLocalRotation(tr);
            AssertRot(Quaternion.Euler(localEuler), Quaternion.Euler(back));
        }

        // --- Local / world scale ---

        [Test]
        public void ToWorldScale_MultipliesByLossyScale()
        {
            var tr = MakeTransform(Vector3.zero, Quaternion.identity, new Vector3(2, 3, 4));
            AssertVec(new Vector3(2, 6, 12), new Vector3(1, 2, 3).ToWorldScale(tr));
        }

        [Test]
        public void ToWorldScale_ToLocalScale_RoundTrip()
        {
            var tr = MakeTransform(Vector3.zero, Quaternion.identity, new Vector3(2, 3, 4));
            var local = new Vector3(1.5f, 2.5f, 0.5f);
            var world = local.ToWorldScale(tr);
            AssertVec(local, world.ToLocalScale(tr));
        }
    }
}
