using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HexTileGizmo : MonoBehaviour
{
    public List<HexTile> Neighbours { get; set; }
    public HexGrid Grid;
    private bool unlocked = false;
    
    private void Awake()
    {
        if (Grid == null)
            Grid = GameObject.FindFirstObjectByType<HexGrid>();
        if (Grid == null)
            Debug.LogError("No HexGrid component found");
    }
    
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {

        Grid.OnTileInstancesGenerated += Unlock;
    }

    public void Unlock()
    {
        unlocked = true;
    }

    public void OnDrawGizmosSelected()
    {
        if (unlocked)
            foreach (HexTile neighbour in Neighbours)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(transform.position, 0.5f);
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, neighbour.CentrePosition);
            }
    }
}
