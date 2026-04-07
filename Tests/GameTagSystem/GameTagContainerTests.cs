using NUnit.Framework;
using DataKeeper.GameTagSystem;

namespace DataKeeper.Tests.GameTagSystem
{
    public class GameTagContainerTests
    {
        private GameTagContainer _container;

        [SetUp]
        public void SetUp()
        {
            _container = new GameTagContainer();
        }

        // --- AddTag / HasTag ---

        [Test]
        public void HasTag_AfterAdd_ReturnsTrue()
        {
            var tag = new GameTag("Enemy");
            _container.AddTag(tag);
            Assert.IsTrue(_container.HasTag(tag));
        }

        [Test]
        public void HasTag_NeverAdded_ReturnsFalse()
        {
            Assert.IsFalse(_container.HasTag(new GameTag("Enemy")));
        }

        [Test]
        public void AddTag_Duplicate_DoesNotAddTwice()
        {
            var tag = new GameTag("Enemy");
            _container.AddTag(tag);
            _container.AddTag(tag);
            Assert.AreEqual(1, _container.Tags.Count);
        }

        // --- RemoveTag ---

        [Test]
        public void RemoveTag_ExistingTag_RemovesIt()
        {
            var tag = new GameTag("Enemy");
            _container.AddTag(tag);
            _container.RemoveTag(tag);
            Assert.IsFalse(_container.HasTag(tag));
        }

        [Test]
        public void RemoveTag_NotPresent_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _container.RemoveTag(new GameTag("Enemy")));
        }

        // --- Tags (IReadOnlyList) ---

        [Test]
        public void Tags_ReflectsAddedTags()
        {
            _container.AddTag(new GameTag("Enemy"));
            _container.AddTag(new GameTag("Player"));
            Assert.AreEqual(2, _container.Tags.Count);
        }

        // --- HasStartWith ---

        [Test]
        public void HasStartWith_MatchingPrefix_ReturnsTrue()
        {
            _container.AddTag(new GameTag("Enemy/Boss"));
            Assert.IsTrue(_container.HasStartsWithAndNotEquals(new GameTag("Enemy")));
        }
        
        [Test]
        public void HasStartWith_MatchingPrefix_ReturnsFalse()
        {
            _container.AddTag(new GameTag("Enemy"));
            Assert.IsFalse(_container.HasStartsWithAndNotEquals(new GameTag("Enemy")));
        }

        [Test]
        public void HasStartWith_NoMatchingPrefix_ReturnsFalse()
        {
            _container.AddTag(new GameTag("Enemy/Boss"));
            Assert.IsFalse(_container.HasStartsWithAndNotEquals(new GameTag("Player")));
        }

        [Test]
        public void HasStartWith_ExactMatch_ReturnsTrue()
        {
            // StartsWith requires the separator — exact value doesn't count
            _container.AddTag(new GameTag("Enemy"));
            Assert.IsTrue(_container.HasStartWithOrEquals(new GameTag("Enemy")));
        }
        
        [Test]
        public void HasStartWith_ExactMatch_ReturnsTrue2()
        {
            // StartsWith requires the separator — exact value doesn't count
            _container.AddTag(new GameTag("Enemy/Boss"));
            Assert.IsTrue(_container.HasStartWithOrEquals(new GameTag("Enemy")));
        }
        
        [Test]
        public void HasStartWith_ExactMatch_ReturnsTrue3()
        {
            // StartsWith requires the separator — exact value doesn't count
            _container.AddTag(new GameTag("Enemy"));
            _container.AddTag(new GameTag("Enemy/Boss"));
            Assert.IsTrue(_container.HasStartWithOrEquals(new GameTag("Enemy")));
        }

        // --- GetTagsStartsWithAndNotEquals (GameTag overload) ---

        [Test]
        public void GetTagsStartsWith_GameTag_ReturnsMatchingTags()
        {
            _container.AddTag(new GameTag("Enemy/Boss"));
            _container.AddTag(new GameTag("Enemy/Minion"));
            _container.AddTag(new GameTag("Player"));

            var results = new System.Collections.Generic.List<GameTag>(
                _container.GetTagsStartsWithAndNotEquals(new GameTag("Enemy")));

            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Exists(t => t.Value == "Enemy/Boss"));
            Assert.IsTrue(results.Exists(t => t.Value == "Enemy/Minion"));
        }

        [Test]
        public void GetTagsStartsWith_GameTag_NoMatch_ReturnsEmpty()
        {
            _container.AddTag(new GameTag("Player"));

            var results = new System.Collections.Generic.List<GameTag>(
                _container.GetTagsStartsWithAndNotEquals(new GameTag("Player")));

            Assert.AreEqual(0, results.Count);
        }

        // --- GetTagsStartsWithOrEquals (string overload) ---

        [Test]
        public void GetTagsStartsWith_String_ReturnsMatchingTags1()
        {
            _container.AddTag(new GameTag("Enemy"));
            _container.AddTag(new GameTag("Enemy/Boss"));
            _container.AddTag(new GameTag("Enemy/Boss/Elite"));
            _container.AddTag(new GameTag("Player/Enemy"));

            var results = new System.Collections.Generic.List<GameTag>(
                _container.GetTagsStartsWithOrEquals(new GameTag("Enemy")));
            
            Assert.AreEqual(3, results.Count);
        }
        
        [Test]
        public void GetTagsStartsWith_String_ReturnsMatchingTags()
        {
            _container.AddTag(new GameTag("Enemy/Boss"));
            _container.AddTag(new GameTag("Enemy/Minion"));
            _container.AddTag(new GameTag("Player"));

            var results = new System.Collections.Generic.List<GameTag>(
                _container.GetTagsStartsWithOrEquals(new GameTag("Player")));

            
            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void GetTagsStartsWith_String_NoMatch_ReturnsEmpty()
        {
            _container.AddTag(new GameTag("Player"));

            var results = new System.Collections.Generic.List<GameTag>(
                _container.GetTagsStartsWithOrEquals(new GameTag("Enemy")));

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void GetTagsStartsWith_EmptyContainer_ReturnsEmpty()
        {
            var results = new System.Collections.Generic.List<GameTag>(
                _container.GetTagsStartsWithOrEquals(new GameTag("Enemy")));

            Assert.AreEqual(0, results.Count);
        }
    }
}
