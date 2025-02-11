using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WFC))]
public class WFC_Editor : Editor
{
    private WFC wfc;

    private void OnEnable()
    {
        wfc = (WFC)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Generate"))
        {
            wfc.Generate();
        }
        if(GUILayout.Button("Delete Solution"))
        {
            wfc.DeleteSolution();
        }
        EditorGUILayout.Separator();
        GUILayout.Label("Individual Steps");
        if(GUILayout.Button("Init"))
        {
            wfc.Init();
        }
        if(GUILayout.Button("Generate Rivers"))
        {
            wfc.GenerateRivers();
        }
        
        
        
        
        if(GUILayout.Button("Solve first Layer"))
        {
            wfc.SolveFirstLayer();
        }
        if(GUILayout.Button("Generate Houses"))
        {
            wfc.GenerateHouses();
        }
        if(GUILayout.Button("Place Socket Meshes"))
        {
            wfc.PlaceSocketMeshes();
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Collapse Tile"))
        {
            wfc.CollapseTile();
        }
    }
}
