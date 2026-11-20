using NUnit.Framework;
using UnityEngine;

public class MapShapeTests
{
    [Test]
    public void Bump_OneAtCentre_ZeroBeyondRadius()
    {
        var centre = new Vector2(10f, 10f); var radius = new Vector2(5f, 4f);
        Assert.AreEqual(1f, MapShape.Bump(10f, 10f, centre, radius), 1e-4f);
        Assert.AreEqual(0f, MapShape.Bump(16f, 10f, centre, radius), 1e-4f);
        Assert.AreEqual(0f, MapShape.Bump(10f, 15f, centre, radius), 1e-4f);
    }

    [Test]
    public void Bump_Wrap_CloseAcrossSeam()
    {
        var centre = new Vector2(0f, 5f); var radius = new Vector2(5f, 5f);
        Assert.AreEqual(0f, MapShape.Bump(39f, 5f, centre, radius), 1e-4f);              // bords fixes : 39 cases
        Assert.Greater(MapShape.Bump(39f, 5f, centre, radius, 40f), 0.8f);               // à travers la couture : 1 case
    }

    [Test]
    public void Mask_Fractal_IsNull() =>
        Assert.IsNull(MapShape.Mask(MapType.Fractal, 40, 29, 2, false));

    [Test]
    public void Mask_Pangaea_HighInCentre_ZeroAtEdges([Values(false, true)] bool wrap)
    {
        var mask = MapShape.Mask(MapType.Pangaea, 40, 29, 2, wrap);
        Assert.Greater(mask[20, 14], 0.95f);
        Assert.AreEqual(0f, mask[0, 14], 1e-4f);     // ouest, et la couture en monde qui boucle
        Assert.AreEqual(0f, mask[20, 2], 1e-4f);     // sud, au bord de la banquise
    }
}
