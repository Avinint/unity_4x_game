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

    public List<GameObject> units { get; set; } = new List<GameObject>();

    [field: SerializeField] public Color Color { get; set; } = Color.white;

    private Material playerColorMaterial;

    // Matériau uni à la couleur du joueur (socles, bannières, frontières…), créé une fois par joueur.
    public Material PlayerColorMaterial
    {
        get
        {
            if (playerColorMaterial == null)
            {
                playerColorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                playerColorMaterial.SetColor("_BaseColor", Color);
            }
            return playerColorMaterial;
        }
    }

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
