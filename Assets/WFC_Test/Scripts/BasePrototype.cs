using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "basePrototype", menuName = "WFC/Base Prototype")]
public class BasePrototype : ScriptableObject
{
    [Min(0.0f)] public float probability = 1.0f;
    public GameObject mesh;
    public string type;
    public Socket[] sockets;
}

public enum Rotatable
{
    NoRotation,
    RotateBy90,
    FreeRotation
}

[System.Serializable]
public class Socket
{
    public string name;
    public SocketPlacementBase placementRule;
    public Rotatable rotationType;
    [Tooltip("X is Minimum Z Rotation & Y is Maximum")]public Vector2 rotation;
    public bool movable;
    public Vector2 maxMoveAmount = new Vector2(0.0f, 0.0f);
    public Vector2Int adjTileOffset;
    public Receptacle[] receptacles;

    public int GeneratePlacement()
    {
        if (placementRule == null)
            return -1;
        float[] probabilities = new float[receptacles.Length];
        for(int i=0; i<receptacles.Length; i++)
        {
            probabilities[i] = receptacles[i].probability;
        }
        return placementRule.GeneratePlacement(probabilities);
    }

    public int GeneratePlacement(Tile socketTile, ref Tile adjacentTileInfo)
    {
        if (socketTile.houseID == adjacentTileInfo.houseID)
            return -1;

        //Choose Socket ignore probability
        Dictionary<int, float> probDict = new Dictionary<int, float>();
        float max = 0.0f;
        for (int i = 0; i < receptacles.Length; i++)
        {
            max += receptacles[i].probability;
            probDict.Add(i, receptacles[i].probability);
        }
        float random = Random.Range(0.0f, max);

        foreach (KeyValuePair<int, float> pair in probDict)
        {
            if (random < pair.Value)
                return pair.Key;
            random -= pair.Value;
        }
        return -1;
        
    }
}
