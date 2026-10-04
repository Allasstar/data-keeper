using DataKeeper.Forge.Render;
using NUnit.Framework;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class DeterminismTests
    {
        private SfxRecipe _recipe;

        [SetUp]
        public void SetUp()
        {
            var offset = ForgeTestRecipes.Oscillator(Waveform.Triangle, -12f, -9f);
            offset.StartOffsetMs = 120f;
            offset.Pan = 0.6f;

            _recipe = ForgeTestRecipes.Create(800f,
                ForgeTestRecipes.Oscillator(Waveform.Saw, -5f, -6f),
                ForgeTestRecipes.Noise(NoiseColor.Pink, -10f),
                ForgeTestRecipes.Noise(NoiseColor.Brown, -8f),
                offset);
            _recipe.Seed = 42;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_recipe);

        [Test]
        public void SameRecipeAndSeed_RenderBitIdentical()
        {
            using var first = new SfxRenderer();
            using var second = new SfxRenderer();

            first.Render(_recipe);
            second.Render(_recipe);
            AssertIdentical(first, second);

            // A reused renderer must not leak state from the previous render.
            first.Render(_recipe, 7);
            first.Render(_recipe);
            AssertIdentical(first, second);
        }

        [Test]
        public void DifferentSeed_ChangesNoiseLayers()
        {
            using var a = new SfxRenderer();
            using var b = new SfxRenderer();

            a.Render(_recipe, 1);
            b.Render(_recipe, 2);

            var differs = false;
            var outA = a.Output;
            var outB = b.Output;
            for (var i = 0; i < outA.Length && !differs; i++) differs = outA[i] != outB[i];

            Assert.IsTrue(differs);
        }

        private static void AssertIdentical(SfxRenderer a, SfxRenderer b)
        {
            var outA = a.Output;
            var outB = b.Output;
            Assert.AreEqual(outA.Length, outB.Length);

            for (var i = 0; i < outA.Length; i++)
            {
                if (System.BitConverter.SingleToInt32Bits(outA[i]) != System.BitConverter.SingleToInt32Bits(outB[i]))
                    Assert.Fail($"Sample {i} differs: {outA[i]:R} vs {outB[i]:R}");
            }
        }
    }
}
