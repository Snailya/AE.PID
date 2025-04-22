using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace AE.PID.Client.VisioAddIn;

internal sealed class RibbonCommandManager
{
    private readonly ImmutableDictionary<string, IRibbonItem> _items;

    public RibbonCommandManager()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, IRibbonItem>();
        var commands = LoadRibbonCommands(builder);
        BuildContextMenuGroups(builder, commands);
        _items = builder.ToImmutable();
    }

    public IRibbonItem? this[string key] => _items.TryGetValue(key, out var item) ? item : null;

    private List<IRibbonCommand> LoadRibbonCommands(ImmutableDictionary<string, IRibbonItem>.Builder builder)
    {
        var commands = new List<IRibbonCommand>();
        var commandTypes = typeof(IRibbonCommand).Assembly
            .GetTypes()
            .Where(t => typeof(IRibbonCommand).IsAssignableFrom(t) &&
                        !t.IsAbstract &&
                        t.IsClass);

        foreach (var type in commandTypes)
        {
            if (Activator.CreateInstance(type) is not IRibbonCommand command) continue;

            if (builder.ContainsKey(command.Id))
                throw new InvalidOperationException($"Duplicate command ID: {command.Id}");

            builder.Add(command.Id, command);
            commands.Add(command);
        }

        return commands;
    }

    private static void BuildContextMenuGroups(
        ImmutableDictionary<string, IRibbonItem>.Builder builder,
        IEnumerable<IRibbonCommand> commands)
    {
        var contextMenuItems = commands
            .Select(c => new
            {
                Command = c,
                ContextMenu = c.GetType().GetCustomAttribute<RibbonContextMenu>()
            })
            .Where(x => x.ContextMenu != null);

        foreach (var group in contextMenuItems.GroupBy(x => x.ContextMenu.Id))
        {
            var firstItem = group.First();
            var commandGroup = new RibbonContextMenuGroup(
                firstItem.ContextMenu.Id,
                firstItem.ContextMenu.Label,
                group.Select(x => x.Command)
            );

            if (builder.ContainsKey(commandGroup.Id))
                throw new InvalidOperationException($"CommandGroup ID conflict: {commandGroup.Id}");

            builder.Add(commandGroup.Id, commandGroup);
        }
    }
}