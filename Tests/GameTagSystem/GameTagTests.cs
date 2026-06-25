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

        // --- GetOrCreate dedupes ---
        [Test] public void GetOrCreate_SamePath_ReturnsSameId()
            => Assert.AreEqual(_elite, _registry.GetOrCreate("Enemy/Boss/Elite"));

        // --- MatchesTag (hierarchical, Unreal semantics) ---
        [Test] public void MatchesTag_Ancestor_True()      => Assert.IsTrue(Tag("Enemy/Boss/Elite").MatchesTag(Tag("Enemy")));
        [Test] public void MatchesTag_Self_True()          => Assert.IsTrue(Tag("Enemy/Boss").MatchesTag(Tag("Enemy/Boss")));
        [Test] public void MatchesTag_NonAncestor_False()  => Assert.IsFalse(Tag("Enemy/Boss").MatchesTag(Tag("Player")));
        [Test] public void MatchesTag_Descendant_False()   => Assert.IsFalse(Tag("Enemy").MatchesTag(Tag("Enemy/Boss")));
        [Test] public void MatchesTag_UnregisteredQuery_False() => Assert.IsFalse(Tag("Enemy/Boss/Elite").MatchesTag(Tag("Ene")));

        // --- MatchesTagExact / IsChildOf ---
        [Test] public void MatchesTagExact_Same_True()      => Assert.IsTrue(Tag("Enemy/Boss").MatchesTagExact(Tag("Enemy/Boss")));
        [Test] public void MatchesTagExact_Ancestor_False() => Assert.IsFalse(Tag("Enemy/Boss").MatchesTagExact(Tag("Enemy")));
        [Test] public void IsChildOf_Descendant_True()      => Assert.IsTrue(Tag("Enemy/Boss").IsChildOf(Tag("Enemy")));
        [Test] public void IsChildOf_Self_False()           => Assert.IsFalse(Tag("Enemy").IsChildOf(Tag("Enemy")));

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
    }
}
