using NUnit.Framework;

public class UserOptionsStoreTests
{
    [Test]
    public void RoundTrip()
    {
        var o = new UserOptions { Language = "en", Fullscreen = false, EdgeScroll = false, InvertWheel = true };

        var back = UserOptionsStore.FromJson(UserOptionsStore.ToJson(o));

        Assert.AreEqual("en", back.Language);
        Assert.IsFalse(back.Fullscreen);
        Assert.IsFalse(back.EdgeScroll);
        Assert.IsTrue(back.InvertWheel);
    }

    [Test]
    public void EmptyOrBroken_GivesDefaults([Values("", "  ", "{ pas du json")] string json)
    {
        var o = UserOptionsStore.FromJson(json);

        Assert.AreEqual("", o.Language);
        Assert.IsTrue(o.Fullscreen);
        Assert.IsTrue(o.EdgeScroll);
        Assert.IsFalse(o.InvertWheel);
    }

    [Test]
    public void MissingField_KeepsDefault()
    {
        var o = UserOptionsStore.FromJson("{\"Language\":\"fr\"}");

        Assert.AreEqual("fr", o.Language);
        Assert.IsTrue(o.EdgeScroll);
    }
}
