using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiddenState : BaseCellState
{
    public override CellState State => CellState.Hidden;

    public override void Enter(HexCell cell)
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

    public override void Exit(HexCell cell)
    {
        if (cell.Terrain == null)
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
