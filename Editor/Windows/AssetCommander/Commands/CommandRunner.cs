namespace DataKeeper.Editor.Windows.AssetCommander
{
    // The single path from "the user asked for a command" to "the project changed": the command
    // bar, the keyboard and the context menu all come through here, so the confirm dialog cannot
    // be skipped by adding another entry point later.
    public static class CommandRunner
    {
        public static void Run(ICommanderCommand command, CommanderContext context)
        {
            if (command == null || !command.CanExecute(context)) return;

            var confirmed = OperationPlanDialog.Confirm(command.Plan(context));
            if (confirmed == null) return;

            command.Execute(confirmed);
            context.RefreshBoth();
        }
    }
}
