using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WFC_Tile))]
public class WFC_Tile_Editor : Editor
{
    private WFC_Tile _tile;
    static bool _showPrototypes = true;
    static bool _showTypes = true;

    public void OnEnable()
    {
        _tile = (WFC_Tile)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.LabelField("Position : " + _tile.position.x + ", " + _tile.position.y + ", " + _tile.position.z);
        EditorGUILayout.LabelField("River Direction: " + _tile.riverDirection.x + ", " + _tile.riverDirection.y);
        EditorGUILayout.LabelField("Is Collapsed: " + _tile.isCollapsed);
        EditorGUILayout.LabelField("House ID: " + _tile.houseID);
        EditorGUILayout.LabelField("Height: " + _tile.height);
        _showPrototypes = EditorGUILayout.BeginFoldoutHeaderGroup(_showPrototypes, "Possible Prototypes");
        if(_showPrototypes)
        {
            foreach(string prototype in _tile.possibleTiles)
            {
                EditorGUILayout.LabelField(prototype);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        _showTypes = EditorGUILayout.BeginFoldoutHeaderGroup(_showTypes, "Types");
        if(_showTypes)
            foreach(string type in _tile.types)
                EditorGUILayout.LabelField(type);
        EditorGUILayout.EndFoldoutHeaderGroup();

    }
}
