using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DataKeeper.Editor.Windows.AssetCommander
{
    // Buttons, keys and context-menu entries are all the same list of commands asked the same
    // question — CanExecute against the current context — so a command that is wrong for the
    // current selection is visibly dead rather than failing when pressed.
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
                    text = Label(command),
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

        // Returns whether the key was claimed, so the window only swallows a keystroke a command
        // actually took.
        public bool HandleKey(KeyCode key, EventModifiers modifiers)
        {
            var command = CommanderCommands.ForShortcut(key, modifiers);
            if (command == null || !command.CanExecute(_context())) return false;

            Run(command);
            return true;
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

                menu.AppendAction(Label(command), _ => Run(captured), status);
            }
        }

        private static string Label(ICommanderCommand command)
        {
            var shortcuts = command.Shortcuts;
            if (shortcuts.Count == 0) return command.DisplayName;

            var keys = shortcuts[0].Label;
            for (int i = 1; i < shortcuts.Count; i++) keys += "/" + shortcuts[i].Label;

            return command.DisplayName + " (" + keys + ")";
        }
    }
}
