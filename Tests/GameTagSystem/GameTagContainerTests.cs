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
        [Test] public void HasTag_Sibling_False()    { _container.AddTag(Tag("Enemy/Boss")); Assert.IsFalse(_container.HasTag(Tag("Enemy/Minion"))); }
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

        // --- Num / IsEmpty / IsValid ---
        [Test] public void IsEmpty_New_True()         => Assert.IsTrue(_container.IsEmpty());
        [Test] public void IsEmpty_AfterAdd_False()   { _container.AddTag(Tag("Player")); Assert.IsFalse(_container.IsEmpty()); }
        [Test] public void IsValid_New_False()        => Assert.IsFalse(_container.IsValid());
        [Test] public void IsValid_AfterAdd_True()    { _container.AddTag(Tag("Player")); Assert.IsTrue(_container.IsValid()); }
        [Test] public void Num_MatchesCount()         { _container.AddTag(Tag("Player")); _container.AddTag(Tag("Enemy/Boss")); Assert.AreEqual(2, _container.Num()); }

        // --- AddTagFast skips uniqueness ---
        [Test]
        public void AddTagFast_AllowsDuplicates()
        {
            _container.AddTagFast(Tag("Player"));
            _container.AddTagFast(Tag("Player"));
            Assert.AreEqual(2, _container.Count);
        }

        // --- AddLeafTag ---
        [Test]
        public void AddLeafTag_ReplacesParentBranch()
        {
            _container.AddTag(Tag("Enemy"));
            Assert.IsTrue(_container.AddLeafTag(Tag("Enemy/Boss")));
            Assert.AreEqual(1, _container.Count, "redundant parent was dropped");
            Assert.IsTrue(_container.HasTagExact(Tag("Enemy/Boss")));
            Assert.IsFalse(_container.HasTagExact(Tag("Enemy")));
        }

        [Test]
        public void AddLeafTag_RefusedWhenDescendantPresent()
        {
            _container.AddTag(Tag("Enemy/Boss"));
            Assert.IsFalse(_container.AddLeafTag(Tag("Enemy")), "a more-specific tag already covers this");
            Assert.AreEqual(1, _container.Count);
            Assert.IsTrue(_container.HasTagExact(Tag("Enemy/Boss")));
        }

        // --- RemoveTags / Reset ---
        [Test]
        public void RemoveTags_RemovesListedTags()
        {
            _container.AddTag(Tag("Enemy"));
            _container.AddTag(Tag("Player"));
            _container.RemoveTags(Container("Player"));
            Assert.IsFalse(_container.HasTagExact(Tag("Player")));
            Assert.IsTrue(_container.HasTagExact(Tag("Enemy")));
        }

        [Test]
        public void Reset_ClearsAll()
        {
            _container.AddTag(Tag("Enemy"));
            _container.Reset();
            Assert.IsTrue(_container.IsEmpty());
        }

        // --- Indexed access ---
        [Test]
        public void First_Last_GetByIndex()
        {
            _container.AddTag(Tag("Enemy"));
            _container.AddTag(Tag("Player"));
            Assert.IsTrue(_container.First().MatchesTagExact(Tag("Enemy")));
            Assert.IsTrue(_container.Last().MatchesTagExact(Tag("Player")));
            Assert.IsTrue(_container.GetByIndex(1).MatchesTagExact(Tag("Player")));
        }

        [Test] public void GetByIndex_OutOfRange_Invalid() => Assert.IsFalse(_container.GetByIndex(5).IsValid);

        // --- Filter (hierarchical, parents expanded) ---
        [Test]
        public void Filter_KeepsTagsMatchingOther()
        {
            _container.AddTag(Tag("Enemy/Boss/Elite"));
            _container.AddTag(Tag("Player"));
            var filtered = _container.Filter(Container("Enemy"));
            Assert.AreEqual(1, filtered.Count);
            Assert.IsTrue(filtered.HasTagExact(Tag("Enemy/Boss/Elite")));
            Assert.IsFalse(filtered.HasTagExact(Tag("Player")));
        }

        // --- GetGameTagParents (self + all ancestors) ---
        [Test]
        public void GetGameTagParents_ExpandsAncestors()
        {
            _container.AddTag(Tag("Enemy/Boss/Elite"));
            var parents = _container.GetGameTagParents();
            Assert.AreEqual(3, parents.Count);
            Assert.IsTrue(parents.HasTagExact(Tag("Enemy/Boss/Elite")));
            Assert.IsTrue(parents.HasTagExact(Tag("Enemy/Boss")));
            Assert.IsTrue(parents.HasTagExact(Tag("Enemy")));
        }

        [Test]
        public void GetGameTagParents_DedupesSharedAncestors()
        {
            _container.AddTag(Tag("Enemy/Boss/Elite"));
            _container.AddTag(Tag("Enemy/Minion"));
            var parents = _container.GetGameTagParents(); // Elite, Boss, Enemy, Minion (Enemy shared once)
            Assert.AreEqual(4, parents.Count);
            Assert.IsTrue(parents.HasTagExact(Tag("Enemy")));
            Assert.IsTrue(parents.HasTagExact(Tag("Enemy/Minion")));
        }

        [Test] public void GetGameTagParents_Empty_Empty() => Assert.IsTrue(_container.GetGameTagParents().IsEmpty());

        // --- AppendTags ---
        [Test]
        public void AppendTags_AddsAndDedupes()
        {
            _container.AddTag(Tag("Enemy"));
            _container.AppendTags(Container("Enemy", "Player"));
            Assert.AreEqual(2, _container.Count, "duplicate Enemy not re-added");
            Assert.IsTrue(_container.HasTagExact(Tag("Player")));
        }

        [Test] public void AppendTags_Null_NoThrow() => Assert.DoesNotThrow(() => _container.AppendTags(null));

        // --- Empty / null argument semantics (Unreal) ---
        [Test] public void HasAny_EmptyOther_False() { _container.AddTag(Tag("Enemy")); Assert.IsFalse(_container.HasAny(Container())); }
        [Test] public void HasAll_EmptyOther_True()  { _container.AddTag(Tag("Enemy")); Assert.IsTrue(_container.HasAll(Container())); }
        [Test] public void HasAny_Null_False()       => Assert.IsFalse(_container.HasAny(null));
        [Test] public void HasAll_Null_True()        => Assert.IsTrue(_container.HasAll(null));
        [Test] public void HasAnyExact_Null_False()  => Assert.IsFalse(_container.HasAnyExact(null));
        [Test] public void HasAllExact_Null_True()   => Assert.IsTrue(_container.HasAllExact(null));

        // --- Hierarchical vs exact across HasAll ---
        [Test]
        public void HasAll_Hierarchical_vs_Exact()
        {
            _container.AddTag(Tag("Enemy/Boss/Elite"));
            _container.AddTag(Tag("Player"));
            Assert.IsTrue(_container.HasAll(Container("Enemy", "Player")), "ancestor satisfied hierarchically");
            Assert.IsFalse(_container.HasAllExact(Container("Enemy", "Player")), "Enemy not present exactly");
        }

        // --- RemoveTag bool result / RemoveTags null-safe / Clear alias ---
        [Test] public void RemoveTag_NotPresent_ReturnsFalse() => Assert.IsFalse(_container.RemoveTag(Tag("Enemy")));
        [Test] public void RemoveTag_Present_ReturnsTrue()     { _container.AddTag(Tag("Enemy")); Assert.IsTrue(_container.RemoveTag(Tag("Enemy"))); }
        [Test] public void RemoveTags_Null_NoThrow()           => Assert.DoesNotThrow(() => _container.RemoveTags(null));
        [Test] public void Clear_EmptiesContainer()            { _container.AddTag(Tag("Enemy")); _container.Clear(); Assert.IsTrue(_container.IsEmpty()); }

        // --- AddLeafTag edge cases ---
        [Test] public void AddLeafTag_OntoEmpty_Adds()      { Assert.IsTrue(_container.AddLeafTag(Tag("Enemy/Boss"))); Assert.AreEqual(1, _container.Count); }
        [Test] public void AddLeafTag_DuplicateExact_False() { _container.AddLeafTag(Tag("Enemy/Boss")); Assert.IsFalse(_container.AddLeafTag(Tag("Enemy/Boss"))); }
        [Test] public void AddLeafTag_Invalid_False()        => Assert.IsFalse(_container.AddLeafTag(Tag("Nope")));

        // --- Indexed access edge cases ---
        [Test] public void First_Empty_Invalid()         => Assert.IsFalse(_container.First().IsValid);
        [Test] public void Last_Empty_Invalid()          => Assert.IsFalse(_container.Last().IsValid);
        [Test] public void GetByIndex_Negative_Invalid() => Assert.IsFalse(_container.GetByIndex(-1).IsValid);

        // --- Filter edge cases ---
        [Test]
        public void Filter_AncestorContained_DescendantQuery_Empty()
        {
            _container.AddTag(Tag("Enemy")); // ancestor contained, querying a descendant must not match
            Assert.IsTrue(_container.Filter(Container("Enemy/Boss")).IsEmpty());
        }

        [Test] public void Filter_Null_Empty() => Assert.IsTrue(_container.Filter(null).IsEmpty());
    }
}
