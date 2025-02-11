using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WFC_Tile : MonoBehaviour
{
    private Vector3Int _position;
    private Vector2 _riverDirection;
    private List<string> _possibleTiles;
    private string[] _types;
    private int _houseID;
    private bool _isCollapsed;
    private int _height;

    public GameObject debugCube;

    public Vector3Int position
    {
        get { return _position; }
        set { _position = value; }
    }
    public Vector2 riverDirection
    {
        get { return _riverDirection; }
        set { _riverDirection = value; }
    }
    public List<string> possibleTiles
    {
        get { return _possibleTiles; }
        set
        {
            _possibleTiles = value;
        }
    }
    public string[] types
    {
        get { return _types; }
        set
        {
            _types = value;
        }
    }
    public int houseID
    {
        get { return _houseID; }
        set {_houseID = value; }
    }

    public int height
    {
        get { return _height; }
        set { _height = value; }
    }
    public bool isCollapsed
    {
        get { return _isCollapsed; }
        set { _isCollapsed = value; }
    }

    public void SetDebugCubeMaterial(Material mat)
    {
        if (debugCube == null)
            return;
        debugCube.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    public void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + new Vector3(0, 2, 0);
        Vector3 direction = new Vector3(riverDirection.x, 0, riverDirection.y);
        Gizmos.DrawRay(origin, direction);
        Gizmos.DrawRay(origin + direction, Quaternion.AngleAxis(-45, Vector3.up) * (-direction * 0.5f));
        Gizmos.DrawRay(origin + direction, Quaternion.AngleAxis(45, Vector3.up) * (-direction * 0.5f));
    }

    public void EnableDebugCube()
    {
        debugCube.SetActive(true);
    }
}
