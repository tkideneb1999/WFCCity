using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="receptacle", menuName ="WFC/Receptacle")]
public class Receptacle : ScriptableObject
{
    public GameObject prefab;
    public bool useHousePaint = false;
    public float probability = 1.0f;
}
