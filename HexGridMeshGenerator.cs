using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridMeshGenerator : MonoBehaviour
{
    [field:SerializeField] public LayerMask gridLayer { get; private set; }
    [field:SerializeField] public HexGrid hexGrid { get; private set; }
    
    
    private void Awake()
    {
        if (hexGrid == null)
            hexGrid = GetComponentInParent<HexGrid>();
        if (hexGrid == null)
            Debug.LogError("No HexGrid component found");
    }

    public void CreateHexMesh()
    {
        CreateHexMesh(hexGrid.Width,  hexGrid.Height, hexGrid.HexSize, hexGrid.Orientation, gridLayer);
    }

    public void CreateHexMesh(HexGrid hexGrid, LayerMask layerMask)
    {
        this.hexGrid = hexGrid;
        this.gridLayer = layerMask;
        CreateHexMesh(hexGrid.Width,  hexGrid.Height, hexGrid.HexSize, hexGrid.Orientation, gridLayer);
    }

    public void CreateHexMesh(int width, int height, float hexSize, HexOrientation orientation,LayerMask layerMask)
    {
        ClearHexGridMesh();
        Vector3[] vertices = new Vector3[7 * width * height];
        for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                Vector3 centrePosition = HexMetrics.Center(hexSize, x, z, orientation);
                vertices[(z * width + x) * 7] = centrePosition;
                for (int s = 0; s < )
            }
    }

    public void ClearHexGridMesh()
    {
        if (GetComponent <MeshFilter>().sharedMesh == null) return;
        GetComponent <MeshFilter>().sharedMesh.Clear();
        GetComponent<MeshCollider>().sharedMesh.Clear();
    }
}
