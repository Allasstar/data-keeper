using System.Collections.Generic;
using System.Linq;
using DataKeeper.Editor.Windows.AssetCommander;
using NUnit.Framework;

namespace DataKeeper.Tests.Editor.AssetCommander
{
    // Every folder-side mode is a query over IndexQuery, so all of it can be asked of records
    // written by hand — no assets on disk, no AssetDatabase, no scene. The scene branches of
    // Broken References, Missing Scripts and Cross-Side need live GameObjects and are covered by
    // the playtest instead.
    public class ModeTests
    {
        private const string SideA = "Assets/A";
        private const string SideB = "Assets/B";

        [Test]
        public void BrokenReferences_ListsOnlyAssetsPointingAtAMissingGuid()
        {
            var index = Index(
                Record("Assets/A/Broken.prefab", "broken", deps: new[] { Guid("gone") }),
                Record("Assets/A/Fine.prefab", "fine", deps: new[] { Guid("mat") }),
                Record("Assets/A/Builtin.mat", "builtin", deps: new[] { "0000000000000000f000000000000000" }),
                Record("Assets/A/Target.mat", "mat"));

            var result = new BrokenReferencesMode().Evaluate(Context(index, SideA));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/Broken.prefab" }));
        }

        [Test]
        public void BrokenReferences_AnnotatesTheMissingGuidAndCallsOutAMissingScript()
        {
            var missing = Guid("dead");
            var index = Index(Record("Assets/A/Broken.prefab", "broken",
                deps: new[] { missing }, scripts: new[] { missing }));

            var result = new BrokenReferencesMode().Evaluate(Context(index, SideA));

            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].Badge, Does.Contain(missing.Substring(0, 8)));
            Assert.That(result.Items[0].Badge, Does.StartWith("missing script"));
            Assert.That(result.Items[0].BadgeIsAlert, Is.True);
        }

        [Test]
        public void BrokenReferences_IgnoresAssetsOutsideTheSidesRoot()
        {
            var index = Index(
                Record("Assets/A/Broken.prefab", "a", deps: new[] { Guid("gone") }),
                Record("Assets/B/Broken.prefab", "b", deps: new[] { Guid("gone") }));

            var result = new BrokenReferencesMode().Evaluate(Context(index, SideA));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/Broken.prefab" }));
        }

        [Test]
        public void MissingScripts_ListsAssetsWhoseScriptBindingResolvesToNothing()
        {
            var index = Index(
                Record("Assets/A/Orphan.prefab", "orphan",
                    deps: new[] { Guid("script") }, scripts: new[] { Guid("script") }),
                Record("Assets/A/Ok.prefab", "ok",
                    deps: new[] { Guid("real") }, scripts: new[] { Guid("real") }),
                Record("Assets/A/Real.cs", "real", AssetKind.Script));

            var result = new MissingScriptsMode().Evaluate(Context(index, SideA));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/Orphan.prefab" }));
            Assert.That(result.Items[0].Badge, Is.EqualTo("1 missing script"));
        }

        [Test]
        public void Unused_ExcludesAnythingABuildSceneCanReachHoweverIndirectly()
        {
            var index = Index(
                Record("Assets/A/Level.unity", "scene", AssetKind.Scene, deps: new[] { Guid("prefab") }),
                Record("Assets/A/Player.prefab", "prefab", AssetKind.Prefab, deps: new[] { Guid("mat") }),
                Record("Assets/A/Body.mat", "mat", AssetKind.Material),
                Record("Assets/A/Loose.mat", "loose", AssetKind.Material));

            var mode = new UnusedAssetsMode(_ => new[] { Guid("scene") });
            var result = mode.Evaluate(Context(index, SideA));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/Loose.mat" }));
        }

        [Test]
        public void Unused_KeepsAnAssetWhoseOnlyReferrerIsItselfUnreachable()
        {
            // A dead prefab still referencing a material means that material has a referrer, so
            // an empty reverse map alone would miss it; the mode reports the prefab and leaves
            // the material for the pass after the prefab is gone.
            var index = Index(
                Record("Assets/A/Dead.prefab", "dead", AssetKind.Prefab, deps: new[] { Guid("mat") }),
                Record("Assets/A/Body.mat", "mat", AssetKind.Material));

            var result = new UnusedAssetsMode(_ => System.Array.Empty<string>())
                .Evaluate(Context(index, SideA));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/Dead.prefab" }));
        }

        [Test]
        public void Unused_NeverListsScripts()
        {
            var index = Index(Record("Assets/A/Tool.cs", "tool", AssetKind.Script));

            var result = new UnusedAssetsMode(_ => System.Array.Empty<string>())
                .Evaluate(Context(index, SideA));

            Assert.That(result.Items, Is.Empty);
            Assert.That(result.Caveat, Is.Not.Null);
        }

        [Test]
        public void Duplicates_GroupsAssetsWithEqualContentHashAndSkipsUniqueOnes()
        {
            var index = Index(
                Record("Assets/A/One.png", "one", AssetKind.Texture, hash: 7UL),
                Record("Assets/A/Two.png", "two", AssetKind.Texture, hash: 7UL),
                Record("Assets/A/Other.png", "other", AssetKind.Texture, hash: 9UL),
                Record("Assets/A/Unhashed.png", "unhashed", AssetKind.Texture));

            var result = new DuplicatesMode().Evaluate(Context(index, SideA));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/One.png", "Assets/A/Two.png" }));
            Assert.That(result.Items[0].Badge, Does.StartWith("2 copies"));
        }

        [Test]
        public void Duplicates_SaysWhenTheTwinIsOnTheOtherSide()
        {
            var index = Index(
                Record("Assets/A/One.png", "one", AssetKind.Texture, hash: 7UL),
                Record("Assets/B/Copy.png", "copy", AssetKind.Texture, hash: 7UL));

            var result = new DuplicatesMode().Evaluate(Context(index, SideA, SideB));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/One.png" }));
            Assert.That(result.Items[0].Badge, Does.Contain("also in B"));
        }

        [Test]
        public void CrossSide_ForwardListsWhatThisSideReferencesOnTheOther()
        {
            var index = Index(
                Record("Assets/A/Uses.prefab", "uses", AssetKind.Prefab, deps: new[] { Guid("shared") }),
                Record("Assets/A/Alone.prefab", "alone", AssetKind.Prefab),
                Record("Assets/B/Shared.mat", "shared", AssetKind.Material));

            var result = new CrossSideReferencesMode().Evaluate(Context(index, SideA, SideB));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/Uses.prefab" }));
            Assert.That(result.Items[0].Badge, Is.EqualTo("→ 1 B"));
        }

        [Test]
        public void CrossSide_ReverseListsWhatTheOtherSideReferencesHere()
        {
            var index = Index(
                Record("Assets/A/Shared.mat", "shared", AssetKind.Material),
                Record("Assets/A/Alone.mat", "alone", AssetKind.Material),
                Record("Assets/B/Uses.prefab", "uses", AssetKind.Prefab, deps: new[] { Guid("shared") }));

            var result = new CrossSideReferencesMode()
                .Evaluate(Context(index, SideA, SideB, reverse: true));

            Assert.That(Paths(result), Is.EquivalentTo(new[] { "Assets/A/Shared.mat" }));
            Assert.That(result.Items[0].Badge, Is.EqualTo("← 1 B"));
        }

        [Test]
        public void CrossSide_SaysNothingWhenTheOtherSideIsEmpty()
        {
            var index = Index(Record("Assets/A/Uses.prefab", "uses", AssetKind.Prefab));

            var result = new CrossSideReferencesMode().Evaluate(Context(index, SideA));

            Assert.That(result.Items, Is.Empty);
            Assert.That(result.Summary, Is.EqualTo("The other side is empty"));
        }

        [Test]
        public void SceneOnlyAndAssetOnlyModesDeclareWhatTheySupport()
        {
            Assert.That(new DuplicatesMode().Supports(SideKind.Scene), Is.False);
            Assert.That(new UnusedAssetsMode().Supports(SideKind.Scene), Is.False);
            Assert.That(new BrokenReferencesMode().Supports(SideKind.Scene), Is.True);
            Assert.That(CrossSideReferencesMode.SupportsReverse(SideKind.Scene), Is.False);
        }

        [Test]
        public void EveryRegisteredModeIsReachableById()
        {
            foreach (var mode in CommanderModes.All)
                Assert.That(CommanderModes.Get(mode.Id), Is.SameAs(mode));

            Assert.That(CommanderModes.Get("no-such-mode").Id, Is.EqualTo(CommanderModes.SearchId));
        }

        [TestCase("Assets/A/x.mat", "Assets/A", true)]
        [TestCase("Assets/A", "Assets/A", true)]
        [TestCase("Assets/AB/x.mat", "Assets/A", false)]
        [TestCase("Assets/x.mat", "Assets/A", false)]
        public void IsUnder_TreatsOnlyWholeSegmentsAsInside(string path, string root, bool expected)
        {
            Assert.That(ModeScope.IsUnder(path, root), Is.EqualTo(expected));
        }

        // ── Search filter ───────────────────────────────────────────────────────────────

        [TestCase("play", true)]
        [TestCase("PLAY", true)]
        [TestCase("play zzz", false)]
        [TestCase("play prefab", true)]
        [TestCase("t:prefab", true)]
        [TestCase("t:Prefab play", true)]
        [TestCase("t:mat", false)]
        [TestCase("play*.prefab", true)]
        [TestCase("*.prefab", true)]
        [TestCase("*.mat", false)]
        [TestCase("Play?r.prefab", true)]
        [TestCase("Play??.prefab", false)]
        public void SearchFilter_MatchesNamesTypesAndGlobs(string query, bool expected)
        {
            var item = new AssetItem("Assets/A/Player.prefab", false, false, 0, 0);

            Assert.That(SearchFilter.Parse(query).Matches(item), Is.EqualTo(expected));
        }

        [Test]
        public void SearchFilter_EmptyQueryMatchesEverything()
        {
            var filter = SearchFilter.Parse("   ");

            Assert.That(filter.IsEmpty, Is.True);
            Assert.That(filter.Matches(new AssetItem("Assets/A/x.mat", false, false, 0, 0)), Is.True);
        }

        // ── Fixture ─────────────────────────────────────────────────────────────────────

        private static IndexQuery Index(params AssetRecord[] records) => new IndexQuery(records);

        private static ModeContext Context(IndexQuery index, string selfRoot, string otherRoot = null,
            bool reverse = false)
        {
            var self = new SideContext(SideId.A, SideKind.Folder, selfRoot, reverse);
            var other = otherRoot == null
                ? new SideContext(SideId.B, SideKind.None, "")
                : new SideContext(SideId.B, SideKind.Folder, otherRoot);

            return new ModeContext(self, other, index);
        }

        private static AssetRecord Record(string path, string guid, AssetKind kind = AssetKind.ScriptableObject,
            string[] deps = null, string[] scripts = null, ulong hash = 0UL)
        {
            return new AssetRecord
            {
                Guid = Guid(guid),
                Path = path,
                Kind = kind,
                ContentHash = hash,
                DependencyGuids = deps ?? AssetRecord.NoGuids,
                ScriptGuids = scripts ?? AssetRecord.NoGuids,
            };
        }

        // Guids are compared and sliced as 32-character strings everywhere in the index, so a
        // fixture guid has to be one.
        private static string Guid(string seed) => seed.PadRight(32, 'f');

        private static IEnumerable<string> Paths(ModeResult result) => result.Items.Select(item => item.AssetPath);
    }
}
