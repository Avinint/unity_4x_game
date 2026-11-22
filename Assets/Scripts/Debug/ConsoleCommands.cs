using System;
using System.Collections.Generic;
using System.Linq;

// Commandes de la console de débogage : nom (anglais, sans casse) → aide et action.
// L'action reçoit les arguments et renvoie le texte à afficher.
public class ConsoleCommands
{
    private readonly SortedDictionary<string, (string help, Func<string[], string> run)> commands =
        new SortedDictionary<string, (string, Func<string[], string>)>(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, string help, Func<string[], string> run) => commands[name] = (help, run);

    // « Moves  ALL » → ("moves", ["ALL"]) ; ligne vide → ("", []).
    public static (string name, string[] args) Parse(string line)
    {
        var words = (line ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0 ? ("", new string[0]) : (words[0].ToLowerInvariant(), words.Skip(1).ToArray());
    }

    public string Run(string line)
    {
        var (name, args) = Parse(line);
        if (name == "") return "";
        if (!commands.TryGetValue(name, out var command)) return $"Unknown command: {name} (type help)";
        try { return command.run(args); }
        catch (Exception e) { return $"Error: {e.Message}"; }
    }

    public IEnumerable<string> HelpLines() => commands.Select(c => $"{c.Key} — {c.Value.help}");
}
