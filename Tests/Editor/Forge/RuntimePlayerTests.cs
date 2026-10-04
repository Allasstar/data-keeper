using System.Collections.Generic;
using DataKeeper.Forge.Render;
using DataKeeper.Forge.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace DataKeeper.Forge.Tests
{
    public class RuntimePlayerTests
    {
        private SfxRecipe _recipe;
        private readonly List<Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            _recipe = ForgeTestRecipes.Create(300f,
                ForgeTestRecipes.Oscillator(Waveform.Saw, -12f, -9f),
                ForgeTestRecipes.Noise(NoiseColor.Pink, -12f));
            _recipe.name = "Runtime Test";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                if (obj != null) Object.DestroyImmediate(obj);
            _created.Clear();
            Object.DestroyImmediate(_recipe);
        }

        [Test]
        public void Render_WithoutVariationMatchesTheOfflineRenderer()
        {
            var clip = Track(SfxClipRenderer.Render(_recipe, 9u, 0f));
            using var renderer = new SfxRenderer();
            renderer.Render(_recipe, 9u);

            Assert.AreEqual(renderer.FrameCount, clip.samples);
            Assert.AreEqual(renderer.SampleRate, clip.frequency);
            Assert.AreEqual(1, clip.channels, "a centred recipe has identical sides and becomes mono");

            var data = new float[clip.samples];
            clip.GetData(data, 0);
            var output = renderer.Output;
            for (var i = 0; i < data.Length; i++) Assert.AreEqual(output[i * 2], data[i], 1e-6f, $"frame {i}");
        }

        [Test]
        public void Render_StereoRecipeStaysStereo()
        {
            _recipe.Layers[0].Pan = -0.8f;
            var clip = Track(SfxClipRenderer.Render(_recipe, 9u, 0f));
            Assert.AreEqual(2, clip.channels);
        }

        [Test]
        public void Render_VariationIsDeterministicAndLeavesTheRecipeAlone()
        {
            var before = _recipe.ToJson();

            var a = Data(Track(SfxClipRenderer.Render(_recipe, 4u, 0.6f)));
            var b = Data(Track(SfxClipRenderer.Render(_recipe, 4u, 0.6f)));
            var plain = Data(Track(SfxClipRenderer.Render(_recipe, 4u, 0f)));

            CollectionAssert.AreEqual(a, b);
            CollectionAssert.AreNotEqual(a, plain);
            Assert.AreEqual(before, _recipe.ToJson());
        }

        [Test]
        public void Pool_PlaysEveryClipOncePerRoundWithoutBackToBackRepeats()
        {
            var host = new GameObject("Pool Test");
            _created.Add(host);
            var pool = host.AddComponent<SfxVariationPool>();
            pool.SetRecipe(_recipe);
            foreach (var clip in pool.Clips) _created.Add(clip);

            Assert.AreEqual(8, pool.Clips.Count);

            AudioClip previous = null;
            for (var round = 0; round < 5; round++)
            {
                var seen = new HashSet<AudioClip>();
                for (var i = 0; i < pool.Clips.Count; i++)
                {
                    var clip = pool.Next();
                    Assert.AreNotSame(previous, clip, $"round {round} pick {i}");
                    Assert.IsTrue(seen.Add(clip), $"round {round} repeated a clip");
                    previous = clip;
                }
            }
        }

        private AudioClip Track(AudioClip clip)
        {
            _created.Add(clip);
            return clip;
        }

        private static float[] Data(AudioClip clip)
        {
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            return data;
        }
    }
}
