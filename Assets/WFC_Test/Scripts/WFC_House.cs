using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFC_House
{
    private int id;
    private int height;
    private int maxSizeY;
    private int maxUncertainty;
    private List<int[]> coords2D;
    private Material houseColor;

    public delegate ref Tile GetTile(int[] coords);
    public delegate void SetHouseColor(int[] coord, ref Material color);
    public event GetTile getTile;
    public event SetHouseColor setHouseColor;

    public WFC_House(int id, int height, int maxSizeY, int maxUncertainty, List<int[]> coords2D, GetTile getTileFunc, Material houseColor, SetHouseColor setHouseColor)
    {
        this.id = id;
        this.height = height;
        this.maxSizeY = maxSizeY;
        this.maxUncertainty = maxUncertainty;
        this.coords2D = coords2D;
        this.getTile += getTileFunc;
        this.houseColor = houseColor;
        this.setHouseColor += setHouseColor;
    }

    public bool CollapseHouse()
    {
        foreach (int[] coord in coords2D)
        {
            if (!Propagate(new int[] { coord[0], height, coord[1] }))
                return false;
        }
        while(!IsCollapsed())
        {
            int[] coord = GetMinimumUncertaintyCoords();
            getTile(coord).Collapse();
            if (!Propagate(coord))
                return false;
        }
        foreach (int[] coord in coords2D)
        {
            for (int i = 0; i <= height; i++)
                setHouseColor(new int[] { coord[0], i, coord[1] }, ref houseColor);
        }
        return true;
    }
    private bool Propagate(int[] startCoord)
    {
        Stack<int[]> coordsStack = new Stack<int[]>();
        coordsStack.Push(startCoord);
        while (coordsStack.Count > 0)
        {
            // Get Adjacent 6 Tiles
            int[] pCoords = coordsStack.Pop();
            int[][] adjCoords = new int[][] {
                new int[] { pCoords[0] + 1, pCoords[1],     pCoords[2]     }, //Xp
                new int[] { pCoords[0] - 1, pCoords[1],     pCoords[2]     }, //Xn
                new int[] { pCoords[0],     pCoords[1] + 1, pCoords[2]     }, //Yp
                new int[] { pCoords[0],     pCoords[1] - 1, pCoords[2]     }, //Yn
                new int[] { pCoords[0],     pCoords[1],     pCoords[2] + 1 }, //Zp
                new int[] { pCoords[0],     pCoords[1],     pCoords[2] - 1 }  //Zn
            };
            ref Tile currTile = ref getTile.Invoke(pCoords);
            currTile.Update();

            for (int i = 0; i < 6; i++)
            {
                int[] currAdjCoords = adjCoords[i];
                int[] currAdjCoords2D = new int[] { currAdjCoords[0], currAdjCoords[2] };

                if (currAdjCoords[1] < 0 || currAdjCoords[1] >= maxSizeY)
                    continue;
                if (!IsInCoordList(ref currAdjCoords2D))
                    continue;

                ref Tile adjTile = ref getTile.Invoke(currAdjCoords);

                if (adjTile.isCollapsed())
                    continue;

                //Get possible prototypes for adjacent tile from currTile
                HashSet<Prototype> possible_prototypes = new HashSet<Prototype>();
                foreach (KeyValuePair<Prototype, float> p in currTile.prototypes)
                {
                    possible_prototypes.UnionWith(p.Key.prototypes[i].prototypes);
                }

                //Remove any Prototypes that are not part
                HashSet<Prototype> adjTilePrototypeList = new HashSet<Prototype>(adjTile.prototypes.Keys);
                adjTilePrototypeList.ExceptWith(possible_prototypes);

                if (adjTilePrototypeList.Count == 0)
                    continue;

                foreach (Prototype p in adjTilePrototypeList)
                {
                    adjTile.prototypes.Remove(p);
                }

                //DEBUG
                // string prototypeString = "Prototypes Left: ";
                // foreach (KeyValuePair<Prototype, float> p in adjTile.prototypes)
                //     prototypeString += p.Key.name + ", ";
                // Debug.Log(prototypeString);

                // Check if there are any valid prototypes left
                if (adjTile.prototypes.Count == 0)
                {
                    Debug.LogWarning("Tile has no Prototypes Left: " + currAdjCoords[0] + ", " + currAdjCoords[1] + ", " + currAdjCoords[2], adjTile.gameObject);
                    return false;
                }

                // Add tiles that changed to stack
                coordsStack.Push(currAdjCoords);
            }
        }
        return true;
    }

    private bool IsCollapsed()
    {
        foreach (int[] houseCoords in coords2D)
            for (int i = 0; i < maxSizeY; i++)
            {
                ref Tile tile = ref getTile.Invoke(new int[] { houseCoords[0], i, houseCoords[1]});
                if (!tile.isCollapsed())
                    return false;
            }
        return true;
    }

    private int[] GetMinimumUncertaintyCoords()
    {
        List<int[]> coords = new List<int[]>();
        int minUncertainty = maxUncertainty;
        for (int i = 0; i < coords2D.Count; i++) // i == x
        {
            for (int j = 0; j < maxSizeY; j++) // j == z
            {
                int[] coord = { coords2D[i][0], j, coords2D[i][1] };
                if (getTile.Invoke(coord).isCollapsed()) continue;
                int tileUncertainty = getTile.Invoke(coord).GetUncertainty();
                if (tileUncertainty < minUncertainty)
                {
                    coords.Clear();
                    coords.Add(coord);
                }
                else if (tileUncertainty == minUncertainty)
                {
                    coords.Add(coord);
                }
            }
        }
        if (coords.Count == 1)
        {
            return coords[0];
        }
        else if (coords.Count == 0)
        {
            int random = Random.Range(0, coords2D.Count);
            return new int[] {
                coords2D[random][0],
                Random.Range(0, maxSizeY),
                coords2D[random][1]
            };
        }
        else
        {
            return coords[Random.Range(0, coords.Count)];
        }
    }

    private bool IsInCoordList(ref int[] coord)
    {
        foreach (int[] c in coords2D)
        {
            if (IsSameCoord(c, coord))
                return true;
        }
        return false;
    }

    private bool IsSameCoord(int[] a, int[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] == b[i])
                continue;
            else
                return false;
        return true;
    }
}
