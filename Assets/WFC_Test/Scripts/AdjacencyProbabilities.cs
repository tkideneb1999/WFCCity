using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "adjacencyProbablity", menuName = "WFC/Adjacency Probability")]
public class AdjacencyProbabilities : ScriptableObject
{
    public AdjacencyProbability[] overwriteProbabilities;

    public ref AdjacencyProbability[] Get()
    {
        return ref overwriteProbabilities;
    }
}

[System.Serializable]
public class AdjacencyProbability
{
    public Prototype first;
    public Prototype second;
    public float probability;
}
