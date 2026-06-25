using NUnit.Framework;
using UnityEngine;
using DataKeeper.GameTagSystem;

namespace DataKeeper.Tests.GameTagSystem
{
    public class GameTagContainerTests
    {
        private GameTagRegistry _registry;
        private GameTagContainer _container;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<GameTagRegistry>();
            _registry.GetOrCreate("Enemy/Boss/Elite");
            _registry.GetOrCreate("Enemy/Minion");
            _registry.GetOrCreate("Player");
            GameTagRegistry.SetDefault(_registry);
            _container = new GameTagContainer();
        }

        [TearDown]
        public void TearDown()
        {
            GameTagRegistry.SetDefault(null);
            Object.DestroyImmediate(_registry);
        }

        private static GameTag Tag(string path) => GameTag.Find(path);

        private static GameTagContainer Container(params string[] paths)
        {
            var c = new GameTagContainer();
            foreach (var p in paths) c.AddTag(Tag(p));
            return c;
        }

        // --- Add / HasTagExact / Remove ---
        [Test] public void HasTagExact_AfterAdd_True()    { _container.AddTag(Tag("Enemy")); Assert.IsTrue(_container.HasTagExact(Tag("Enemy"))); }
        [Test] public void HasTagExact_NeverAdded_False() => Assert.IsFalse(_container.HasTagExact(Tag("Enemy")));
        [Test] public void AddTag_Duplicate_NotAddedTwice() { _container.AddTag(Tag("Enemy")); _container.AddTag(Tag("Enemy")); Assert.AreEqual(1, _container.Count); }
        [Test] public void AddTag_Invalid_Ignored()       { _container.AddTag(Tag("Nope")); Assert.AreEqual(0, _container.Count); }
        [Test] public void RemoveTag_Existing_Removes()   { var t = Tag("Enemy"); _container.AddTag(t); _container.RemoveTag(t); Assert.IsFalse(_container.HasTagExact(t)); }
        [Test] public void RemoveTag_NotPresent_DoesNotThrow() => Assert.DoesNotThrow(() => _container.RemoveTag(Tag("Enemy")));

        // --- HasTag (hierarchical) ---
        [Test] public void HasTag_ContainedDescendant_MatchesAncestorQuery() { _container.AddTag(Tag("Enemy/Boss/Elite")); Assert.IsTrue(_container.HasTag(Tag("Enemy"))); }
        [Test] public void HasTag_Exact_True()       { _container.AddTag(Tag("Enemy")); Assert.IsTrue(_container.HasTag(Tag("Enemy"))); }
        [Test] public void HasTag_Unrelated_False()  { _container.AddTag(Tag("Enemy/Boss")); Assert.IsFalse(_container.HasTag(Tag("Player"))); }
        [Test] public void HasTag_AncestorContained_DoesNotMatchDescendantQuery() { _container.AddTag(Tag("Enemy")); Assert.IsFalse(_container.HasTag(Tag("Enemy/Boss"))); }

        // --- HasAny / HasAll ---
        [Test] public void HasAny_OneMatches_True()  { _container.AddTag(Tag("Enemy/Boss")); Assert.IsTrue(_container.HasAny(Container("Enemy", "Player"))); }
        [Test] public void HasAny_NoneMatch_False()  { _container.AddTag(Tag("Enemy/Boss")); Assert.IsFalse(_container.HasAny(Container("Player"))); }
        [Test] public void HasAll_AllMatch_True()    { _container.AddTag(Tag("Enemy/Boss/Elite")); _container.AddTag(Tag("Player")); Assert.IsTrue(_container.HasAll(Container("Enemy", "Player"))); }
        [Test] public void HasAll_OneMissing_False() { _container.AddTag(Tag("Enemy/Boss")); Assert.IsFalse(_container.HasAll(Container("Enemy", "Player"))); }

        // --- Exact variants ignore hierarchy ---
        [Test]
        public void HasAnyExact_OnlyExactMatches()
        {
            _container.AddTag(Tag("Enemy/Boss"));
            Assert.IsFalse(_container.HasAnyExact(Container("Enemy")));
            Assert.IsTrue(_container.HasAnyExact(Container("Enemy/Boss")));
        }
    }
}
