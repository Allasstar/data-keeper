using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Buttons and context-menu entries are the same list of commands asked the same question —
    // CanExecute against the current context — so a command that is wrong for the current
    // selection is visibly dead rather than failing when pressed. No command is bound to a key:
    // the function keys and Ctrl+D this bar used to claim collide with the editor's own global
    // shortcuts, and a destructive command reached by a stray keystroke is the one mistake this
    // window must not make.
    public sealed class CommandBarView
    {
        private readonly Func<CommanderContext> _context;
        private readonly Dictionary<ICommanderCommand, Button> _buttons =
            new Dictionary<ICommanderCommand, Button>();

        public CommandBarView(VisualElement root, Func<CommanderContext> context)
        {
            _context = context;

            var host = root.Q<VisualElement>("command-buttons");

            foreach (var command in CommanderCommands.All)
            {
                var captured = command;
                var button = new Button(() => Run(captured))
                {
                    text = command.DisplayName,
                    tooltip = command.Tooltip,
                };

                button.AddToClassList("ac-command");
                host.Add(button);
                _buttons[command] = button;
            }
        }

        public void Sync()
        {
            var context = _context();

            foreach (var pair in _buttons)
                pair.Value.SetEnabled(pair.Key.CanExecute(context));
        }

        public void Run(ICommanderCommand command)
        {
            CommandRunner.Run(command, _context());
            Sync();
        }

        public void PopulateMenu(DropdownMenu menu)
        {
            var context = _context();

            foreach (var command in CommanderCommands.All)
            {
                var captured = command;
                var status = command.CanExecute(context)
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled;

                menu.AppendAction(command.DisplayName, _ => Run(captured), status);
            }
        }
    }
}
