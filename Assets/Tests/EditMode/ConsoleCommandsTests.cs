using System;
using System.Linq;
using NUnit.Framework;

public class ConsoleCommandsTests
{
    [Test]
    public void Parse_SplitsWords_LowercasesName()
    {
        var (name, args) = ConsoleCommands.Parse("  Moves   ALL ");
        Assert.AreEqual("moves", name);
        CollectionAssert.AreEqual(new[] { "ALL" }, args);
    }

    [Test]
    public void Parse_EmptyLine([Values("", "   ", null)] string line)
    {
        var (name, args) = ConsoleCommands.Parse(line);
        Assert.AreEqual("", name);
        Assert.IsEmpty(args);
    }

    [Test]
    public void Run_KnownCommand_GetsArguments()
    {
        var c = new ConsoleCommands();
        c.Register("echo", "repeat", a => string.Join(" ", a));
        Assert.AreEqual("a b", c.Run("ECHO a b"));
    }

    [Test]
    public void Run_UnknownCommand_SaysSo() =>
        Assert.AreEqual("Unknown command: foo (type help)", new ConsoleCommands().Run("foo"));

    [Test]
    public void Run_EmptyLine_NothingToShow() => Assert.AreEqual("", new ConsoleCommands().Run("  "));

    [Test]
    public void Run_ActionThrows_ShowsError()
    {
        var c = new ConsoleCommands();
        c.Register("boom", "fails", _ => throw new InvalidOperationException("no map"));
        Assert.AreEqual("Error: no map", c.Run("boom"));
    }

    [Test]
    public void HelpLines_Sorted()
    {
        var c = new ConsoleCommands();
        c.Register("seed", "show seed", _ => "");
        c.Register("clear", "clear", _ => "");
        CollectionAssert.AreEqual(new[] { "clear — clear", "seed — show seed" }, c.HelpLines().ToArray());
    }
}
