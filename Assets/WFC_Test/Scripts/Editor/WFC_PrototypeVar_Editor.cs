using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WFC_PrototypeVar_Editor
{
    [MenuItem("Assets/Create Prototype Variations", true)]
    private static bool IsBasePrototype()
    {
        return Selection.activeObject is BasePrototype;
    }

    [MenuItem("Assets/Create Prototype Variations")]
    private static void CreatePrototypeVariations()
    {
        //Debug.Log("Creating Variations");
        //BasePrototype basePrototype = (BasePrototype) Selection.activeObject;
        //char[] orientations = { 'n', 'e', 's', 'w' };
        //string asset_path = "Assets/WFC_Test/prototypes";
        //foreach(char c in orientations)
        //{
        //    Prototype prot = ScriptableObject.CreateInstance<Prototype>();
        //    prot.name = basePrototype.name + "_" + c;
        //    prot.basePrototype = basePrototype;
        //    prot.orientation = c;
        //    AssetDatabase.CreateAsset(prot, asset_path + "/" + prot.name + ".asset");
        //}

    }
}
