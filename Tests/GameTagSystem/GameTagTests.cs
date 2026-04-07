using NUnit.Framework;
using DataKeeper.GameTagSystem;

namespace DataKeeper.Tests.GameTagSystem
{
    public class GameTagTests
    {
        // --- Value & ToString ---
        
        [Test]
        public void Value_ReturnsConstructorString()
        {
            var tag = new GameTag("Enemy/Boss");
            Assert.AreEqual("Enemy/Boss", tag.Value);
        }

        [Test]
        public void Value_ReturnsConstructorString2()
        {
            var tag = new GameTag("Enemy/Boss/ ");
            Assert.AreEqual("Enemy/Boss", tag.Value);
        }

        [Test]
        public void ToString_ReturnsValue()
        {
            var tag = new GameTag("Player/    ");
            Assert.AreEqual("Player", tag.ToString());
        }

        [Test]
        public void ToString_NullValue_ReturnsEmptyString()
        {
            var tag = new GameTag(null);
            Assert.AreEqual(string.Empty, tag.ToString());
        }

        // --- Equals ---

        [Test]
        public void Equals_SameValue_ReturnsTrue()
        {
            var a = new GameTag("Enemy");
            var b = new GameTag("Enemy");
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_DifferentValue_ReturnsFalse()
        {
            var a = new GameTag("Enemy");
            var b = new GameTag("Player");
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_String_DifferentValue_ReturnsTrue()
        {
            var tag = new GameTag("Enemy");
            Assert.IsTrue(tag.Equals("Enemy"));
        }
        

        [Test]
        public void Equals_String_DifferentValue_ReturnsFalse()
        {
            var tag = new GameTag("Enemy");
            Assert.IsFalse(tag.Equals("Player"));
        }

        // --- StartsWith ---

        [Test]
        public void StartsWith_GameTag_DirectParent_ReturnsTrue()
        {
            var child = new GameTag("Enemy/Boss/Elite");
            var parent = new GameTag("Enemy");
            Assert.IsTrue(child.StartsWithAndNotEquals(parent));
        }

        [Test]
        public void StartsWith_GameTag_NotParent_ReturnsFalse()
        {
            var tag = new GameTag("Enemy/Boss");
            var other = new GameTag("Player");
            Assert.IsFalse(tag.StartsWithAndNotEquals(other));
        }
        
        [Test]
        public void StartsWith_GameTag_NotParent_ReturnsFalse2()
        {
            var tag = new GameTag("Enemy/Boss");
            var other = new GameTag("Enemy/Boss");
            Assert.IsFalse(tag.StartsWithAndNotEquals(other));
        }

        // --- StartsWithOrEquals ---

        [Test]
        public void Contains_GameTag_SubstringPresent_ReturnsTrue()
        {
            var tag = new GameTag("Enemy/Boss/Elite");
            Assert.IsTrue(tag.StartsWithOrEquals(new GameTag("Enemy/Boss")));
        }
        
        [Test]
        public void Contains_GameTag_SubstringPresent2_ReturnsTrue()
        {
            var tag = new GameTag("Enemy/Boss/Elite");
            Assert.IsTrue(tag.StartsWithOrEquals(new GameTag("Enemy/Boss/Elite")));
        }

        [Test]
        public void Contains_GameTag_SubstringAbsent_ReturnsFalse()
        {
            var tag = new GameTag("Enemy/Boss/Elite");
            Assert.IsFalse(tag.StartsWithOrEquals(new GameTag("Player")));
        }
        
        [Test]
        public void Contains_GameTag_SubstringAbsent_ReturnsFalse2()
        {
            var tag = new GameTag("Enemy/Boss/Elite");
            Assert.IsFalse(tag.StartsWithOrEquals(new GameTag("Ene")));
        }


        // --- GetNodes ---

        [Test]
        public void GetNodes_SingleSegment_ReturnsSingleNode()
        {
            var tag = new GameTag("Enemy");
            var nodes = tag.GetNodes();
            Assert.AreEqual(1, nodes.Count);
            Assert.AreEqual("Enemy", nodes[0].Value);
        }

        [Test]
        public void GetNodes_MultiSegment_ReturnsAllNodes()
        {
            var tag = new GameTag("Enemy/Boss/Elite");
            var nodes = tag.GetNodes();
            Assert.AreEqual(3, nodes.Count);
            Assert.AreEqual("Enemy", nodes[0].Value);
            Assert.AreEqual("Boss", nodes[1].Value);
            Assert.AreEqual("Elite", nodes[2].Value);
        }

        [Test]
        public void GetNodes_NullValue_ReturnsEmpty()
        {
            var tag = new GameTag(null);
            var nodes = tag.GetNodes(); // Error is not thrown
            Assert.AreEqual(0, nodes.Count);
        }

        [Test]
        public void GetNodes_CalledTwice_ReturnsSameNodes()
        {
            var tag = new GameTag("Enemy/Boss");
            var first = tag.GetNodes();
            var second = tag.GetNodes();
            Assert.AreSame(first, second);
        }
    }
}
