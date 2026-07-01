using System;
using System.Collections.Generic;
using DataKeeper.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DataKeeper.Tests.Generic
{
    public class DataKeeperJsonTests
    {
        [Serializable]
        private class SaveData
        {
            public string Name;
            public Vector3 Position;
            public Quaternion Rotation;
            public Color Tint;
            public List<Vector2Int> Cells;
        }

        [Test]
        public void Vector3_SerializesFieldsOnly()
        {
            var json = DataKeeperJson.Serialize(new Vector3(2f, 0f, 0f));

            Assert.That(json, Does.Not.Contain("normalized"));
            Assert.That(json, Does.Not.Contain("magnitude"));
            Assert.That(json, Does.Contain("\"x\""));
        }

        [Test]
        public void Vector3_RoundTrips()
        {
            var source = new Vector3(1.5f, -2.25f, 3f);
            var result = DataKeeperJson.Deserialize<Vector3>(DataKeeperJson.Serialize(source));

            Assert.AreEqual(source, result);
        }

        [Test]
        public void Vector2_RoundTrips_WithoutDerivedProperties()
        {
            var source = new Vector2(3.5f, -1.25f);
            var json = DataKeeperJson.Serialize(source);

            Assert.That(json, Does.Not.Contain("normalized"));
            Assert.That(json, Does.Not.Contain("magnitude"));
            Assert.AreEqual(source, DataKeeperJson.Deserialize<Vector2>(json));
        }

        [Test]
        public void Vector4_RoundTrips_WithoutDerivedProperties()
        {
            var source = new Vector4(1f, -2f, 3.75f, 0.5f);
            var json = DataKeeperJson.Serialize(source);

            Assert.That(json, Does.Not.Contain("normalized"));
            Assert.That(json, Does.Not.Contain("magnitude"));
            Assert.AreEqual(source, DataKeeperJson.Deserialize<Vector4>(json));
        }

        [Test]
        public void Quaternion_RoundTrips_WithoutEulerAngles()
        {
            var source = Quaternion.Euler(10f, 20f, 30f);
            var json = DataKeeperJson.Serialize(source);

            Assert.That(json, Does.Not.Contain("eulerAngles"));
            Assert.AreEqual(source, DataKeeperJson.Deserialize<Quaternion>(json));
        }

        [Test]
        public void Color_RoundTrips_WithoutDerivedProperties()
        {
            var source = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            var json = DataKeeperJson.Serialize(source);

            Assert.That(json, Does.Not.Contain("linear"));
            Assert.That(json, Does.Not.Contain("grayscale"));
            Assert.AreEqual(source, DataKeeperJson.Deserialize<Color>(json));
        }

        [Test]
        public void Rect_RoundTrips()
        {
            var source = new Rect(1f, 2f, 3f, 4f);
            Assert.AreEqual(source, DataKeeperJson.Deserialize<Rect>(DataKeeperJson.Serialize(source)));
        }

        [Test]
        public void Bounds_RoundTrips_ViaPrivateFields()
        {
            var source = new Bounds(new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 6f));
            Assert.AreEqual(source, DataKeeperJson.Deserialize<Bounds>(DataKeeperJson.Serialize(source)));
        }

        [Test]
        public void Vector3Int_RoundTrips_ViaPrivateFields()
        {
            var source = new Vector3Int(7, -8, 9);
            Assert.AreEqual(source, DataKeeperJson.Deserialize<Vector3Int>(DataKeeperJson.Serialize(source)));
        }

        [Test]
        public void Matrix4x4_RoundTrips()
        {
            var source = Matrix4x4.TRS(new Vector3(1f, 2f, 3f), Quaternion.Euler(15f, 30f, 45f), new Vector3(2f, 2f, 2f));
            Assert.AreEqual(source, DataKeeperJson.Deserialize<Matrix4x4>(DataKeeperJson.Serialize(source)));
        }

        [Test]
        public void NestedObject_WithUnityTypes_RoundTrips()
        {
            var source = new SaveData
            {
                Name = "slot1",
                Position = new Vector3(10f, 0f, -5.5f),
                Rotation = Quaternion.Euler(0f, 90f, 0f),
                Tint = Color.cyan,
                Cells = new List<Vector2Int> { new Vector2Int(1, 2), new Vector2Int(-3, 4) }
            };

            var result = DataKeeperJson.Deserialize<SaveData>(DataKeeperJson.Serialize(source));

            Assert.AreEqual(source.Name, result.Name);
            Assert.AreEqual(source.Position, result.Position);
            Assert.AreEqual(source.Rotation, result.Rotation);
            Assert.AreEqual(source.Tint, result.Tint);
            CollectionAssert.AreEqual(source.Cells, result.Cells);
        }

        [Test]
        public void UnityEnum_SerializesAsValue_NotFields()
        {
            var json = DataKeeperJson.Serialize(KeyCode.Space);

            Assert.AreEqual(((int)KeyCode.Space).ToString(), json);
            Assert.AreEqual(KeyCode.Space, DataKeeperJson.Deserialize<KeyCode>(json));
        }
    }
}
