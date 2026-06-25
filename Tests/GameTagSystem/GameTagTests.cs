using NUnit.Framework;
using UnityEngine;
using DataKeeper.GameTagSystem;

namespace DataKeeper.Tests.GameTagSystem
{
    public class GameTagTests
    {
        private GameTagRegistry _registry;
        private int _enemy, _boss, _elite, _minion, _player;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<GameTagRegistry>();
            _elite  = _registry.GetOrCreate("Enemy/Boss/Elite");
            _minion = _registry.GetOrCreate("Enemy/Minion");
            _player = _registry.GetOrCreate("Player");
            _enemy  = _registry.FindByPath("Enemy");
            _boss   = _registry.FindByPath("Enemy/Boss");
            GameTagRegistry.SetDefault(_registry);
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

        // Author a redirect directly (the public Delete API never writes a ToId == NONE redirect,
        // so a "deprecated, no replacement" entry can only be created by editing the serialized list).
        private void InjectRedirect(int fromId, int toId)
        {
            var field = typeof(GameTagRegistry).GetField("_redirects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (System.Collections.Generic.List<GameTagRedirect>)field.GetValue(_registry);
            list.Add(new GameTagRedirect { FromId = fromId, ToId = toId });
            _registry.Bake();
        }

        // --- Find / validity ---
        [Test] public void Find_ExistingPath_IsValid()  => Assert.IsTrue(Tag("Enemy/Boss").IsValid);
        [Test] public void Find_UnknownPath_IsInvalid() => Assert.IsFalse(Tag("Nope/Nope").IsValid);
        [Test] public void Default_Tag_IsInvalid()      => Assert.IsFalse(default(GameTag).IsValid);
        [Test] public void Branch_IsFirstClassValidTag()=> Assert.IsTrue(Tag("Enemy").IsValid);

        // --- Name / Path / Parent ---
        [Test] public void Path_ReturnsFullPath()    => Assert.AreEqual("Enemy/Boss/Elite", Tag("Enemy/Boss/Elite").Path);
        [Test] public void Name_ReturnsLeafSegment() => Assert.AreEqual("Elite", Tag("Enemy/Boss/Elite").Name);
        [Test] public void ToString_ReturnsPath()    => Assert.AreEqual("Enemy/Boss", Tag("Enemy/Boss").ToString());
        [Test] public void Parent_ReturnsParentTag() => Assert.IsTrue(Tag("Enemy/Boss/Elite").Parent.MatchesTagExact(Tag("Enemy/Boss")));

        // --- Equality / hash (by id) ---
        [Test] public void Equals_SamePath_True()       => Assert.IsTrue(Tag("Enemy").Equals(Tag("Enemy")));
        [Test] public void Equals_DifferentPath_False() => Assert.IsFalse(Tag("Enemy").Equals(Tag("Player")));
        [Test] public void Operator_Equals_True()       => Assert.IsTrue(Tag("Enemy") == Tag("Enemy"));
        [Test] public void Hash_EqualsId()              => Assert.AreEqual(_enemy, Tag("Enemy").Hash);

        // --- Inequality / hashing / object Equals ---
        [Test] public void Operator_NotEquals_Different_True()  => Assert.IsTrue(Tag("Enemy") != Tag("Player"));
        [Test] public void Operator_NotEquals_Same_False()      => Assert.IsFalse(Tag("Enemy") != Tag("Enemy"));
        [Test] public void GetHashCode_EqualTags_Match()        => Assert.AreEqual(Tag("Enemy").GetHashCode(), Tag("Enemy").GetHashCode());
        [Test] public void GetHashCode_DifferentTags_Differ()   => Assert.AreNotEqual(Tag("Enemy").GetHashCode(), Tag("Player").GetHashCode());
        [Test] public void Equals_Object_SameTag_True()         => Assert.IsTrue(Tag("Enemy").Equals((object)Tag("Enemy")));
        [Test] public void Equals_Object_Null_False()           => Assert.IsFalse(Tag("Enemy").Equals(null));
        [Test] public void Equals_Object_WrongType_False()      => Assert.IsFalse(Tag("Enemy").Equals("Enemy"));

        // --- FromId / Id round-trip ---
        [Test] public void FromId_RoundTripsId()      => Assert.AreEqual(_enemy, GameTag.FromId(_enemy).Id);
        [Test] public void FromId_UnknownId_Invalid() => Assert.IsFalse(GameTag.FromId(987654321).IsValid);

        // --- Invalid / default tag surface ---
        [Test] public void Default_Name_Null()      => Assert.IsNull(default(GameTag).Name);
        [Test] public void Default_Path_Null()      => Assert.IsNull(default(GameTag).Path);
        [Test] public void Default_ToString_Empty() => Assert.AreEqual(string.Empty, default(GameTag).ToString());
        [Test] public void Default_Hash_None()      => Assert.AreEqual(GameTagRegistry.NONE, default(GameTag).Hash);
        [Test] public void Root_Parent_Invalid()    => Assert.IsFalse(Tag("Enemy").Parent.IsValid);

        // --- Matching with invalid / default operands ---
        [Test] public void MatchesTag_DefaultQuery_False()       => Assert.IsFalse(Tag("Enemy").MatchesTag(default));
        [Test] public void MatchesTag_DefaultReceiver_False()    => Assert.IsFalse(default(GameTag).MatchesTag(Tag("Enemy")));
        [Test] public void MatchesTagExact_DefaultBoth_False()   => Assert.IsFalse(default(GameTag).MatchesTagExact(default));
        [Test] public void MatchesTagDepth_DefaultQuery_Zero()   => Assert.AreEqual(0, Tag("Enemy").MatchesTagDepth(default));
        [Test] public void MatchesAny_EmptyContainer_False()     => Assert.IsFalse(Tag("Enemy").MatchesAny(Container()));
        [Test] public void MatchesAnyExact_EmptyContainer_False() => Assert.IsFalse(Tag("Enemy").MatchesAnyExact(Container()));

        // --- GetOrCreate dedupes ---
        [Test] public void GetOrCreate_SamePath_ReturnsSameId()
            => Assert.AreEqual(_elite, _registry.GetOrCreate("Enemy/Boss/Elite"));

        // --- MatchesTag (hierarchical, Unreal semantics) ---
        [Test] public void MatchesTag_Ancestor_True()      => Assert.IsTrue(Tag("Enemy/Boss/Elite").MatchesTag(Tag("Enemy")));
        [Test] public void MatchesTag_Self_True()          => Assert.IsTrue(Tag("Enemy/Boss").MatchesTag(Tag("Enemy/Boss")));
        [Test] public void MatchesTag_NonAncestor_False()  => Assert.IsFalse(Tag("Enemy/Boss").MatchesTag(Tag("Player")));
        [Test] public void MatchesTag_Descendant_False()   => Assert.IsFalse(Tag("Enemy").MatchesTag(Tag("Enemy/Boss")));
        [Test] public void MatchesTag_Sibling_False()      => Assert.IsFalse(Tag("Enemy/Boss").MatchesTag(Tag("Enemy/Minion")));
        [Test] public void MatchesTag_UnregisteredQuery_False() => Assert.IsFalse(Tag("Enemy/Boss/Elite").MatchesTag(Tag("Ene")));

        // --- MatchesTagExact / IsChildOf ---
        [Test] public void MatchesTagExact_Same_True()      => Assert.IsTrue(Tag("Enemy/Boss").MatchesTagExact(Tag("Enemy/Boss")));
        [Test] public void MatchesTagExact_Ancestor_False() => Assert.IsFalse(Tag("Enemy/Boss").MatchesTagExact(Tag("Enemy")));
        [Test] public void IsChildOf_Descendant_True()      => Assert.IsTrue(Tag("Enemy/Boss").IsChildOf(Tag("Enemy")));
        [Test] public void IsChildOf_DeeperAncestor_True()  => Assert.IsTrue(Tag("Enemy/Boss/Elite").IsChildOf(Tag("Enemy")));
        [Test] public void IsChildOf_Self_False()           => Assert.IsFalse(Tag("Enemy").IsChildOf(Tag("Enemy")));

        // --- MatchesAny / MatchesAnyExact (tag vs container, Unreal semantics) ---
        [Test] public void MatchesAny_AncestorInContainer_True()  => Assert.IsTrue(Tag("Enemy/Boss/Elite").MatchesAny(Container("Enemy", "Player")));
        [Test] public void MatchesAny_NoMatch_False()             => Assert.IsFalse(Tag("Enemy/Boss/Elite").MatchesAny(Container("Player")));
        [Test] public void MatchesAny_Null_False()                => Assert.IsFalse(Tag("Enemy").MatchesAny(null));
        [Test] public void MatchesAnyExact_ExactInContainer_True() => Assert.IsTrue(Tag("Enemy/Boss/Elite").MatchesAnyExact(Container("Enemy/Boss/Elite", "Player")));
        [Test] public void MatchesAnyExact_OnlyAncestor_False()    => Assert.IsFalse(Tag("Enemy/Boss/Elite").MatchesAnyExact(Container("Enemy")));

        // --- MatchesTagDepth (shared ancestor count) ---
        [Test] public void MatchesTagDepth_Self_FullDepth()  => Assert.AreEqual(3, Tag("Enemy/Boss/Elite").MatchesTagDepth(Tag("Enemy/Boss/Elite")));
        [Test] public void MatchesTagDepth_SharedBranch()    => Assert.AreEqual(2, Tag("Enemy/Boss/Elite").MatchesTagDepth(Tag("Enemy/Boss")));
        [Test] public void MatchesTagDepth_Sibling()         => Assert.AreEqual(1, Tag("Enemy/Boss/Elite").MatchesTagDepth(Tag("Enemy/Minion")));
        [Test] public void MatchesTagDepth_Unrelated_Zero()  => Assert.AreEqual(0, Tag("Enemy/Boss/Elite").MatchesTagDepth(Tag("Player")));
        [Test] public void MatchesTagDepth_Symmetric()       => Assert.AreEqual(1, Tag("Enemy").MatchesTagDepth(Tag("Enemy/Boss/Elite")));

        // --- Rename keeps references (the headline feature) ---
        [Test]
        public void Rename_KeepsId_ReferenceResolvesToNewPath()
        {
            var tag = GameTag.FromId(_boss); // a "stored reference" holding the id

            _registry.Rename(_boss, "Champion");

            Assert.AreEqual(GameTagRegistry.NONE, _registry.FindByPath("Enemy/Boss"), "old path is gone");
            Assert.AreEqual(_boss, _registry.FindByPath("Enemy/Champion"), "same id, new path");
            Assert.AreEqual("Enemy/Champion", tag.Path, "the reference now resolves to the renamed path");
            Assert.IsTrue(tag.MatchesTag(Tag("Enemy")), "hierarchy still holds after rename");
        }

        // --- Reparent keeps id and updates descendant paths ---
        [Test]
        public void Reparent_KeepsId_UpdatesDescendantPaths()
        {
            _registry.Reparent(_boss, _player);
            Assert.AreEqual("Player/Boss", GameTag.FromId(_boss).Path);
            Assert.AreEqual("Player/Boss/Elite", _registry.GetPath(_elite));
        }

        [Test]
        public void Reparent_IntoOwnSubtree_IsRefused()
        {
            _registry.Reparent(_enemy, _elite); // _elite is under _enemy -> cycle
            Assert.AreEqual("Enemy", GameTag.FromId(_enemy).Path, "reparent was refused");
        }

        // --- Delete with redirect resolves old references to the replacement ---
        [Test]
        public void Delete_WithRedirect_OldIdResolvesToReplacement()
        {
            _registry.Delete(_minion, redirectToId: _player);
            Assert.IsTrue(GameTag.FromId(_minion).MatchesTagExact(GameTag.FromId(_player)));
        }

        [Test]
        public void Delete_WithRedirect_StructuralEqualityStillDiffers()
        {
            _registry.Delete(_minion, redirectToId: _player);
            var minionRef = GameTag.FromId(_minion);
            Assert.IsFalse(minionRef == GameTag.FromId(_player), "raw-id == is not redirect-aware");
            Assert.IsTrue(minionRef.MatchesTagExact(GameTag.FromId(_player)), "MatchesTagExact resolves the redirect");
        }

        [Test]
        public void Delete_WithRedirect_HierarchyFollowsReplacement()
        {
            _registry.Delete(_minion, redirectToId: _player);
            var minionRef = GameTag.FromId(_minion);
            Assert.IsTrue(minionRef.MatchesTag(Tag("Player")), "now under the replacement's branch");
            Assert.IsFalse(minionRef.MatchesTag(Tag("Enemy")), "old branch no longer applies");
        }

        // --- Redirect-to-NONE (deprecated, no replacement): the MatchesExact NONE guard ---
        [Test]
        public void MatchesTagExact_TwoDeprecatedTags_DoNotMatch()
        {
            InjectRedirect(_minion, GameTagRegistry.NONE);
            InjectRedirect(_boss, GameTagRegistry.NONE);

            var deprecatedA = GameTag.FromId(_minion);
            var deprecatedB = GameTag.FromId(_boss);

            // Both resolve to NONE; without the guard, Resolve(a) == Resolve(b) would be 0 == 0 -> a false match.
            Assert.IsFalse(deprecatedA.MatchesTagExact(deprecatedB), "two NONE-resolved tags must not match");
            Assert.IsFalse(deprecatedA.MatchesTagExact(GameTag.Find("Player")), "deprecated matches nothing exactly");
            Assert.IsFalse(deprecatedA.MatchesTagExact(deprecatedA), "even against itself, NONE-resolved is not a match");
        }

        [Test]
        public void MatchesTag_DeprecatedTag_MatchesNothing()
        {
            InjectRedirect(_minion, GameTagRegistry.NONE);
            var deprecated = GameTag.FromId(_minion);
            Assert.IsFalse(deprecated.MatchesTag(Tag("Enemy")), "resolves to NONE -> no hierarchical match");
            Assert.IsFalse(deprecated.IsChildOf(Tag("Enemy")));
        }
    }
}
