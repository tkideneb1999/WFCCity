using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Always", menuName = "WFC/Socket Placement/Always Placement")]
public class SocketPlacementAlways : SocketPlacementBase
{
    public override int GeneratePlacement(float[] probabilities)
    {
        float max = 0f;
        Dictionary<int, float> probDict = new Dictionary<int, float>();
        for(int i=0; i<probabilities.Length; i++)
        {
            max += probabilities[i];
            probDict.Add(i, probabilities[i]);
        }

        float random = Random.Range(0.0f, max);

        foreach(KeyValuePair<int, float> pair in probDict)
        {
            if (random < pair.Value)
                return pair.Key;
            random -= pair.Value;
        }
        return -1;
    }
}
