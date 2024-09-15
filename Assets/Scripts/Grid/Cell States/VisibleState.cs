using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibleState : BaseCellState
{
    public override CellState State => CellState.Visible;
    
    public override void Enter(HexCell cell)
    {
        // Debug.Log("cell "+ cell.AxialCoordinates + " is entering Visible State");


        if (cell.Terrain != null && !cell.Terrain.gameObject.activeSelf)
        {
            Debug.Log("cell terrain object actif self : " + (cell.Terrain.gameObject));
            cell.Terrain.gameObject.SetActive(true);
        }
        
    }

    public override void Exit(HexCell cell)
    {
        // Debug.Log("cell "+ cell.AxialCoordinates + "is exiting Visible State");
    }

    public override ICellState OnMouseEnter()
    {
        return new HighlightedState();
    }
}
