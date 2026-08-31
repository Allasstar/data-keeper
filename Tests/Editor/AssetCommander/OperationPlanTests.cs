using System.Collections.Generic;
using System.Linq;
using DataKeeper.Editor.Windows.AssetCommander;
using NUnit.Framework;

namespace DataKeeper.Tests.Editor.AssetCommander
{
    // Destination resolution is the part of a destructive command worth pinning, and it is pure
    // string work — the existence test is injected, so nothing here touches the disk or the
    // AssetDatabase.
    public class OperationPlanTests
    {
        private const string SideA = "Assets/A";
        private const string SideB = "Assets/B";

        [Test]
        public void KeepStructure_ReproducesTheSubfolderUnderTheTarget()
        {
            var plan = Plan(new[] { "Assets/A/Props/Crate.prefab" }, FolderStructure.KeepStructure);

            Assert.That(Destinations(plan), Is.EqualTo(new[] { "Assets/B/Props/Crate.prefab" }));
        }

        [Test]
        public void Flatten_DropsEverythingDirectlyIntoTheTarget()
        {
            var plan = Plan(new[] { "Assets/A/Props/Crate.prefab", "Assets/A/Mats/Wood.mat" },
                FolderStructure.Flatten);

            Assert.That(Destinations(plan),
                Is.EqualTo(new[] { "Assets/B/Crate.prefab", "Assets/B/Wood.mat" }));
        }

        // A result set spans folders, so an item that is not under the stated root has no
        // relative part to keep and lands in the target either way.
        [Test]
        public void KeepStructure_FlattensAnItemFromOutsideTheRoot()
        {
            var plan = Plan(new[] { "Assets/Elsewhere/Crate.prefab" }, FolderStructure.KeepStructure);

            Assert.That(Destinations(plan), Is.EqualTo(new[] { "Assets/B/Crate.prefab" }));
        }

        [Test]
        public void AutoRename_ProducesAPathThatIsNotTaken()
        {
            var plan = Plan(new[] { "Assets/A/Crate.prefab" }, FolderStructure.Flatten,
                ConflictResolution.AutoRename, "Assets/B/Crate.prefab");

            Assert.That(Destinations(plan), Is.EqualTo(new[] { "Assets/B/Crate 1.prefab" }));
            Assert.That(plan.Operations[0].Overwrites, Is.False);
        }

        // Two sources with one name are a collision the disk cannot report yet, so the planner
        // has to treat its own earlier rows as taken.
        [Test]
        public void AutoRename_KeepsRowsOfTheSamePlanApart()
        {
            var plan = Plan(new[] { "Assets/A/One/Crate.prefab", "Assets/A/Two/Crate.prefab" },
                FolderStructure.Flatten);

            Assert.That(Destinations(plan),
                Is.EqualTo(new[] { "Assets/B/Crate.prefab", "Assets/B/Crate 1.prefab" }));
        }

        [Test]
        public void Overwrite_TargetsTheExistingPathAndFlagsTheRow()
        {
            var plan = Plan(new[] { "Assets/A/Crate.prefab" }, FolderStructure.Flatten,
                ConflictResolution.Overwrite, "Assets/B/Crate.prefab");

            Assert.That(Destinations(plan), Is.EqualTo(new[] { "Assets/B/Crate.prefab" }));
            Assert.That(plan.Operations[0].Overwrites, Is.True);
            Assert.That(plan.Operations[0].Alert, Is.True);
        }

        [Test]
        public void Skip_DropsTheCollidingRowAndKeepsTheRest()
        {
            var plan = Plan(new[] { "Assets/A/Crate.prefab", "Assets/A/Barrel.prefab" },
                FolderStructure.Flatten, ConflictResolution.Skip, "Assets/B/Crate.prefab");

            Assert.That(Destinations(plan), Is.EqualTo(new[] { "Assets/B/Barrel.prefab" }));
            Assert.That(plan.Summary, Does.Contain("1 skipped"));
        }

        [Test]
        public void MovingIntoTheFolderTheAssetIsAlreadyInIsRejected()
        {
            var plan = Build(new[] { "Assets/B/Crate.prefab" }, SideA, SideB,
                new PlanOptions(ConflictResolution.AutoRename, FolderStructure.Flatten),
                rejectSameFolder: true);

            Assert.That(plan.Operations, Is.Empty);
            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.Blocked, Does.Contain(SideB));
        }

        [Test]
        public void CopyingIntoTheFolderTheAssetIsAlreadyInIsAllowed()
        {
            var plan = Build(new[] { "Assets/B/Crate.prefab" }, SideA, SideB,
                new PlanOptions(ConflictResolution.AutoRename, FolderStructure.Flatten),
                false, "Assets/B/Crate.prefab");

            Assert.That(Destinations(plan), Is.EqualTo(new[] { "Assets/B/Crate 1.prefab" }));
        }

        [Test]
        public void AFolderCannotBeMovedInsideItself()
        {
            var items = new List<ICommanderItem> { new AssetItem("Assets/A/Props", true, true, 0, 0) };

            var plan = new TransferPlanner(path => false).Build(items, SideA, "Assets/A/Props/Nested",
                new PlanOptions(ConflictResolution.AutoRename, FolderStructure.Flatten),
                "Move", "Move", true);

            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.Blocked, Does.Contain("inside itself"));
        }

        [Test]
        public void AnEmptyTargetSideIsRejectedBeforeAnyRowIsResolved()
        {
            var plan = Build(new[] { "Assets/A/Crate.prefab" }, SideA, null,
                new PlanOptions(ConflictResolution.AutoRename, FolderStructure.Flatten), true);

            Assert.That(plan.IsBlocked, Is.True);
            Assert.That(plan.Operations, Is.Empty);
        }

        // ── Rename patterns ─────────────────────────────────────────────────────────────

        [Test]
        public void PatternExpandsNameAndCounterTokens()
        {
            Assert.That(NamePattern.Apply("Prop_{n:000}", "Crate", 5), Is.EqualTo("Prop_005"));
            Assert.That(NamePattern.Apply("{name}_{n}", "Crate", 2), Is.EqualTo("Crate_2"));
            Assert.That(NamePattern.Apply("Old_{name}", "Crate", 1), Is.EqualTo("Old_Crate"));
        }

        [Test]
        public void PatternWithoutTokensIsALiteralName()
        {
            Assert.That(NamePattern.Apply("Crate", "Barrel", 3), Is.EqualTo("Crate"));
        }

        [Test]
        public void PatternLeavesAnUnknownTokenAsTyped()
        {
            Assert.That(NamePattern.Apply("{nope}_{n}", "Crate", 1), Is.EqualTo("{nope}_1"));
        }

        // ── Fixture ─────────────────────────────────────────────────────────────────────

        private static OperationPlan Plan(string[] sources, FolderStructure structure,
            ConflictResolution conflict = ConflictResolution.AutoRename, params string[] existing)
        {
            return Build(sources, SideA, SideB, new PlanOptions(conflict, structure), true, existing);
        }

        private static OperationPlan Build(string[] sources, string sourceRoot, string targetRoot,
            PlanOptions options, bool rejectSameFolder, params string[] existing)
        {
            var onDisk = new HashSet<string>(existing ?? new string[0]);
            var items = sources.Select(path => (ICommanderItem)new AssetItem(path, false, false, 0, 0))
                .ToList();

            return new TransferPlanner(onDisk.Contains)
                .Build(items, sourceRoot, targetRoot, options, "Transfer", "Transfer", rejectSameFolder);
        }

        private static string[] Destinations(OperationPlan plan) =>
            plan.Operations.Select(operation => operation.Destination).ToArray();
    }
}
