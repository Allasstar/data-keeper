using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataKeeper.Editor.Windows.AssetCommander;
using NUnit.Framework;

namespace DataKeeper.Tests.Editor.AssetCommander
{
    public class GuidScannerTests
    {
        private const string ScriptGuid = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string MaterialGuid = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        private const string TextureGuid = "cccccccccccccccccccccccccccccccc";

        private const string PrefabYaml =
            "%YAML 1.1\n"
            + "%TAG !u! tag:unity3d.com,2011:\n"
            + "--- !u!1 &1234567890\n"
            + "GameObject:\n"
            + "  m_Component:\n"
            + "  - component: {fileID: 1234567891}\n"
            + "--- !u!114 &1234567891\n"
            + "MonoBehaviour:\n"
            + "  m_Script: {fileID: 11500000, guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 3}\n"
            + "  _material: {fileID: 2100000, guid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb, type: 2}\n"
            + "  _texture: {fileID: 2800000, guid: CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC, type: 3}\n"
            + "  _again: {fileID: 2100000, guid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb, type: 2}\n";

        private readonly List<string> _tempFiles = new List<string>();

        [TearDown]
        public void TearDown()
        {
            foreach (var path in _tempFiles)
            {
                if (File.Exists(path)) File.Delete(path);
            }

            _tempFiles.Clear();
        }

        [Test]
        public void ScanBuffer_FindsEveryDistinctGuid()
        {
            var dependencies = Scan(PrefabYaml, out _);

            CollectionAssert.AreEquivalent(
                new[] { ScriptGuid, MaterialGuid, TextureGuid },
                dependencies);
        }

        [Test]
        public void ScanBuffer_ReportsScriptBindingSeparately()
        {
            Scan(PrefabYaml, out var scriptRefs);

            CollectionAssert.AreEquivalent(new[] { ScriptGuid }, scriptRefs);
        }

        [Test]
        public void ScanBuffer_NormalisesUppercaseHexToLowercase()
        {
            var dependencies = Scan(PrefabYaml, out _);

            Assert.That(dependencies, Contains.Item(TextureGuid));
        }

        [Test]
        public void ScanBuffer_WithoutGuids_YieldsEmptySet()
        {
            var dependencies = Scan("using UnityEngine;\npublic class Foo : MonoBehaviour { }\n", out var scriptRefs);

            Assert.That(dependencies, Is.Empty);
            Assert.That(scriptRefs, Is.Empty);
        }

        [Test]
        public void ScanBuffer_IgnoresTokensLongerThan32HexDigits()
        {
            var dependencies = Scan("  hash: guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaff\n", out _);

            Assert.That(dependencies, Is.Empty);
        }

        [Test]
        public void ScanFile_FindsGuidsSplitAcrossEveryChunkBoundary()
        {
            var path = WriteTemp(PrefabYaml);
            var expected = new[] { ScriptGuid, MaterialGuid, TextureGuid };

            // Small chunk sizes force the guid — and the "m_Script:" that precedes one of
            // them — to straddle a read boundary at a different offset each time.
            foreach (int chunkSize in new[] { 1, 7, 8, 16, 37, 64, 128, 1024 })
            {
                var scratch = new ScanScratch();
                scratch.Reset();

                Assert.That(GuidScanner.ScanFile(path, scratch, forceGuidScan: true, hashContent: true, chunkSize),
                    Is.True, $"chunk size {chunkSize}");

                CollectionAssert.AreEquivalent(expected, scratch.DependencyStrings(null),
                    $"dependencies at chunk size {chunkSize}");
                CollectionAssert.AreEquivalent(new[] { ScriptGuid }, scratch.ScriptStrings(),
                    $"script refs at chunk size {chunkSize}");
            }
        }

        [Test]
        public void ScanFile_HashIsIndependentOfChunkSize()
        {
            var path = WriteTemp(PrefabYaml);
            var hashes = new HashSet<ulong>();

            foreach (int chunkSize in new[] { 1, 7, 64, 1024 })
            {
                var scratch = new ScanScratch();
                scratch.Reset();
                GuidScanner.ScanFile(path, scratch, forceGuidScan: true, hashContent: true, chunkSize);
                hashes.Add(scratch.Hash.Digest());
            }

            Assert.That(hashes, Has.Count.EqualTo(1));
        }

        [Test]
        public void ScanFile_SniffsYamlWhenTheExtensionIsUnknown()
        {
            var path = WriteTemp(PrefabYaml);
            var scratch = new ScanScratch();
            scratch.Reset();

            GuidScanner.ScanFile(path, scratch, forceGuidScan: false, hashContent: true);

            Assert.That(scratch.DependencyStrings(null), Has.Length.EqualTo(3));
        }

        [Test]
        public void ScanFile_SkipsGuidScanForNonYamlContent()
        {
            // A .cs file quoting a guid in a string literal is not a dependency.
            var path = WriteTemp("const string Guid = \"guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\";\n");
            var scratch = new ScanScratch();
            scratch.Reset();

            GuidScanner.ScanFile(path, scratch, forceGuidScan: false, hashContent: true);

            Assert.That(scratch.DependencyStrings(null), Is.Empty);
        }

        [Test]
        public void DependencyStrings_DropsTheAssetsOwnGuid()
        {
            var scratch = new ScanScratch();
            scratch.Reset();
            GuidScanner.ScanBuffer(Encoding.UTF8.GetBytes(PrefabYaml), PrefabYaml.Length,
                scratch.Dependencies, scratch.ScriptRefs);

            var dependencies = scratch.DependencyStrings(MaterialGuid);

            Assert.That(dependencies, Does.Not.Contain(MaterialGuid));
            Assert.That(dependencies, Has.Length.EqualTo(2));
        }

        [Test]
        public void GuidKey_RoundTripsThroughText()
        {
            Assert.That(GuidKey.TryParse(MaterialGuid, out var key), Is.True);
            Assert.That(key.ToString(), Is.EqualTo(MaterialGuid));
        }

        [Test]
        public void GuidKey_RejectsMalformedText()
        {
            Assert.That(GuidKey.TryParse("not-a-guid", out _), Is.False);
            Assert.That(GuidKey.TryParse(MaterialGuid.Substring(1), out _), Is.False);
            Assert.That(GuidKey.TryParse(null, out _), Is.False);
        }

        private static string[] Scan(string text, out string[] scriptRefs)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var dependencies = new HashSet<GuidKey>();
            var scripts = new HashSet<GuidKey>();

            GuidScanner.ScanBuffer(bytes, bytes.Length, dependencies, scripts);

            scriptRefs = scripts.Select(key => key.ToString()).ToArray();
            return dependencies.Select(key => key.ToString()).ToArray();
        }

        private string WriteTemp(string contents)
        {
            var path = Path.Combine(Path.GetTempPath(), $"dk-scanner-{Path.GetRandomFileName()}");
            File.WriteAllText(path, contents, new UTF8Encoding(false));
            _tempFiles.Add(path);
            return path;
        }
    }
}
