using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HighlightedState : BaseTileState
{
    public override TileState State => TileState.Highlighted;

    public override void Enter(HexTile tile)
    {
        LeanTween.scale(tile.Terrain.gameObject, Vector3.one * 1.2f, .2f).setEase(LeanTweenType.easeOutBack);
        LeanTween.moveY(tile.Terrain.gameObject, 5f, .2f).setEase(LeanTweenType.easeOutBack);
    }

    public override void Exit(HexTile tile)
    {
        LeanTween.scale(tile.Terrain.gameObject, Vector3.one, .2f).setEase(LeanTweenType.easeOutBack);
        LeanTween.moveY(tile.Terrain.gameObject, 0f, .2f).setEase(LeanTweenType.easeOutBack);
    }

    public override ITileState OnMouseExit()
    {
        return new VisibleState();
    }

    public override ITileState OnSelect()
    {
        return new SelectedState();
    }
}
