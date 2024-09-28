using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiddenState : BaseTileState
{
    public override TileState State => TileState.Hidden;

    public override void Enter(HexTile tile)
    {
        //Debug.Log("Entering Hidden State");
        // if (cell.Terrain == null)
        // {
        //     Debug.LogWarning("Terrain is null");
        //     return;
        // }
        // cell.Terrain.gameObject.SetActive(false);
        //
        // if (cell.UnknownPrefab == null)
        // {
        //     Debug.LogWarning("Unknown terrain is null");
        //     return;
        // }
        // cell.UnknownPrefab.gameObject.SetActive(true);
    }

    public override void Exit(HexTile tile)
    {
        if (tile.Terrain == null)
        {
            Debug.LogWarning("Terrain is null");
            return;
        }
        // cell.Terrain.gameObject.SetActive(true);

        // if (cell.UnknownPrefab == null)
        {
            // Debug.LogWarning("Unknown terrain is null");
            // return;
        }
        // cell.UnknownPrefab.gameObject.SetActive(false);
    }

}
