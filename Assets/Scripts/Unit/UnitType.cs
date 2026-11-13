using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitType", menuName = "TBS/UnitType")]
public class UnitType : ScriptableObject
{
    [Header("Basic Properties")]
    [SerializeField] private int Id;
    [field:SerializeField]
    public string Name { get; private set; }
    [field:SerializeField] public Transform Prefab { get; private set; }
    [field:SerializeField, Min(1)] public int MovementPoints { get; private set; } = 2;
    [field:SerializeField, Min(0)] public int SightRadius { get; private set; } = 2;
}
