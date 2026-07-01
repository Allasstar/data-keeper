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

        // --- Observation: any-change listener ---
        [Test]
        public void AddListener_FiresOnAdd_WithTagAndKind()
        {
            GameTag got = default; GameTagChangeType kind = GameTagChangeType.Removed; int calls = 0;
            _container.AddListener((t, c) => { got = t; kind = c; calls++; });
            _container.AddTag(Tag("Enemy/Boss"));
            Assert.AreEqual(1, calls);
            Assert.IsTrue(got.MatchesTagExact(Tag("Enemy/Boss")));
            Assert.AreEqual(GameTagChangeType.Added, kind);
        }

        [Test]
        public void AddListener_FiresOnRemove()
        {
            _container.AddTag(Tag("Enemy/Boss"));
            GameTagChangeType kind = GameTagChangeType.Added; int calls = 0;
            _container.AddListener((t, c) => { kind = c; calls++; });
            _container.RemoveTag(Tag("Enemy/Boss"));
            Assert.AreEqual(1, calls);
            Assert.AreEqual(GameTagChangeType.Removed, kind);
        }

        [Test]
        public void AddListener_DuplicateAdd_DoesNotFire()
        {
            _container.AddTag(Tag("Enemy"));
            int calls = 0;
            _container.AddListener((t, c) => calls++);
            _container.AddTag(Tag("Enemy"));       // already present -> no-op
            _container.AddTag(Tag("Nope"));        // invalid -> no-op
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void RemoveTag_NotPresent_DoesNotFire()
        {
            int calls = 0;
            _container.AddListener((t, c) => calls++);
            _container.RemoveTag(Tag("Enemy"));
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void RemoveListener_StopsNotifications()
        {
            int calls = 0;
            System.Action<GameTag, GameTagChangeType> l = (t, c) => calls++;
            _container.AddListener(l);
            _container.RemoveListener(l);
            _container.AddTag(Tag("Enemy"));
            Assert.AreEqual(0, calls);
        }

        // --- Observation: exact-tag listener ---
        [Test]
        public void AddTagListener_FiresOnlyForExactTag()
        {
            int calls = 0;
            _container.AddTagListener(Tag("Enemy"), c => calls++);
            _container.AddTag(Tag("Enemy/Boss")); // descendant -> must NOT fire an exact listener
            Assert.AreEqual(0, calls);
            _container.AddTag(Tag("Enemy"));       // exact -> fires
            Assert.AreEqual(1, calls);
        }

        // --- Observation: branch listener (hierarchical) ---
        [Test]
        public void AddBranchListener_FiresForDescendant_WithConcreteTag()
        {
            GameTag got = default; GameTagChangeType kind = GameTagChangeType.Removed; int calls = 0;
            _container.AddBranchListener(Tag("Enemy"), (t, c) => { got = t; kind = c; calls++; });
            _container.AddTag(Tag("Enemy/Boss/Elite"));
            Assert.AreEqual(1, calls);
            Assert.IsTrue(got.MatchesTagExact(Tag("Enemy/Boss/Elite")), "callback receives the concrete descendant");
            Assert.AreEqual(GameTagChangeType.Added, kind);
        }

        [Test]
        public void AddBranchListener_DoesNotFireForUnrelatedOrSibling()
        {
            int calls = 0;
            _container.AddBranchListener(Tag("Enemy/Boss"), (t, c) => calls++);
            _container.AddTag(Tag("Enemy/Minion")); // sibling branch
            _container.AddTag(Tag("Player"));       // unrelated root
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void AddBranchListener_RawSemantics_FiresPerDescendant()
        {
            int calls = 0;
            _container.AddBranchListener(Tag("Enemy"), (t, c) => calls++);
            _container.AddTag(Tag("Enemy/Boss/Elite"));
            _container.AddTag(Tag("Enemy/Minion"));
            _container.RemoveTag(Tag("Enemy/Boss/Elite")); // branch still present via Minion, but still fires
            Assert.AreEqual(3, calls);
        }

        // --- Observation: compound mutators ---
        [Test]
        public void AddLeafTag_Emits_RemovedParent_Then_AddedLeaf()
        {
            _container.AddTag(Tag("Enemy"));
            var log = new System.Collections.Generic.List<(string path, GameTagChangeType kind)>();
            _container.AddListener((t, c) => log.Add((t.Path, c)));
            _container.AddLeafTag(Tag("Enemy/Boss"));
            Assert.AreEqual(2, log.Count);
            Assert.AreEqual(("Enemy", GameTagChangeType.Removed), log[0]);
            Assert.AreEqual(("Enemy/Boss", GameTagChangeType.Added), log[1]);
        }

        [Test]
        public void Reset_EmitsRemovedPerTag()
        {
            _container.AddTag(Tag("Enemy"));
            _container.AddTag(Tag("Player"));
            int removed = 0;
            _container.AddListener((t, c) => { if (c == GameTagChangeType.Removed) removed++; });
            _container.Reset();
            Assert.AreEqual(2, removed);
            Assert.IsTrue(_container.IsEmpty());
        }

        // --- Observation: branch presence (deduped transitions) ---
        [Test]
        public void BranchPresence_FiresTrueOnce_OnFirstCoveredAdd()
        {
            var log = new System.Collections.Generic.List<bool>();
            _container.AddBranchPresenceListener(Tag("Enemy"), log.Add);
            _container.AddTag(Tag("Enemy/Boss/Elite"));
            _container.AddTag(Tag("Enemy/Minion")); // branch already present -> no second fire
            Assert.AreEqual(1, log.Count);
            Assert.IsTrue(log[0]);
        }

        [Test]
        public void BranchPresence_FiresFalseOnce_WhenLastCoveredRemoved()
        {
            var log = new System.Collections.Generic.List<bool>();
            _container.AddBranchPresenceListener(Tag("Enemy"), log.Add);
            _container.AddTag(Tag("Enemy/Boss/Elite"));
            _container.AddTag(Tag("Enemy/Minion"));
            log.Clear();
            _container.RemoveTag(Tag("Enemy/Boss/Elite")); // still present via Minion -> no fire
            Assert.AreEqual(0, log.Count);
            _container.RemoveTag(Tag("Enemy/Minion"));      // now absent -> fire false
            Assert.AreEqual(1, log.Count);
            Assert.IsFalse(log[0]);
        }

        [Test]
        public void BranchPresence_SubscribeWhenAlreadyPresent_NoFire_ThenFalseOnRemoval()
        {
            _container.AddTag(Tag("Enemy/Boss")); // present before subscribing
            var log = new System.Collections.Generic.List<bool>();
            _container.AddBranchPresenceListener(Tag("Enemy"), log.Add);
            Assert.AreEqual(0, log.Count, "does not fire on subscribe");
            _container.RemoveTag(Tag("Enemy/Boss"));
            Assert.AreEqual(1, log.Count);
            Assert.IsFalse(log[0], "seeded count made the present->absent transition correct");
        }

        [Test]
        public void BranchPresence_HandlesAddTagFastDuplicates()
        {
            var log = new System.Collections.Generic.List<bool>();
            _container.AddBranchPresenceListener(Tag("Enemy"), log.Add);
            _container.AddTagFast(Tag("Enemy/Boss")); // true
            _container.AddTagFast(Tag("Enemy/Boss")); // duplicate occurrence -> no fire
            _container.RemoveTag(Tag("Enemy/Boss"));  // one occurrence left -> no fire
            _container.RemoveTag(Tag("Enemy/Boss"));  // last one gone -> false
            Assert.AreEqual(2, log.Count);
            Assert.IsTrue(log[0]);
            Assert.IsFalse(log[1]);
        }

        [Test]
        public void BranchPresence_Reset_FiresFalse()
        {
            var log = new System.Collections.Generic.List<bool>();
            _container.AddTag(Tag("Enemy/Boss"));
            _container.AddBranchPresenceListener(Tag("Enemy"), log.Add);
            _container.Reset();
            Assert.AreEqual(1, log.Count);
            Assert.IsFalse(log[0]);
        }

        [Test]
        public void BranchPresence_UnrelatedChange_DoesNotFire()
        {
            var log = new System.Collections.Generic.List<bool>();
            _container.AddBranchPresenceListener(Tag("Enemy"), log.Add);
            _container.AddTag(Tag("Player"));
            Assert.AreEqual(0, log.Count);
        }

        [Test]
        public void RemoveBranchPresenceListener_StopsNotifications()
        {
            var log = new System.Collections.Generic.List<bool>();
            System.Action<bool> l = log.Add;
            _container.AddBranchPresenceListener(Tag("Enemy"), l);
            _container.RemoveBranchPresenceListener(Tag("Enemy"), l);
            _container.AddTag(Tag("Enemy/Boss"));
            Assert.AreEqual(0, log.Count);
        }

        [Test]
        public void RemoveAllListeners_StopsAllNotifications()
        {
            int calls = 0;
            _container.AddListener((t, c) => calls++);
            _container.AddTagListener(Tag("Enemy"), c => calls++);
            _container.AddBranchListener(Tag("Enemy"), (t, c) => calls++);
            _container.AddBranchPresenceListener(Tag("Enemy"), b => calls++);
            _container.RemoveAllListeners();
            _container.AddTag(Tag("Enemy"));
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void BranchPresence_ResubscribeAfterLastListenerRemoved_ReseedsFromCurrentState()
        {
            System.Action<bool> l = _ => { };
            _container.AddBranchPresenceListener(Tag("Enemy"), l);
            _container.RemoveBranchPresenceListener(Tag("Enemy"), l); // observer evicted with its count
            _container.AddTag(Tag("Enemy/Boss"));                      // unobserved change
            var log = new System.Collections.Generic.List<bool>();
            _container.AddBranchPresenceListener(Tag("Enemy"), log.Add);
            _container.RemoveTag(Tag("Enemy/Boss"));
            Assert.AreEqual(1, log.Count);
            Assert.IsFalse(log[0], "count was re-seeded on resubscribe, so present->absent fires");
        }

        // --- Reentrancy: listeners mutating the container from the callback ---
        [Test]
        public void Reset_ListenerRemovingAnotherTag_DoesNotThrow()
        {
            _container.AddTag(Tag("Enemy"));
            _container.AddTag(Tag("Player"));
            _container.AddTag(Tag("Enemy/Minion"));
            _container.AddListener((t, c) => { if (c == GameTagChangeType.Removed) _container.RemoveTag(Tag("Enemy")); });
            Assert.DoesNotThrow(() => _container.Reset());
            Assert.IsTrue(_container.IsEmpty());
        }

        [Test]
        public void AddLeafTag_ListenerRemovingTags_DoesNotThrow()
        {
            _container.AddTag(Tag("Player"));
            _container.AddTag(Tag("Enemy"));
            _container.AddListener((t, c) => { if (c == GameTagChangeType.Removed) _container.RemoveTag(Tag("Player")); });
            Assert.DoesNotThrow(() => _container.AddLeafTag(Tag("Enemy/Boss")));
            Assert.IsTrue(_container.HasTagExact(Tag("Enemy/Boss")));
            Assert.IsFalse(_container.HasTagExact(Tag("Player")));
        }

        // --- RemoveTags with itself ---
        [Test]
        public void RemoveTags_Self_EmptiesContainer()
        {
            _container.AddTag(Tag("Enemy"));
            _container.AddTag(Tag("Player"));
            _container.AddTag(Tag("Enemy/Minion"));
            _container.RemoveTags(_container);
            Assert.IsTrue(_container.IsEmpty());
        }

        // --- Redirect canonicalization on add/remove and listener keys ---
        [Test]
        public void AddTag_RetiredHandle_StoresCanonicalReplacement()
        {
            int minion = _registry.FindByPath("Enemy/Minion");
            int player = _registry.FindByPath("Player");
            _registry.Delete(minion, redirectToId: player);

            _container.AddTag(GameTag.FromId(minion));
            Assert.AreEqual(1, _container.Count);
            Assert.AreEqual(player, _container.First().Id, "the canonical id was stored");
            Assert.IsTrue(_container.RemoveTag(GameTag.FromId(player)), "canonical handle removes it");
            Assert.IsTrue(_container.IsEmpty());
        }

        [Test]
        public void RemoveTag_RetiredHandle_RemovesCanonicalMember()
        {
            int minion = _registry.FindByPath("Enemy/Minion");
            int player = _registry.FindByPath("Player");
            _registry.Delete(minion, redirectToId: player);

            _container.AddTag(GameTag.FromId(player));
            Assert.IsTrue(_container.RemoveTag(GameTag.FromId(minion)), "retired handle resolves to the member");
            Assert.IsTrue(_container.IsEmpty());
        }

        [Test]
        public void AddTagListener_RetiredHandle_FiresWhenReplacementAdded()
        {
            int minion = _registry.FindByPath("Enemy/Minion");
            int player = _registry.FindByPath("Player");
            _registry.Delete(minion, redirectToId: player);

            int calls = 0;
            _container.AddTagListener(GameTag.FromId(minion), c => calls++);
            _container.AddTag(GameTag.FromId(player));
            Assert.AreEqual(1, calls, "listener key was canonicalized at subscribe");
        }

        // --- Expanded ancestor set (large-container HasTag fast path) ---
        [Test]
        public void HasTag_LargeContainer_MatchesLinearSemantics()
        {
            for (int i = 0; i < 10; i++) _registry.GetOrCreate("Bulk/Item" + i);
            for (int i = 0; i < 10; i++) _container.AddTag(Tag("Bulk/Item" + i));

            Assert.IsTrue(_container.HasTag(Tag("Bulk")), "ancestor query covered");
            Assert.IsTrue(_container.HasTag(Tag("Bulk/Item3")));
            Assert.IsFalse(_container.HasTag(Tag("Player")));

            _container.RemoveTag(Tag("Bulk/Item3"));
            Assert.IsFalse(_container.HasTag(Tag("Bulk/Item3")), "set tracked the removal");
            Assert.IsTrue(_container.HasTag(Tag("Bulk")), "branch still covered by remaining items");

            for (int i = 0; i < 10; i++) _container.RemoveTag(Tag("Bulk/Item" + i));
            Assert.IsFalse(_container.HasTag(Tag("Bulk")), "consistent after draining below the threshold");
        }

        [Test]
        public void HasTag_LargeContainer_SurvivesRegistryRebake()
        {
            for (int i = 0; i < 10; i++) _registry.GetOrCreate("Bulk/Item" + i);
            for (int i = 0; i < 10; i++) _container.AddTag(Tag("Bulk/Item" + i));
            Assert.IsTrue(_container.HasTag(Tag("Bulk"))); // builds the expanded set

            _registry.GetOrCreate("Fresh/Thing"); // re-bakes the registry -> set is stale
            _container.AddTag(Tag("Fresh/Thing"));
            Assert.IsTrue(_container.HasTag(Tag("Fresh")), "rebuilt set includes the new tag");
            Assert.IsTrue(_container.HasTag(Tag("Bulk")));
        }
    }
}
