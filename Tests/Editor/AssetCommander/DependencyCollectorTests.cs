using System.Collections.Generic;
using DataKeeper.Editor.Windows.AssetCommander;
using NUnit.Framework;

namespace DataKeeper.Tests.Editor.AssetCommander
{
    // The walk reads only IndexQuery, so a prefab's whole dependency tree can be written by hand.
    public class DependencyCollectorTests
    {
        [Test]
        public void CollectsTheWholeChainIncludingAVariantsBasePrefab()
        {
            var index = new IndexQuery(new[]
            {
                Record("Assets/A/HeroVariant.prefab", "variant", AssetKind.Prefab, Guid("base")),
                Record("Assets/Base/Hero.prefab", "base", AssetKind.Prefab, Guid("mat")),
                Record("Assets/Mats/Hero.mat", "mat", AssetKind.Material, Guid("tex")),
                Record("Assets/Tex/Hero.png", "tex", AssetKind.Texture),
                Record("Assets/Unrelated.png", "other", AssetKind.Texture),
            });

            var set = Collect(index, Asset("Assets/A/HeroVariant.prefab"));

            Assert.That(set.Paths, Is.EqualTo(new[]
            {
                "Assets/Base/Hero.prefab", "Assets/Mats/Hero.mat", "Assets/Tex/Hero.png",
            }));
        }

        [Test]
        public void ScriptsShadersAndPackagesStayShared()
        {
            var index = new IndexQuery(new[]
            {
                Record("Assets/A/Hero.prefab", "prefab", AssetKind.Prefab,
                    Guid("script"), Guid("mat"), Guid("pkg")),
                Record("Assets/Code/Hero.cs", "script", AssetKind.Script),
                Record("Assets/Mats/Hero.mat", "mat", AssetKind.Material, Guid("shader")),
                Record("Assets/Shaders/Toon.shader", "shader", AssetKind.Shader),
                Record("Packages/com.x/Icon.png", "pkg", AssetKind.Texture),
            });

            var set = Collect(index, Asset("Assets/A/Hero.prefab"));

            Assert.That(set.Paths, Is.EqualTo(new[] { "Assets/Mats/Hero.mat" }));
            Assert.That(set.LeftCode, Is.EqualTo(2));
            Assert.That(set.LeftPackages, Is.EqualTo(1));
        }

        // A folder already carries its contents; only what they reach outside it is added.
        [Test]
        public void AFolderBringsOnlyTheDependenciesOutsideIt()
        {
            var index = new IndexQuery(new[]
            {
                Record("Assets/A", "folder", AssetKind.Folder),
                Record("Assets/A/Hero.prefab", "prefab", AssetKind.Prefab, Guid("inside"), Guid("outside")),
                Record("Assets/A/Hero.mat", "inside", AssetKind.Material),
                Record("Assets/Tex/Hero.png", "outside", AssetKind.Texture),
            });

            var set = Collect(index, new AssetItem("Assets/A", true, true, 0, 0));

            Assert.That(set.Paths, Is.EqualTo(new[] { "Assets/Tex/Hero.png" }));
            Assert.That(set.Members, Does.Contain("Assets/A/Hero.mat"));
        }

        [Test]
        public void ACycleTerminatesAndASelectedDependencyIsNotRepeated()
        {
            var index = new IndexQuery(new[]
            {
                Record("Assets/A/One.asset", "one", AssetKind.ScriptableObject, Guid("two")),
                Record("Assets/A/Two.asset", "two", AssetKind.ScriptableObject, Guid("one"), Guid("three")),
                Record("Assets/A/Three.asset", "three", AssetKind.ScriptableObject),
            });

            var set = Collect(index, Asset("Assets/A/One.asset"), Asset("Assets/A/Two.asset"));

            Assert.That(set.Paths, Is.EqualTo(new[] { "Assets/A/Three.asset" }));
        }

        [Test]
        public void RewriteRedirectsOnlyMappedGuids()
        {
            var map = new Dictionary<string, string> { { Guid("aaa"), Guid("bbb") } };
            var text = "m_Material: {fileID: 2100000, guid: " + Guid("aaa") + ", type: 2}\n"
                       + "m_Other: {fileID: 1, guid: " + Guid("ccc") + ", type: 3}";

            var result = GuidRemapper.Rewrite(text, map, out var changed);

            Assert.That(changed, Is.True);
            Assert.That(result, Does.Contain("guid: " + Guid("bbb") + ", type: 2"));
            Assert.That(result, Does.Contain("guid: " + Guid("ccc")));
            Assert.That(result, Does.Not.Contain(Guid("aaa")));
        }

        // ── Fixture ─────────────────────────────────────────────────────────────────────

        private static DependencySet Collect(IndexQuery index, params ICommanderItem[] selection) =>
            DependencyCollector.Collect(new IndexDependencySource(index), selection);

        private static ICommanderItem Asset(string path) => new AssetItem(path, false, false, 0, 0);

        private static AssetRecord Record(string path, string guid, AssetKind kind, params string[] deps) =>
            new AssetRecord
            {
                Guid = Guid(guid),
                Path = path,
                Kind = kind,
                DependencyGuids = deps.Length == 0 ? AssetRecord.NoGuids : deps,
            };

        private static string Guid(string seed) => seed.PadRight(32, 'f');
    }
}
