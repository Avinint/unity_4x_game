using NUnit.Framework;

public class GameSettingsTests
{
    [Test]
    public void Dimensions_SmallIsCurrentMap() =>
        Assert.AreEqual((40, 25), GameSettings.Dimensions(MapSize.Small, MapType.Continents));

    [Test]
    public void Dimensions_PangaeaSquare_SameCellsRoughly(
        [Values(MapSize.Small, MapSize.Medium, MapSize.Large)] MapSize size)
    {
        var (w, h) = GameSettings.Dimensions(size, MapType.Continents);
        var (sw, sh) = GameSettings.Dimensions(size, MapType.Pangaea);

        Assert.That(sw * sh, Is.InRange(w * h * 0.95f, w * h * 1.05f));    // ± 5 % de cases
        Assert.That(sw * 1.73f / (sh * 1.5f), Is.InRange(0.9f, 1.1f));    // presque carrée à l'écran
    }

    [Test]
    public void Dimensions_GrowWithSize()
    {
        var (sw, sh) = GameSettings.Dimensions(MapSize.Small, MapType.Fractal);
        var (mw, mh) = GameSettings.Dimensions(MapSize.Medium, MapType.Fractal);
        var (lw, lh) = GameSettings.Dimensions(MapSize.Large, MapType.Fractal);
        Assert.Less(sw * sh, mw * mh);
        Assert.Less(mw * mh, lw * lh);
    }
}
