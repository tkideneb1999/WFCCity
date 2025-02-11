using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Probabalistic", menuName = "WFC/Socket Placement/Probabalistic Placement")]
public class SocketPlacementProbabalistic : SocketPlacementBase
{
    public bool useProbability = false;
    public int minSockets;
    public int maxSockets;
    [Range(0.0f, 1.0f)] public float probability;
    public override int GeneratePlacement(float[] probabilities)
    {
        if (Random.Range(0f, 1f) > probability)
            return -1;
        float max = 0f;
        Dictionary<int, float> probDict = new Dictionary<int, float>();
        for (int i = 0; i < probabilities.Length; i++)
        {
            max += probabilities[i];
            probDict.Add(i, probabilities[i]);
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
