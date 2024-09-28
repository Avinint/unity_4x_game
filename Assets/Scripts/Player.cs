using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public HexTile StartingTile;
    public Dictionary<Vector2Int, HexTile> StartingArea = new Dictionary<Vector2Int, HexTile>();
    public GameObject Leader;
    public GameObject Civilisation;

    public List<GameObject> StartingUnits { get; set; } = new List<GameObject>();

    private void OnEnable()
    {
       
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
