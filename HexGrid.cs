using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
   [field:SerializeField] public int Width { get; private set; }
   [field:SerializeField] public int Height { get; private set; }
   [field:SerializeField] public float HexSize { get; private set; }


   [field:SerializeField] public HexOrientation Orientation { get; private set; }
   [field:SerializeField] public GameObject HexPrefab { get; private set; }


   private void OnDrawGizmos()
   {
      for (int z = 0; z < Height; z++)
         for (int x = 0; x < Width; x++)
         {
            Vector3 centrePosition = HexMetrics.Center(HexSize, x, z , Orientation) + transform.position;
            Vector3[] corners = HexMetrics.Corners(HexSize, Orientation);
            for (int s = 0; s < 6; s++)
            {
              Gizmos.DrawLine(
                 centrePosition + corners[ s ],
                 centrePosition + corners[(s + 1) % 6]
                 );
               
            }
         }
   }
}

public enum HexOrientation
{
   FlatTop,
   PointyTop
}
