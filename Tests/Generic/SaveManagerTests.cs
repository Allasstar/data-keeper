using System;
using System.IO;
using DataKeeper.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DataKeeper.Tests.Generic
{
    public class SaveManagerTests
    {
        [Serializable]
        private class Progress
        {
            public int level;
        }

        private const string SlotA = "test_slot_a";
        private const string SlotB = "test_slot_b";
        private const string SlotFileName = "test_progress.json";
        private const string GlobalFileName = "test_global.json";

        [SetUp]
        public void SetUp() => Cleanup();

        [TearDown]
        public void TearDown() => Cleanup();

        private static void Cleanup()
        {
            SaveManager.UnregisterAll();
            SaveManager.ClearMigrations();
            SaveManager.SetSlot(string.Empty);
            SaveManager.DataVersion = 1;
            SaveManager.DeleteSlot(SlotA);
            SaveManager.DeleteSlot(SlotB);
            SaveManager.OnBeforeSave.RemoveAllListeners();
            SaveManager.OnAfterSave.RemoveAllListeners();
            SaveManager.OnBeforeLoad.RemoveAllListeners();
            SaveManager.OnAfterLoad.RemoveAllListeners();
            SaveManager.OnSlotChanged.RemoveAllListeners();

            DeleteRootFile(SlotFileName);
            DeleteRootFile(GlobalFileName);
            DeleteRootFile("save_meta.json");
        }

        private static void DeleteRootFile(string fileName)
        {
            string path = $"{Application.persistentDataPath}/{fileName}";
            if (File.Exists(path)) File.Delete(path);
        }

        [Test]
        public void SlotScopedFile_IsIsolatedPerSlot()
        {
            var file = new DataFile<Progress>(SlotFileName, SerializationType.Json, new Progress(), SaveScope.Slot);
            SaveManager.Register(file);

            SaveManager.SetSlot(SlotA);
            file.Data.level = 1;
            SaveManager.SaveAll();

            SaveManager.SetSlot(SlotB);
            file.Data = new Progress { level = 42 };
            SaveManager.SaveAll();

            SaveManager.SetSlot(SlotA);
            SaveManager.LoadAll();
            Assert.AreEqual(1, file.Data.level);

            SaveManager.SetSlot(SlotB);
            SaveManager.LoadAll();
            Assert.AreEqual(42, file.Data.level);
        }

        [Test]
        public void GlobalScopedFile_IgnoresSlotSwitch()
        {
            var file = new DataFile<Progress>(GlobalFileName, SerializationType.Json, new Progress { level = 7 });
            SaveManager.Register(file);

            SaveManager.SaveAll();

            SaveManager.SetSlot(SlotA);
            file.Data = new Progress { level = 0 };
            SaveManager.LoadAll();

            Assert.AreEqual(7, file.Data.level);
        }

        [Test]
        public void SlotExists_GetSlots_DeleteSlot()
        {
            var file = new DataFile<Progress>(SlotFileName, SerializationType.Json, new Progress(), SaveScope.Slot);
            SaveManager.Register(file);

            SaveManager.SetSlot(SlotA);
            SaveManager.SaveAll();

            Assert.IsTrue(SaveManager.SlotExists(SlotA));
            CollectionAssert.Contains(SaveManager.GetSlots(), SlotA);

            SaveManager.SetSlot(string.Empty);
            SaveManager.DeleteSlot(SlotA);

            Assert.IsFalse(SaveManager.SlotExists(SlotA));
        }

        [Test]
        public void SaveAll_StampsCurrentDataVersion()
        {
            SaveManager.SetSlot(SlotA);
            SaveManager.DataVersion = 3;
            SaveManager.SaveAll();

            Assert.AreEqual(3, SaveManager.GetSavedVersion(SlotA));
        }

        [Test]
        public void Migration_RunsOnce_InOrder_AndStampsVersion()
        {
            SaveManager.SetSlot(SlotA);

            var file = new DataFile<Progress>(SlotFileName, SerializationType.Json, new Progress { level = 5 }, SaveScope.Slot);
            SaveManager.Register(file);
            file.SaveData(); // legacy data on disk, no meta => saved version 0

            int firstRuns = 0, secondRuns = 0, order = 0;
            SaveManager.DataVersion = 2;
            SaveManager.RegisterMigration(2, keyPrefix =>
            {
                secondRuns++;
                Assert.AreEqual(2, ++order);
                Assert.IsTrue(DataKeeperStorage.Files.Exists($"{keyPrefix}/{SlotFileName}"));
            });
            SaveManager.RegisterMigration(1, keyPrefix =>
            {
                firstRuns++;
                Assert.AreEqual(1, ++order);
            });

            SaveManager.LoadAll();

            Assert.AreEqual(1, firstRuns);
            Assert.AreEqual(1, secondRuns);
            Assert.AreEqual(2, SaveManager.GetSavedVersion(SlotA));

            SaveManager.LoadAll(); // version is stamped now — nothing re-runs
            Assert.AreEqual(1, firstRuns);
            Assert.AreEqual(1, secondRuns);
        }

        [Test]
        public void Migration_SkippedOnFreshInstall()
        {
            SaveManager.SetSlot(SlotA);
            SaveManager.DataVersion = 2;

            int runs = 0;
            SaveManager.RegisterMigration(2, _ => runs++);
            SaveManager.LoadAll();

            Assert.AreEqual(0, runs);
        }

        [Test]
        public void Migration_AboveDataVersion_DoesNotRun()
        {
            SaveManager.SetSlot(SlotA);
            SaveManager.DataVersion = 1;
            SaveManager.SaveAll(); // meta exists at version 1

            int runs = 0;
            SaveManager.DataVersion = 2;
            SaveManager.RegisterMigration(2, _ => runs++);
            SaveManager.RegisterMigration(3, _ => runs++); // future version — must not run

            SaveManager.LoadAll();

            Assert.AreEqual(1, runs);
        }

        [Test]
        public void SaveLoad_Events_Fire()
        {
            int beforeSave = 0, afterSave = 0, beforeLoad = 0, afterLoad = 0;
            SaveManager.OnBeforeSave.AddListener(() => beforeSave++);
            SaveManager.OnAfterSave.AddListener(() => afterSave++);
            SaveManager.OnBeforeLoad.AddListener(() => beforeLoad++);
            SaveManager.OnAfterLoad.AddListener(() => afterLoad++);

            SaveManager.SaveAll();
            SaveManager.LoadAll();

            Assert.AreEqual(1, beforeSave);
            Assert.AreEqual(1, afterSave);
            Assert.AreEqual(1, beforeLoad);
            Assert.AreEqual(1, afterLoad);
        }

        [Test]
        public void SetSlot_FiresOnSlotChanged_OnceForSameSlot()
        {
            int changes = 0;
            string lastSlot = null;
            SaveManager.OnSlotChanged.AddListener(slot => { changes++; lastSlot = slot; });

            SaveManager.SetSlot(SlotA);
            SaveManager.SetSlot(SlotA); // no-op

            Assert.AreEqual(1, changes);
            Assert.AreEqual(SlotA, lastSlot);
        }
    }
}
