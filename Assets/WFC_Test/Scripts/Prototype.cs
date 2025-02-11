using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "prototype", menuName = "WFC/Prototype")]
public class Prototype : ScriptableObject
{
    public Mesh mesh;
    public BasePrototype basePrototype;
    public int rotation;
    public PrototypesList[] prototypes;
    public AdjacentTypesList[] typeAdjacency;

    public void SetPrototypes(List<Prototype>[] prototypes, string[] directions)
    {
        this.prototypes = new PrototypesList[prototypes.Length];
        for (int i = 0; i < prototypes.Length; i++)
        {
            this.prototypes[i] = new PrototypesList();
            this.prototypes[i].direction = directions[i];
            this.prototypes[i].prototypes = prototypes[i].ToArray();
        }
    }
}

[Serializable]
public class PrototypesList
{
    public string direction;
    public Prototype[] prototypes;
}

[System.Serializable]
public class AdjacentTypesList
{
    public Vector3Int direction;
    public string[] types;
}