using System;
using System.Collections.Generic;

namespace Linage.GUI
{
    public class CommandDispatcher
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Ready";

        private Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();

        public CommandDispatcher()
        {
            RegisterDefaultCommands();
        }

        private void RegisterDefaultCommands()
        {
            // Commands will be registered here
        }

        public void RegisterCommand(string name, Action<string[]> handler)
        {
            if (!_commands.ContainsKey(name))
            {
                _commands[name] = handler;
            }
        }

        public bool ExecuteCommand(string command, string[] args)
        {
            if (_commands.ContainsKey(command))
            {
                try
                {
                    _commands[command].Invoke(args);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Command '{command}' failed: {ex.Message}");
                }
            }
            return false;
        }

        public List<string> GetAvailableCommands()
        {
            return new List<string>(_commands.Keys);
        }
    }
}
