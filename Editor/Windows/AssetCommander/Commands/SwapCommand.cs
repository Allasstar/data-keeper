using System.Collections.Generic;
using UnityEditor;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Exchanges two assets' GUIDs, so everything that referenced the first now resolves to the
    // second and vice versa — the one operation here that rewrites .meta files behind the
    // AssetDatabase's back, which is why it is gated on ForceText and shares its implementation
    // with the standalone GUIDSwapper window.
    public sealed class SwapCommand : ICommanderCommand
    {
        public string Id => "swap";
        public string DisplayName => "Swap";

        public string Tooltip =>
            "Exchange the GUIDs of exactly two assets, redirecting every reference from one to "
            + "the other. Requires Force Text serialization.";

        public bool CanExecute(CommanderContext context) => TryGetPair(context, out _, out _);

        public OperationPlan Plan(CommanderContext context)
        {
            if (!TryGetPair(context, out var first, out var second))
                return OperationPlan.Rejected("Swap",
                    "Select exactly two assets — both on one side, or one on each.");

            if (!GuidSwapService.IsSupported)
                return OperationPlan.Rejected("Swap",
                    "Asset Serialization is not set to Force Text. A GUID swap rewrites .meta "
                    + "files, which only redirects references in text-serialized projects.");

            var plan = new OperationPlan("Swap", "Swap") { Context = context };

            plan.Add(null, first.AssetPath, second.AssetPath);
            plan.Add(null, second.AssetPath, first.AssetPath);

            plan.Summary = "Every reference to either asset will resolve to the other.";
            plan.Caveat = "This rewrites both .meta files. It is not covered by Undo — the way "
                          + "back is to swap them again.";

            return plan;
        }

        public void Execute(OperationPlan plan)
        {
            var first = plan.Operations[0].Source;
            var second = plan.Operations[0].Destination;

            if (!GuidSwapService.Swap(first, second, false, out var error))
            {
                EditorUtility.DisplayDialog("Swap failed", error, "OK");
                return;
            }

            AssetDatabase.Refresh();
        }

        // Two on the active side is the plain case; one on each side is what the two-panel layout
        // makes natural, so both are accepted.
        private static bool TryGetPair(CommanderContext context, out ICommanderItem first,
            out ICommanderItem second)
        {
            first = null;
            second = null;

            var active = context.Active;
            var other = context.Other;

            if (active.Count == 2 && active.SelectionIsAssets())
            {
                first = active.Selection[0];
                second = active.Selection[1];
            }
            else if (active.Count == 1 && other.Count == 1
                     && active.SelectionIsAssets() && other.SelectionIsAssets())
            {
                first = active.Selection[0];
                second = other.Selection[0];
            }
            else
            {
                return false;
            }

            if (first.Kind == CommanderItemKind.Folder || second.Kind == CommanderItemKind.Folder)
            {
                first = null;
                second = null;
                return false;
            }

            return first.AssetPath != second.AssetPath;
        }
    }
}
