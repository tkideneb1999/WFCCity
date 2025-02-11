using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WFC_PrototypeImporter : EditorWindow
{
    public TextAsset prototype_data;
    public string prototypes_path;
    public string basePrototypes_path;
    public string meshes_path;
    public TextAsset socket_data;
    public string receptacles_path;
    public string socketMeshes_path;

    [MenuItem("Window/WFC Prototype Importer")]
    static void Init()
    {
        WFC_PrototypeImporter window = (WFC_PrototypeImporter) EditorWindow.GetWindow(typeof(WFC_PrototypeImporter));
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Import", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Prototype Json File");
        prototype_data = (TextAsset) EditorGUILayout.ObjectField(prototype_data, typeof(TextAsset), true);
        EditorGUILayout.LabelField("Prototypes Dir");
        prototypes_path = (string)EditorGUILayout.TextField(prototypes_path);
        EditorGUILayout.LabelField("Base Prototypes Dir");
        basePrototypes_path = (string)EditorGUILayout.TextField(basePrototypes_path);
        EditorGUILayout.LabelField("Mesh Directory");
        meshes_path = (string)EditorGUILayout.TextField(meshes_path);
        if(GUILayout.Button("Generate Prototypes"))
        {
            if(prototype_data == null)
            {
                ShowNotification(new GUIContent("No Text Asset Selected"));
            }
            else if(!AssetDatabase.IsValidFolder(prototypes_path))
            {
                ShowNotification(new GUIContent("No Valid Prototype Folder"));
            }
            else if(!AssetDatabase.IsValidFolder(basePrototypes_path))
            {
                ShowNotification(new GUIContent("No Valid Base Prototype Folder"));
            }
            else if(!AssetDatabase.IsValidFolder(meshes_path))
            {
                ShowNotification(new GUIContent("No Valid Meshes Folder"));
            }
            else
            {
                GeneratePrototypes();
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Socket Data");
        socket_data = (TextAsset) EditorGUILayout.ObjectField(socket_data, typeof(TextAsset), true);
        EditorGUILayout.LabelField("Socket Meshes Directory");
        socketMeshes_path = (string)EditorGUILayout.TextField(socketMeshes_path);
        EditorGUILayout.LabelField("Receptacle Objects Directory");
        receptacles_path = (string)EditorGUILayout.TextField(receptacles_path);
        if(GUILayout.Button("Setup Sockets"))
        {
            if (!AssetDatabase.IsValidFolder(basePrototypes_path))
                ShowNotification(new GUIContent("No Valid Base Prototype Folder"));
            else if (socket_data == null)
                ShowNotification(new GUIContent("No Socekt Data Selected"));
            else if (!AssetDatabase.IsValidFolder(socketMeshes_path))
                ShowNotification(new GUIContent("No valid socket Meshes Folder"));
            else
                SetupSockets();
        }
    }


    private void GeneratePrototypes()
    {
        PrototypeListJson prototypesJson = JsonUtility.FromJson<PrototypeListJson>(prototype_data.text);

        //Setup Base Prototypes
        foreach(BasePrototypeJson bp in prototypesJson.basePrototypes)
        {
            BasePrototype basePrototype = ScriptableObject.CreateInstance<BasePrototype>();
            basePrototype.name = bp.name;
            basePrototype.type = bp.type;
            basePrototype.mesh = AssetDatabase.LoadAssetAtPath<GameObject>(meshes_path + "/" + bp.name + "_var0.fbx");
            AssetDatabase.CreateAsset(basePrototype, basePrototypes_path + "/" + bp.name + ".asset");
        }

        //Setup Initial Prototypes
        foreach(PrototypeJson p in prototypesJson.prototypes)
        {
            Mesh mesh = null;
            Prototype prototype = ScriptableObject.CreateInstance<Prototype>();
            prototype.basePrototype = AssetDatabase.LoadAssetAtPath<BasePrototype>(basePrototypes_path + "/" + p.basePrototype + ".asset");
            if (!(p.name == "nullPrototype"))
            {
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshes_path + "/" + p.mesh + ".fbx");
            }
            
            prototype.name = p.name;
            prototype.mesh = mesh;
            prototype.rotation = p.rotation;

            prototype.typeAdjacency = new AdjacentTypesList[6];
            //Unity Xp
            prototype.typeAdjacency[0] = new AdjacentTypesList();
            prototype.typeAdjacency[0].direction = new Vector3Int(1, 0, 0);
            prototype.typeAdjacency[0].types = p.sockets.Xn.adjacentTypes;
            //Unity Xn
            prototype.typeAdjacency[1] = new AdjacentTypesList();
            prototype.typeAdjacency[1].direction = new Vector3Int(-1, 0, 0);
            prototype.typeAdjacency[1].types = p.sockets.Xp.adjacentTypes;
            //Unity Yp
            prototype.typeAdjacency[2] = new AdjacentTypesList();
            prototype.typeAdjacency[2].direction = new Vector3Int(0, 1, 0);
            prototype.typeAdjacency[2].types = p.sockets.Zp.adjacentTypes;
            //Unity Yn
            prototype.typeAdjacency[3] = new AdjacentTypesList();
            prototype.typeAdjacency[3].direction = new Vector3Int(0, -1, 0);
            prototype.typeAdjacency[3].types = p.sockets.Zn.adjacentTypes;
            //Unity Zp
            prototype.typeAdjacency[4] = new AdjacentTypesList();
            prototype.typeAdjacency[4].direction = new Vector3Int(0, 0, 1);
            prototype.typeAdjacency[4].types = p.sockets.Yn.adjacentTypes;
            //Unity Zn
            prototype.typeAdjacency[5] = new AdjacentTypesList();
            prototype.typeAdjacency[5].direction = new Vector3Int(0, 0, -1);
            prototype.typeAdjacency[5].types = p.sockets.Yp.adjacentTypes;

            if (p.level == "")
                AssetDatabase.CreateAsset(prototype, prototypes_path + "/" + prototype.name + ".asset");
            else
            {
                if (!AssetDatabase.IsValidFolder(prototypes_path + "/" + p.level))
                    AssetDatabase.CreateFolder(prototypes_path, p.level);
                AssetDatabase.CreateAsset(prototype, prototypes_path + "/" + p.level + "/" + prototype.name + ".asset");
            }
        }

        //Wrangle Possible Adjacent Prototypes
        foreach(PrototypeJson p in prototypesJson.prototypes)
        {
            List<Prototype>[] prototypes = new List<Prototype>[6];
            for(int i = 0; i < 6; i++)
            {
                prototypes[i] = new List<Prototype>();
            }
            
            foreach(PrototypeJson q in prototypesJson.prototypes)
            {
                //Blender Xn -> Xp Unity
                //Blender Xp -> Xn Unity
                //Blender Yn -> Zp Unity
                //Blender Yp -> Zn Unity
                //Blender Zn -> Yn Unity
                //Blender Zp -> Yp Unity
                Prototype matchingPrototype = null;

                string[] h_sockets = {
                    p.sockets.Xn.socket,
                    p.sockets.Xp.socket,
                    p.sockets.Yn.socket,
                    p.sockets.Yp.socket
                };
                string[] h_adjSockets = new string[4];
                for( int i = 0; i < 4; i++ )
                {
                    if ( h_sockets[i] == "-1" )
                    {
                        h_adjSockets[i] = "-1";
                        continue;
                    }
                    char modifier = h_sockets[i][h_sockets[i].Length - 1];
                    if (modifier == 's')
                        h_adjSockets[i] = h_sockets[i];
                    else if (modifier == 'f')
                        h_adjSockets[i] = h_sockets[i].Remove(h_sockets[i].Length - 1, 1);
                    else
                        h_adjSockets[i] = h_sockets[i] + "f";
                }
                Debug.Log("Match Prototype Path: " + prototypes_path + "/" + (q.level == "" ? "" : (q.level + "/")) + q.name + ".asset");
                if (h_adjSockets[0] == q.sockets.Xp.socket) //Xp Socket in UnityPrototype
                {
                    matchingPrototype = AssetDatabase.LoadAssetAtPath<Prototype>(prototypes_path + "/" + (q.level == "" ? "" : (q.level + "/")) + q.name + ".asset");
                    prototypes[0].Add(matchingPrototype);
                }
                if(h_adjSockets[1] == q.sockets.Xn.socket) //Xn Socket in UnityPrototype
                {
                    if(matchingPrototype == null)
                    {
                        matchingPrototype = AssetDatabase.LoadAssetAtPath<Prototype>(prototypes_path + "/" + (q.level == "" ? "" : (q.level + "/")) + q.name + ".asset");
                    }
                    prototypes[1].Add(matchingPrototype);
                }
                if(h_adjSockets[2] == q.sockets.Yp.socket) //Zp Socket in UnityPrototype --> Works
                {
                    if (matchingPrototype == null)
                    {
                        matchingPrototype = AssetDatabase.LoadAssetAtPath<Prototype>(prototypes_path + "/" + (q.level == "" ? "" : (q.level + "/")) + q.name + ".asset");
                    }
                    prototypes[4].Add(matchingPrototype);
                }
                if (h_adjSockets[3] == q.sockets.Yn.socket) //Zn Socket in UnityPrototype --> Works
                {
                    if (matchingPrototype == null)
                    {
                        matchingPrototype = AssetDatabase.LoadAssetAtPath<Prototype>(prototypes_path + "/" + (q.level == "" ? "" : (q.level + "/")) + q.name + ".asset");
                    }
                    prototypes[5].Add(matchingPrototype);
                }
                if (p.sockets.Zn.socket == q.sockets.Zp.socket) //Yn Socket in UnityPrototype
                {
                    if (matchingPrototype == null)
                    {
                        matchingPrototype = AssetDatabase.LoadAssetAtPath<Prototype>(prototypes_path + "/" + (q.level == "" ? "" : (q.level + "/")) + q.name + ".asset");
                    }
                    prototypes[3].Add(matchingPrototype);
                }
                if (p.sockets.Zp.socket == q.sockets.Zn.socket) //Yp Socket in UnityPrototype
                {
                    if (matchingPrototype == null)
                    {
                        matchingPrototype = AssetDatabase.LoadAssetAtPath<Prototype>(prototypes_path + "/" + (q.level == "" ? "" : (q.level + "/")) + q.name + ".asset");
                    }
                    prototypes[2].Add(matchingPrototype);
                }
            }
            Prototype prototype = AssetDatabase.LoadAssetAtPath<Prototype>(prototypes_path + "/" + (p.level == "" ? "" : (p.level + "/")) + p.name + ".asset");
            string[] directions = { "Xp", "Xn", "Yp", "Yn", "Zp", "Zn" };
            prototype.SetPrototypes(prototypes, directions);
            EditorUtility.SetDirty(prototype);
            AssetDatabase.SaveAssetIfDirty(prototype);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void SetupSockets()
    {
        SocketCompatListJson socketCompatListJson = JsonUtility.FromJson<SocketCompatListJson>(socket_data.text);
        foreach(string receptacle in socketCompatListJson.socketList)
        {
            Receptacle rso = ScriptableObject.CreateInstance<Receptacle>();
            rso.name = receptacle;
            rso.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(socketMeshes_path + "/" + receptacle + ".fbx");
            AssetDatabase.CreateAsset(rso, receptacles_path + "/" + receptacle + ".asset");
        }

        foreach(SocketCompat sC in socketCompatListJson.socketCompat)
        {
            if (sC.sockets.Length == 0) continue;
            BasePrototype bp = AssetDatabase.LoadAssetAtPath<BasePrototype>(basePrototypes_path + "/" + sC.basePrototype + ".asset");
            bp.sockets = new Socket[sC.sockets.Length];
            for(int i=0; i< bp.sockets.Length; i++)
            {
                Socket socket = new Socket();
                socket.name = sC.sockets[i].name;
                socket.receptacles = new Receptacle[sC.sockets[i].socketMeshes.Length];
                for ( int j=0; j< sC.sockets[i].socketMeshes.Length; j++)
                {
                    socket.receptacles[j] = AssetDatabase.LoadAssetAtPath<Receptacle>(receptacles_path + "/" + sC.sockets[i].socketMeshes[j] + ".asset");
                }
                bp.sockets[i] = socket;
            }
            EditorUtility.SetDirty(bp);
            AssetDatabase.SaveAssetIfDirty(bp);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

[System.Serializable]
class PrototypeListJson
{
    public PrototypeJson[] prototypes;
    public BasePrototypeJson[] basePrototypes;
}

[System.Serializable]
class PrototypeJson
{
    public string name;
    public string level;
    public string basePrototype;
    public string mesh;
    public int rotation;
    public SocketsJson sockets;
}

[System.Serializable]
class SocketsJson
{
    public SocketJson Xp;
    public SocketJson Xn;
    public SocketJson Yp;
    public SocketJson Yn;
    public SocketJson Zp;
    public SocketJson Zn;
}

[System.Serializable]
class SocketJson
{
    public string socket;
    public string[] adjacentTypes;
}

[System.Serializable]
class BasePrototypeJson
{
    public string name;
    public string type;
}

[System.Serializable]
class SocketCompatListJson
{
    public string[] socketList; 
    public SocketCompat[] socketCompat;
}

[System.Serializable]
class SocketCompat
{
    public string basePrototype;
    public MeshSocket[] sockets;
}

[System.Serializable]
class MeshSocket
{
    public string name;
    public string[] socketMeshes;
}
