using NUnit.Framework;

public class GameSettingsStoreTests
{
    [Test]
    public void Defaults_WrapWorldOn() => Assert.IsTrue(new GameSettings().WrapWorld);

    [Test]
    public void RoundTrip_WithSeed()
    {
        var s = new GameSettings { Size = MapSize.Large, MapType = MapType.Archipelago, WrapWorld = false, PlayerCount = 3, Seed = 12345 };

        var back = GameSettingsStore.FromJson(GameSettingsStore.ToJson(s));

        Assert.AreEqual(MapSize.Large, back.Size);
        Assert.AreEqual(MapType.Archipelago, back.MapType);
        Assert.IsFalse(back.WrapWorld);
        Assert.AreEqual(3, back.PlayerCount);
        Assert.AreEqual(12345, back.Seed);
    }

    [Test]
    public void RoundTrip_WithoutSeed_StaysRandom() =>
        Assert.IsNull(GameSettingsStore.FromJson(GameSettingsStore.ToJson(new GameSettings())).Seed);

    [Test]
    public void EmptyOrBroken_GivesDefaults([Values("", "   ", "{ pas du json")] string json)
    {
        var s = GameSettingsStore.FromJson(json);

        Assert.AreEqual(MapSize.Small, s.Size);
        Assert.AreEqual(MapType.Fractal, s.MapType);
        Assert.IsTrue(s.WrapWorld);
        Assert.AreEqual(GameSettings.MaxPlayers, s.PlayerCount);
    }

    [Test]
    public void OutOfRange_Clamped()
    {
        var s = GameSettingsStore.FromJson("{\"Size\":9,\"MapType\":-1,\"WrapWorld\":true,\"PlayerCount\":12,\"HasSeed\":false,\"Seed\":0}");

        Assert.AreEqual(MapSize.Small, s.Size);
        Assert.AreEqual(MapType.Fractal, s.MapType);
        Assert.AreEqual(GameSettings.MaxPlayers, s.PlayerCount);
    }
}
