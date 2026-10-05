using DataKeeper.Forge.Render;
using NUnit.Framework;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class RootNoteTests
    {
        private SfxRecipe _recipe;

        [SetUp]
        public void SetUp()
        {
            _recipe = ForgeTestRecipes.Create(300f,
                ForgeTestRecipes.Oscillator(Waveform.Saw, 0f, -12f),
                ForgeTestRecipes.Oscillator(Waveform.Square, 7f, -12f));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_recipe);

        [Test]
        public void DefaultRootNote_IsA4()
        {
            Assert.AreEqual(69, _recipe.RootNote);
        }

        [Test]
        public void RootNoteA4_RendersBitIdenticalToUntransposedPitch()
        {
            using var reference = new SfxRenderer();
            reference.Render(_recipe);

            _recipe.RootNote = SfxRecipe.DefaultRootNote;
            using var rendered = new SfxRenderer();
            rendered.Render(_recipe);

            AssertIdentical(reference, rendered);
        }

        [Test]
        public void RootNoteOctaveUp_EqualsEveryLayerPitchPlusTwelve()
        {
            _recipe.RootNote = SfxRecipe.DefaultRootNote + 12;
            using var transposed = new SfxRenderer();
            transposed.Render(_recipe);

            _recipe.RootNote = SfxRecipe.DefaultRootNote;
            foreach (var layer in _recipe.Layers) layer.Pitch += 12f;
            using var shifted = new SfxRenderer();
            shifted.Render(_recipe);

            AssertIdentical(shifted, transposed);
        }

        private static void AssertIdentical(SfxRenderer expected, SfxRenderer actual)
        {
            var a = expected.Output;
            var b = actual.Output;
            Assert.AreEqual(a.Length, b.Length);
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) Assert.Fail($"Sample {i} differs: {a[i]:R} vs {b[i]:R}");
            }
        }
    }
}
