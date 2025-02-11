using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFC : MonoBehaviour
{
    [Header("General Parameters")]
    public Vector3Int size = new Vector3Int(10, 10, 10);
    public float tileSize = 2.0f;
    public GameObject baseGameObject;
    public AdjacencyProbabilities overwriteProbabilities;
    public Prototype[] groundPrototypes;
    public Prototype[] housePrototypes;

    [Header("River Parameters")]
    [Range(0, 4)]public int riverAmount = 4;
    public float middleWeight = 0.6f;
    public int riverSolverMaxIterations = 200;
    public Prototype riverBasePrototype;
    public GameObject riverMesh;
    public bool useRiverMesh = true;

    [Header("House Parameters")]
    public Vector2Int avgHouseSize = new Vector2Int(2, 3);
    public Vector2Int maxHouseSize = new Vector2Int(2, 4);
    public int remnantHouseMaxLength = 5;
    public int houseSolverMaxIterations = 20;
    public int houseFloodFillMaxIterations = 600;
    public int smallHouseMergeMaxIteration = 40;
    public Material[] houseColors;

    [SerializeField] private Tile[,,] tiles;
    private Dictionary<int, int> _houseIDHeightDict;
    private int _currentLayer = 0;
    private Vector3Int _internalSize;

    public int currentLayer
    {
        get { return _currentLayer; }
    }
    public void DeleteSolution()
    {
        int childCount = transform.childCount;
        for (int c = 0; c < childCount; c++)
        {
            GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
        }
        if (tiles != null)
            System.Array.Clear(tiles, 0, tiles.Length);

    }

    public void Generate()
    {
        FunctionTimer timer = new FunctionTimer();
        Init();
        if(!GenerateRivers())
        {
            Debug.LogWarning("RIVER GENERATION FAILED! Aborting");
            return;
        }
        SolveFirstLayer();
        if(!GenerateHouses())
        {
            Debug.LogWarning("HOUSE GENERATION FAILED! Aborting");
        }
        PlaceSocketMeshes();
        timer.StopTimer("City Generation took");
    }

    public void Init()
    {
        FunctionTimer timer = new FunctionTimer();
        Debug.Log("--- INITIALIZING WFC ALGORITHM ---");
        _currentLayer = 0;
        tiles = new Tile[size.x + 2, size.y, size.z + 2];
        _internalSize = new Vector3Int(size.x + 2, size.y, size.z + 2);
        for (int i = 0; i < _internalSize.x; i++)
        {
            for (int j = 0; j < _internalSize.y; j++)
            {
                for(int k = 0; k < _internalSize.z; k++)
                {
                    if (j == 0)
                        tiles[i, j, k] = new Tile(
                            groundPrototypes, 
                            new Vector3(i * tileSize, j * tileSize, k * tileSize), 
                            new Vector3Int(i, j, k),
                            gameObject.transform);
                    else
                        tiles[i, j, k] = new Tile(
                            housePrototypes, 
                            new Vector3(i * tileSize, j * tileSize, k * tileSize), 
                            new Vector3Int(i, j, k),
                            gameObject.transform);
                }
            }
        }
        for (int i = 0; i < _internalSize.x; i++)
        {
            
            tiles[i, 0, 0].Collapse(riverBasePrototype);
            int[] coord = { i, 0, 0 };
            Propagate(coord);
            PlaceRiverMesh(coord);

            tiles[i, 0, size.z + 1].Collapse(riverBasePrototype);
            coord = new int[] { i, 0, size.z + 1 };
            Propagate(coord);
            PlaceRiverMesh(coord);
        }

        for (int i = 1; i < _internalSize.z - 1; i++)
        {
            tiles[0, 0, i].Collapse(riverBasePrototype);
            int[] coord = { 0, 0, i };
            Propagate(coord);
            PlaceRiverMesh(coord);

            tiles[size.x + 1, 0, i].Collapse(riverBasePrototype);
            coord = new int[] { size.x + 1, 0, i };
            Propagate(coord);
            PlaceRiverMesh(coord);
        }
        timer.StopTimer("Finished Initialization in");
        Debug.Log("--- FINISHED WFC INIT ---");
    }

    private void Iterate()
    {
        if(IsCurrentLayerCollapsed())
        {
            Debug.Log("Layer Collapsed: " + _currentLayer);
            switch(_currentLayer)
            {
                case 0:
                    GenerateHouseLayout();
                    GenerateHouseHeights();
                    break;
            }
            _currentLayer++;
        }
        int[] coords = GetMinimumUncertaintyCoords();
        Collapse(coords);
        Propagate(coords);
    }

    public bool GenerateHouses()
    {
        FunctionTimer houseTimer = new FunctionTimer();
        if (!GenerateHouseLayout())
            return false;
        houseTimer.StopTimer("House Layout Generation finished in");
        FunctionTimer houseHeightsTimer = new FunctionTimer();
        if (!GenerateHouseHeights())
            return false;
        houseHeightsTimer.StopTimer("House Height Generation finished in");

        houseTimer.StopTimer("House Generation finished in");
        return true;
    }

    public void CollapseTile()
    {
        if (IsCurrentLayerCollapsed())
        {
            _currentLayer++;
        }
        int[] coords = GetMinimumUncertaintyCoords();
        Collapse(coords);
        Propagate(coords);
    }

    public void SolveFirstLayer()
    {
        if (!(currentLayer == 0))
            return;
        while (!IsCurrentLayerCollapsed())
        {
            Iterate();
        }
    }

    private int[][] GetAdjacentCollapsedCoords(int[] coords)
    {
        Vector3Int[] dirs = new Vector3Int[]
        {
            new Vector3Int( 1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 1, 0),
            new Vector3Int( 0,-1, 0),
            new Vector3Int( 0, 0, 1),
            new Vector3Int( 0, 0,-1),
        };
        List<int[]> collapsedCoords = new List<int[]>();
        foreach(Vector3Int dir in dirs)
        {
            int[] adjTile = new int[] { coords[0] + dir.x, coords[1] + dir.y, coords[2] + dir.z };
            if (!IsInBounds(ref adjTile))
                continue;
            if(tiles[adjTile[0], adjTile[1], adjTile[2]].isCollapsed())
                collapsedCoords.Add(adjTile);
        }
        return collapsedCoords.ToArray();
    }

    public bool GenerateRivers()
    {
        Debug.Log("----STARTING RIVER GENERATION----");
        FunctionTimer timer = new FunctionTimer();
        int[][] startCoords = new int[][]
        {   //             X               Y            Z
            new int[] { Random.Range(5, _internalSize.x - 5), 0,                          1 },
            new int[] { Random.Range(5, _internalSize.x - 5), 0,        _internalSize.z - 2 },
            new int[] { 1,                           0, Random.Range(5, _internalSize.z - 5)},
            new int[] { _internalSize.x - 2,         0, Random.Range(5, _internalSize.z - 5)},
        };
        int[][] startDirs = new int[][]
        {
            //          dx  dz
            new int[] { 0,  1},
            new int[] { 0, -1},
            new int[] { 1,  0},
            new int[] {-1,  0}
        };
        List<int[]> riverCoords = new List<int[]>(startCoords);
        bool res = GenerateRiverTiles(ref riverCoords, startDirs);
        //SetRiverDirections(startCoords, startDirs);
        timer.StopTimer("River Generation Finished in");
        Debug.Log("----FINISHED RIVER GENERATION----");
        return res;
    }

    private bool GenerateRiverTiles(ref List<int[]> riverCoords, int[][] startDirections)
    {
        bool res = true;
        // Choose starting points -> two tiles on edge

        List<int> colorIndices = new List<int>() { 0, 1, 2, 3};

        Stack<int[]> pathStack = new Stack<int[]>();
        for(int i = 0; i < riverAmount; i++)
        {
            pathStack.Push(new int[] {
                riverCoords[i][0],
                riverCoords[i][1],
                riverCoords[i][2],
                startDirections[i][0],
                startDirections[i][1],
                colorIndices[i]
            });
        }
        
        int iteration = 0;
        while (pathStack.Count > 0 && iteration < riverSolverMaxIterations)
        {
            iteration++;
            int[] currRiverCoord = pathStack.Pop();

            int[] newCoord = { currRiverCoord[0] + currRiverCoord[3], currRiverCoord[2] + currRiverCoord[4] };
            // Choose Direction
            float random = Random.Range(-1.0f, 1.0f);
            int dx = 0;
            int dz = 0;
            if (Mathf.Abs(random) < middleWeight)
            {
                dx = currRiverCoord[3];
                dz = currRiverCoord[4];
            }
            else
            {
                //Rotate Original Direction by 90 / -90 degree
                int d = (int) Mathf.Sign(random);
                dx = currRiverCoord[4] * d;
                dz = -currRiverCoord[3] * d;
            }
            //int[] newCoord = { currRiverCoord[0] + dx, currRiverCoord[2] + dz };
            // -- If coords out of bounds do nothing
            if (newCoord[0] >= 1 && newCoord[0] < _internalSize.x - 1)
            {
                if (newCoord[1] >= 1 && newCoord[1] < _internalSize.z - 1)
                {
                    bool exists = false;
                    foreach (int[] rc in riverCoords)
                    {
                        if (newCoord[0] == rc[0])
                            if (newCoord[1] == rc[2])
                            {
                                exists = true;
                                break;
                            }
                                    
                    }
                    if (exists) continue;
                    // Add new coord to stack
                    pathStack.Push(new int[] { newCoord[0], 0, newCoord[1], dx, dz, currRiverCoord[5] });

                    // Add adjacent to river coords
                    riverCoords.Add(new int[] { newCoord[0], 0, newCoord[1]});
                    colorIndices.Add(currRiverCoord[5]);

                }
            }
        }
        #region Dilate
        
        // Dilate Rivers to thickness of 2
        int riverCoordcount = riverCoords.Count;
        for(int i=0; i<riverCoordcount; i++)
        {
            int[] dilatedX = new int[4];
        
            if (riverCoords[i][0] + 1 >= _internalSize.x - 1)
                dilatedX[0] = riverCoords[i][0] - 1;
            else
                dilatedX[0] = riverCoords[i][0] + 1;
            dilatedX[1] = 0;
            dilatedX[2] = riverCoords[i][2];
        
            if (!IsInCoordList(ref riverCoords, ref dilatedX))
            {
                riverCoords.Add(dilatedX);
                colorIndices.Add(colorIndices[i]);
            }
        
            int[] dilatedY = new int[4];
        
            dilatedY[0] = riverCoords[i][0];
            dilatedY[1] = 0;
            if (riverCoords[i][2] + 1 >= _internalSize.z - 1)
                dilatedY[2] = riverCoords[i][2] - 1;
            else
                dilatedY[2] = riverCoords[i][2] + 1;
        
            if (!IsInCoordList(ref riverCoords, ref dilatedY))
            {
                riverCoords.Add(dilatedY);
                colorIndices.Add(colorIndices[i]);
            }
        }
        
        #endregion
        // Set River Color DEBUG
        //for ( int i=0; i<riverCoords.Count; i++)
        //{
        //    if (colorIndices[i] < debugMaterials.Length)
        //        tiles[riverCoords[i][0], riverCoords[i][1], riverCoords[i][2]].SetMaterial(debugMaterials[colorIndices[i]]);
        //}
        HashSet<Tile> tileHashSet = new HashSet<Tile>();
        foreach (int[] rc in riverCoords)
        {
            tileHashSet.Add(tiles[rc[0], rc[1], rc[2]]);
        }
        riverCoords = new List<int[]>(tileHashSet.Count);
        foreach(Tile tile in tileHashSet)
        {
            riverCoords.Add(new int[] { tile.tilePosition.x, tile.tilePosition.y, tile.tilePosition.z });
        }
        for (int i=1; i<_internalSize.x - 1; i++)
        {
            int[] coord = { i, 0, 1 };
            if(!IsInCoordList(ref riverCoords, ref coord))
            {
                riverCoords.Add(coord);
            }
            coord = new int[] { i, 0, _internalSize.z - 2 };
            if (!IsInCoordList(ref riverCoords, ref coord))
            {
                riverCoords.Add(coord);
            }
        }
        for (int i = 1; i < _internalSize.z - 1; i++)
        {
            int[] coord = { 1, 0, i };
            if (!IsInCoordList(ref riverCoords, ref coord))
            {
                riverCoords.Add(coord);
            }
            coord = new int[] { _internalSize.x - 2, 0, i };
            if (!IsInCoordList(ref riverCoords, ref coord))
            {
                riverCoords.Add(coord);
            }
        }
        // Remove according Prototypes
        for ( int i = 1; i < _internalSize.x - 1; i++ )
        {
            for(int k = 1; k < _internalSize.z - 1; k++ )
            {
                bool isRiver = false;
                int[] coords = { i, 0, k };
                foreach (int[] rc in riverCoords)
                    if (rc[0] == i)
                        if (rc[2] == k)
                        {
                            isRiver = true;
                            break;
                        }
                if (isRiver)
                    tiles[i, 0, k].KeepPrototypesByType("river");
                else
                    tiles[i, 0, k].RemovePrototypesByType("river");
            }
        }

        //Remove Prototypes by Adjacency
        Vector3Int[] dirs = new Vector3Int[]
        {
            new Vector3Int ( 1, 0, 0),
            new Vector3Int (-1, 0, 0),
            new Vector3Int ( 0, 0, 1),
            new Vector3Int ( 0, 0,-1),
        };
        FunctionTimer adjacencyRemovalTimer = new FunctionTimer();
        foreach (int[] rc in riverCoords)
        {
            //Debug.Log("STARTING: " + rc[0] + "," + rc[1] + "," + rc[2]);
            for (int i = 0; i< dirs.Length; i++)
            {
                int[] adjTileCoord = new int[] { rc[0] + dirs[i].x, rc[1] + dirs[i].y, rc[2] + dirs[i].z };
                string[] types;
                if (!IsInBounds(ref adjTileCoord))
                    types = new string[] { "river" };
                else
                    types = tiles[adjTileCoord[0], adjTileCoord[1], adjTileCoord[2]].types;

                tiles[rc[0], rc[1], rc[2]].RemovePrototypesByAdjacency(dirs[i], types);
            }
            if (tiles[rc[0], rc[1], rc[2]].prototypes.Count == 0)
            {
                Debug.LogWarning("Tile has no prototypes: " + rc[0] + "," + rc[1] + "," + rc[2], tiles[rc[0], rc[1], rc[2]].gameObject);
                res = false;
            }
        }
        adjacencyRemovalTimer.StopTimer("Adjacency Removal finished in");

        FunctionTimer propagationTimer = new FunctionTimer();
        foreach (int[] rc in riverCoords)
        {
            PlaceRiverMesh(rc);
            if (!Propagate(rc))
                res = false;
        }
        propagationTimer.StopTimer("River Tile Propagation took");
        return res;
    }

    private void SetRiverDirections(int[][] startCoords, int[][] startDirections)
    {
        int[] dilationDirs = {-1, 1};
        List<int[]> filledCoords = new List<int[]>(startCoords);

        // Add To Queue
        Queue<int[]> queue = new Queue<int[]>();
        for (int i = 0; i < startCoords.Length; i++)
            queue.Enqueue(startCoords[i]);

        // Add dilated start Coords to Queue
        //for (int i=0; i< startCoords.Length; i++)
        //{
        //    //tiles[startCoords[i][0], startCoords[i][1], startCoords[i][2]].riverDirection = new Vector2(startDirections[i][0], startDirections[i][1]);
        //    for(int j=0; j<dilationDirs.Length; j++)
        //    {
        //        int[] delta = RotateBy90(startDirections[i], dilationDirs[j]);
        //        int[] neighborCoord = { startCoords[i][0] + delta[0], startCoords[i][1], startCoords[i][2] + delta[1] };
        //        if (tiles[neighborCoord[0], neighborCoord[1], neighborCoord[2]].containsType("river"))
        //        {
        //            //tiles[neighborCoord[0], neighborCoord[1], neighborCoord[2]].riverDirection = new Vector2(startDirections[i][0], startDirections[i][1]);
        //            filledCoords.Add(new int[] { neighborCoord[0], neighborCoord[1], neighborCoord[2] });
        //            queue.Enqueue(new int[] { neighborCoord[0], neighborCoord[1], neighborCoord[2] });
        //            break;
        //        }
        //    }
        //}
        Vector2[] dirs =
        {
            new Vector2( 0,  1),
            new Vector2( 0, -1),
            new Vector2( 1,  0),
            new Vector2(-1,  0),
        };
        int iteration = 0;
        while(queue.Count > 0 && iteration < riverSolverMaxIterations)
        {
            iteration++;
            Vector2 flowDir = Vector2.zero;
            int[] currCoord = queue.Dequeue();
            //Check Surrounding Tiles For Connected River Tiles
            List<int> validDirs = new List<int>();
            for (int i = 0; i< dirs.Length; i++)
            {
                int[] adjTile = new int[] { currCoord[0] + (int)dirs[i].x, currCoord[1], currCoord[2] + (int)dirs[i].y };
                //Check if in Bounds
                if (!IsInBounds(ref adjTile))
                    continue;
                //Check if river Tile
                if (!tiles[adjTile[0], adjTile[1], adjTile[2]].containsType("river"))
                    continue;

                //Check if already filled or in the process of filling up
                bool alreadyFilled = false;
                foreach (int[] fc in filledCoords)
                {
                    if (fc[0] == adjTile[0])
                        if (fc[1] == adjTile[1])
                            if (fc[2] == adjTile[2])
                            {
                                alreadyFilled = true;
                                flowDir += tiles[adjTile[0], adjTile[1], adjTile[2]].riverDirection;
                                break;
                            }
                }
                if (alreadyFilled) continue;
                
                // Mark for flow computation
                validDirs.Add(i);
                // Add to Queue if not filled
                queue.Enqueue(adjTile);
                //filledCoords.Add(adjTile);
            }

            //Construct Flow Direction
            
            //if (validDirs.Count <= 0)
            //{
            //    Debug.Log("No Valid Dirs at: " + currCoord[0] + "," + currCoord[1] + "," + currCoord[2]);
            //    for (int i = 0; i < dirs.Length; i++)
            //    {
            //        int[] adjTile = new int[] { currCoord[0] + (int)dirs[i].x, currCoord[1], currCoord[2] + (int)dirs[i].y };
            //        if (!IsInBounds(ref adjTile))
            //            continue;
            //        if (tiles[adjTile[0], adjTile[1], adjTile[2]].containsType("river"))
            //            flowDir += tiles[adjTile[0], adjTile[1], adjTile[2]].riverDirection;
            //
            //    }
            //}
            for ( int i = 0; i < validDirs.Count; i++)
                flowDir += dirs[validDirs[i]];
            flowDir.Normalize();
            tiles[currCoord[0], currCoord[1], currCoord[2]].riverDirection = flowDir;
            filledCoords.Add(currCoord);
        }

        // DEBUG - Collapse
        //foreach (int[] rc in filledCoords)
        //    tiles[rc[0], rc[1], rc[2]].Collapse(rc);
    }

    private bool Propagate(int[] coords)
    {
        bool res = true;
        Stack<int[]> coordsStack = new Stack<int[]>();
        coordsStack.Push(coords);
        int iteration = 0;
        while(coordsStack.Count > 0)
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
            ref Tile currTile = ref tiles[pCoords[0], pCoords[1], pCoords[2]];
            currTile.Update();

            for(int i = 0; i < 6; i++)
            {
                int[] currentAdjCoords = adjCoords[i];

                if (!IsInBounds(ref currentAdjCoords))
                    continue;

                ref Tile adjTile = ref tiles[currentAdjCoords[0], currentAdjCoords[1], currentAdjCoords[2]];

                if (adjTile.isCollapsed())
                    continue;

                //Get possible prototypes for adjacent tile from currTile
                HashSet<Prototype> possible_prototypes = new HashSet<Prototype>();
                foreach(KeyValuePair<Prototype, float> p in currTile.prototypes)
                {
                    possible_prototypes.UnionWith(p.Key.prototypes[i].prototypes);
                }

                //Remove any Prototypes that are not part
                HashSet<Prototype> adjTilePrototypeList = new HashSet<Prototype>(adjTile.prototypes.Keys);
                adjTilePrototypeList.ExceptWith(possible_prototypes);

                if (adjTilePrototypeList.Count == 0)
                    continue;

                foreach(Prototype p in adjTilePrototypeList)
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
                    Debug.LogWarning("Tile has no Prototypes Left: " + currentAdjCoords[0] + ", " + currentAdjCoords[1] + ", " + currentAdjCoords[2], adjTile.gameObject);
                    Debug.Log("Iteration: " + iteration);
                    res = false;
                }

                // Add tiles that changed to stack
                coordsStack.Push(currentAdjCoords);
                iteration++;
            }
        }
        return res;
    }

    private bool GenerateHouseLayout()
    {
        Debug.Log("--- GENERATING HOUSE LAYOUT ---");
        // Get All valid house Coords
        List<int[]> houseCoords = new List<int[]>();
        for(int i=1; i< _internalSize.x - 1; i++)
            for(int j=1; j< _internalSize.z - 1; j++)
            {
                ref Tile tile = ref tiles[i, 0, j];
                if (tile.collapsed_prototype.basePrototype.type == "house")
                {
                    houseCoords.Add(new int[] { i, j });
                    tile.types = new string[] {"house"};
                    tile.Update();
                }
            }
        Debug.Log("Found House Tiles: " + houseCoords.Count);
        int houseIDCounter = 0;

        int iteration = 0;
        while (houseCoords.Count > 0 && iteration < houseSolverMaxIterations)
        {
            iteration++;
            //Flood Fill House Spaces to get connected houses
            List<List<int[]>> houseSpaces = FloodFillHouseCoords(ref houseCoords);


            // Check if House Space bigger than maxHouseSize and Leave as one house
            for (int i = 0; i < houseSpaces.Count;)
            {
                if (houseSpaces[i].Count <= maxHouseSize.x * maxHouseSize.y)
                {
                    foreach (int[] coord in houseSpaces[i])
                    {
                        tiles[coord[0], 0, coord[1]].houseID = houseIDCounter;
                        tiles[coord[0], 0, coord[1]].Update();
                        // DEBUG
                        // tiles[coord[0], 0, coord[1]].SetMaterial(debugMaterials[houseIDCounter % debugMaterials.Length]);
                    }
                    houseIDCounter++;
                    houseSpaces.RemoveAt(i);
                }
                else
                    i++;
            }


            //Subdivide remaining house spaces using avgHouseSize
            foreach (List<int[]> hs in houseSpaces)
            {
                SolveHousePlacement(hs, ref houseIDCounter);
            }

            // Get All Tiles that are of type house but do not have a House ID
            GetNonIDHouseTiles(ref houseCoords);
        }
        if (iteration >= houseSolverMaxIterations)
        {
            Debug.LogWarning("Max Iteration for Houses reached, Aborting!");
            return false;
        }

        // Join up small Houses with bigger ones
        Dictionary<int, List<int[]>> IDAmountDict = new Dictionary<int, List<int[]>>();
        for(int i=1; i<_internalSize.x - 1; i++)
            for (int j=1; j< _internalSize.z - 1; j++)
            {
                ref Tile tile = ref tiles[i, 0, j];
                if (tile.houseID == -1)
                    continue;
                if(IDAmountDict.ContainsKey(tile.houseID))
                {
                    IDAmountDict[tile.houseID].Add(new int[] {i,j});
                }
                else
                    IDAmountDict[tile.houseID] = new List<int[]>() { new int[] {i,j} };
            }
        int[][] coordOffsets = new int[][]
        {
            new int[] { 1, 0, 0},
            new int[] {-1, 0, 1},
            new int[] { 0, 1, 4},
            new int[] { 0,-1, 5}
        };
        Queue<int> houseIDQueue = new Queue<int>();
        foreach (KeyValuePair<int, List<int[]>> ida in IDAmountDict)
            if (ida.Value.Count <= 2)
                houseIDQueue.Enqueue(ida.Key);
        iteration = 0;
        while (houseIDQueue.Count > 0 && iteration < smallHouseMergeMaxIteration)
        {
            iteration++;
            int id = houseIDQueue.Dequeue();
            int smallestAdjID = -1;
            if (!IDAmountDict.ContainsKey(id))
                continue;
            for (int i = 0; i < IDAmountDict[id].Count; i++)
            {
                int[] coord = IDAmountDict[id][i];
                for (int j = 0; j < 4; j++)
                {
                    // Check if connected
                    if (!IsConnectedToHouse(ref coord, coordOffsets[j][2]))
                        continue;
                    int[] adjCoord = { coord[0] + coordOffsets[j][0], coord[1] + coordOffsets[j][1] };
                    //Check if in Bounds
                    if (!IsInBounds2D(ref adjCoord, new int[] { 0, 1, 0 }))
                        continue;

                    ref Tile tile = ref tiles[adjCoord[0], 0, adjCoord[1]];
                    if (tile.houseID == id || tile.houseID == -1)
                        continue;

                    // Get House ID replace if smaller amount
                    if (smallestAdjID == -1)
                        smallestAdjID = tile.houseID;
                    else if (IDAmountDict[smallestAdjID].Count < IDAmountDict[tile.houseID].Count)
                    {
                        smallestAdjID = tile.houseID;
                    }
                }
            }
            if (smallestAdjID == -1)
                continue;

            for (int i = 0; i < IDAmountDict[id].Count; i++)
            {
                tiles[IDAmountDict[id][i][0], 0, IDAmountDict[id][i][1]].houseID = smallestAdjID;
                // DEBUG
                //tiles[IDAmountDict[id][i][0], 0, IDAmountDict[id][i][1]].SetMaterial(debugMaterials[smallestAdjID % debugMaterials.Length]);
            }
            IDAmountDict[smallestAdjID].AddRange(IDAmountDict[id]);
            IDAmountDict.Remove(id);
            if (IDAmountDict[smallestAdjID].Count <= 2)
                houseIDQueue.Enqueue(smallestAdjID);
        }
        if (iteration >= smallHouseMergeMaxIteration)
        {
            Debug.LogWarning("Small House Assignment Max Iteration reached! Items in Queue Left: " + houseIDQueue.Count);
            return false;
        }

        Debug.Log("--- FINISHED GENERATING HOUSE LAYOUT ---");
        return true;
    }

    private bool GenerateHouseHeights()
    {
        _houseIDHeightDict = new Dictionary<int, int>();
        Debug.Log("--- GENERATING HOUSE HEIGHTS ---");
        // Assign Random Height to house ID
        for (int i = 1; i < _internalSize.x - 1; i++)
            for (int j = 1; j < _internalSize.z - 1; j++)
            {
                int houseID = tiles[i, 0, j].houseID;
                if (houseID == -1)
                    continue;
                else if (_houseIDHeightDict.ContainsKey(houseID))
                    continue;
                else
                    _houseIDHeightDict.Add(houseID, Random.Range(2, size.y));
            }

        // APPROACH 1
        // Remove Only Set Roof Prototypes at height & null Prototype above
        //List<int[]> houseCoords = new List<int[]>();
        //for (int i = 0; i < size.x; i++)
        //    for (int j = 0; j < size.z; j++)
        //    {
        //        int houseHeight;
        //        if (_houseIDHeightDict.TryGetValue(tiles[i, 0, j].houseID, out houseHeight))
        //        {
        //            houseCoords.Add(new int[] { i, houseHeight - 1, j });
        //            tiles[i, houseHeight - 1, j].KeepPrototypesByType("roof");
        //            //Propagate(new int[] { i, houseHeight - 1, j });
        //            for (int k = 0; k < houseHeight; k++)
        //            {
        //                tiles[i, k, j].SetHeight(houseHeight);
        //                tiles[i, k, j].houseID = tiles[i, 0, j].houseID;
        //                tiles[i, k, j].Update();
        //            }
        //        }
        //    }

        // APPROACH 2
        //for (int i = 0; i < size.x; i++)
        //    for (int j = 0; j < size.z; j++)
        //    {
        //        ref Tile groundTile = ref tiles[i, 0, j];
        //        if (groundTile.houseID == -1)
        //            continue;
        //        int houseHeight = _houseIDHeightDict[groundTile.houseID];
        //        ref Tile roofTile = ref tiles[i, _houseIDHeightDict[groundTile.houseID], j];
        //        for (int k = 0; k < houseHeight; k++)
        //        {
        //            tiles[i, k, j].SetHeight(houseHeight);
        //            tiles[i, k, j].houseID = tiles[i, 0, j].houseID;
        //            tiles[i, k, j].Update();
        //        }
        //        roofTile.Collapse(nullPrototype);
        //        Propagate(new int[] { i, houseHeight, j });
        //    }

        // APPROACH 3
        Dictionary<int, List<int[]>> houses = new Dictionary<int, List<int[]>>();
        for (int i = 1; i < _internalSize.x - 1; i++)
            for (int j = 1; j < _internalSize.z - 1; j++)
            {
                ref Tile groundTile = ref tiles[i, 0, j];
                if (groundTile.houseID == -1)
                    continue;
                int houseHeight = _houseIDHeightDict[groundTile.houseID];
                ref Tile roofTile = ref tiles[i, _houseIDHeightDict[groundTile.houseID], j];
                roofTile.KeepPrototypesByType("roof");
                if(!houses.ContainsKey(groundTile.houseID))
                    houses.Add(groundTile.houseID, new List<int[]>());
                houses[groundTile.houseID].Add(new int[] { i, j });
                for (int k = 0; k < _internalSize.y; k++)
                {
                    tiles[i, k, j].SetHeight(houseHeight);
                    tiles[i, k, j].houseID = tiles[i, 0, j].houseID;
                    tiles[i, k, j].Update();
                }
            }
        bool allHousesCollapsed = true;
        foreach (KeyValuePair<int, List<int[]>> houseData in houses )
        {
            WFC_House house = new WFC_House(
                houseData.Key,
                _houseIDHeightDict[houseData.Key],
                size.y,
                housePrototypes.Length,
                houseData.Value,
                GetTile,
                houseColors[houseData.Key % houseColors.Length],
                SetHouseColor
                );
            if (!house.CollapseHouse())
            {
                allHousesCollapsed = false;
            }
        }

        

        //foreach (int[] hc in houseCoords)
        //    Propagate(hc);
        Debug.Log("--- FINISHED GENERATING HOUSE HEIGHTS ---");
        return allHousesCollapsed;
    }

    private ref Tile GetTile(int[] coord)
    {
        return ref tiles[coord[0], coord[1], coord[2]];
    }

    private void SetHouseColor(int[] coord, ref Material houseColor)
    {
        tiles[coord[0], coord[1], coord[2]].SetMaterial(houseColor, 0);
    }

    private void GetNonIDHouseTiles(ref List<int[]> houseCoordsList)
    {
        houseCoordsList.Clear();
        for (int i = 1; i < _internalSize.x - 1; i++)
            for (int j = 1; j < _internalSize.z - 1; j++)
            {
                ref Tile tile = ref tiles[i, 0, j];
                if (tile.collapsed_prototype.basePrototype.type == "house")
                {
                    if (tile.houseID == -1)
                        houseCoordsList.Add(new int[] { i, j });
                }
            }
        Debug.Log("House Coordindates found: " + houseCoordsList.Count);
    }

    private List<List<int[]>> FloodFillHouseCoords(ref List<int[]> houseCoords)
    {
        Vector3Int[] dirs = new Vector3Int[]
        {
            new Vector3Int( 1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int( 0, 0, 1),
            new Vector3Int( 0, 0,-1),
        };
        int[] typeAdjacencyIndices = new int[] { 0, 1, 4, 5 };
        List<List<int[]>> houseSpaces = new List<List<int[]>>();
        // While
        int iteration = 0;
        

        while (houseCoords.Count > 0 && iteration < houseFloodFillMaxIterations)
        {
            iteration++;
            List<int[]> houseSpace = new List<int[]>();
            Queue<int[]> coordQueue = new Queue<int[]>();

            // - Choose Random House Coord
            int[] startCoord = houseCoords[Random.Range(0, houseCoords.Count)];
            coordQueue.Enqueue(startCoord);
            houseSpace.Add(startCoord);
            houseCoords.Remove(startCoord);

            int queueIteration = 0;
            while (coordQueue.Count > 0 && queueIteration < houseFloodFillMaxIterations)
            {
                queueIteration++;
                // - Check adjacency if only house
                int[] currCoord = coordQueue.Dequeue();
                ref Prototype p = ref tiles[currCoord[0], 0, currCoord[1]].collapsed_prototype;
                for (int i = 0; i < typeAdjacencyIndices.Length; i++)
                {
                    ref string[] adjacentTypes = ref p.typeAdjacency[typeAdjacencyIndices[i]].types;

                    // Check if only house can be in this direction
                    if (adjacentTypes.Length > 1)
                        continue;
                    if (adjacentTypes[0] != "house")
                        continue;

                    // Add Adjacent Coord if not already existent
                    int[] adjCoord = new int[] { currCoord[0] + dirs[i].x, currCoord[1] + dirs[i].z };
                    if (!IsInBounds2D(ref adjCoord, new int[] { 0, 1, 0 }))
                        continue;
                    if (!IsInCoordList(ref houseSpace, ref adjCoord))
                    {
                        if (!IsInCoordList(ref houseCoords, ref adjCoord))
                            continue;
                        coordQueue.Enqueue(adjCoord);
                        houseSpace.Add(adjCoord);
                        RemoveCoordInList(ref houseCoords, ref adjCoord);
                    }
                }
            }
            if (queueIteration >= houseFloodFillMaxIterations)
                Debug.LogWarning("Aborting Current House Space");

            houseSpaces.Add(houseSpace);
        }
        if (iteration >= houseFloodFillMaxIterations)
            Debug.LogWarning("Aborting House Space Generation!");
        return houseSpaces;
    }

    private void SolveHousePlacement(List<int[]> houseCoords, ref int houseIDCounter)
    {
        //Calculate MinX, MinZ Point -- Preference X over Z
        int[] startPoint = new int[2] { _internalSize.x - 1, _internalSize.z - 1};
        foreach(int[] hc in houseCoords)
        {
            if (startPoint[0] > hc[0])
            {
                startPoint = hc;
                continue;
            }
            if (startPoint[0] == hc[0])
            {
                if(startPoint[1] > hc[1])
                {
                    startPoint = hc;
                    continue;
                }
            }
        }
        Debug.Log("StartCoord: " + startPoint[0] + ", " + startPoint[1], tiles[startPoint[0], 0, startPoint[1]].gameObject);

        // Flood fill in X direction and Y to houseSize
        

        // Normalize House size that first Coord is smaller than second
        Vector2Int houseSizeNormalized = new Vector2Int(Mathf.Min(avgHouseSize.x, avgHouseSize.y), Mathf.Max(avgHouseSize.x, avgHouseSize.y));

        List<int> coordShiftsX = new List<int>();
        Vector2Int dir = new Vector2Int(1, 0);
        int houseCountX = CalcPossibleHouseAmount(ref houseCoords, ref startPoint, ref coordShiftsX, houseSizeNormalized, dir);

        dir = new Vector2Int(0, 1);
        List<int> coordShiftsZ = new List<int>();
        int houseCountZ = CalcPossibleHouseAmount(ref houseCoords, ref startPoint, ref coordShiftsZ, houseSizeNormalized, dir);


        // - Choose option that places more houses
        List<int> chosenCoordShifts;
        int chosenHouseAmount;
        if( houseCountZ < houseCountX)
        {
            chosenHouseAmount = houseCountX;
            chosenCoordShifts = coordShiftsX;
            dir = new Vector2Int(1, 0);
        }
        else
        {
            chosenHouseAmount = houseCountZ;
            chosenCoordShifts = coordShiftsZ;
            dir = new Vector2Int(0, 1);
        }
        if (chosenHouseAmount > 0)
        {
            Vector2Int longHouseDir = new Vector2Int(dir.y, dir.x);
            Vector2Int houseShort = dir * houseSizeNormalized.x;
            Vector2Int houseWS = houseShort + longHouseDir * houseSizeNormalized.y;
            int shiftAccum = 0;
            for (int i = 0; i < chosenHouseAmount; i++)
            {
                //elongate Houses to double max Size y
                shiftAccum += chosenCoordShifts[i];
                Vector2Int shift = longHouseDir * shiftAccum;
                Vector2Int coordDiff = houseShort * i + shift;
                int[] coord = new int[] { startPoint[0] + coordDiff[0], startPoint[1] + coordDiff[1] };
                List<int[]> startCoords = new List<int[]>()
                {
                    new int[] { startPoint[0] + coordDiff[0], startPoint[1] + coordDiff[1] }
                };
                List<Vector2Int> housesWS = new List<Vector2Int>() { houseWS };
                Debug.Log("Original House Coords: " + startCoords[0][0] + ", " + startCoords[0][1] + "; House Dimensions: " + houseWS);
                ElongateAndSplitHouse(ref houseCoords, ref startCoords, ref housesWS, dir);
                for (int j = 0; j < housesWS.Count; j++)
                {
                    Debug.Log("Placing House " + (j + 1) + " out of " + housesWS.Count);
                    PlaceHouse(ref houseCoords, startCoords[j], housesWS[j], houseIDCounter);
                    houseIDCounter++;
                }
            }
        }
        else
        {
            Debug.Log("--- REMNANT HOUSES ---");
            int[][] adjDirs = new int[][]
            {
                new int[] { 1, 0, 0}, //Dir x, Dir y, typeAdjacency Index
                new int[] {-1, 0, 1},
                new int[] { 0, 1, 4},
                new int[] { 0,-1, 5}
            };
            int[] currCoord = startPoint;
            List<int[]> remnantHouseCoords = new List<int[]>() { currCoord };
            int currentLength = 0;
            while (currentLength < remnantHouseMaxLength)
            {
                List<int[]> adjCoords = new List<int[]>();
                for( int i = 0; i < adjDirs.Length; i++)
                {
                    //Check if house connected at that side
                    ref string[] types = ref tiles[currCoord[0], 0, currCoord[1]].collapsed_prototype.typeAdjacency[adjDirs[i][2]].types;
                    if (types.Length > 1)
                        continue;
                    if (!(types[0] == "house"))
                        continue;
                    //Check if adj Tile exists within House Space
                    int[] coord = { currCoord[0] + adjDirs[i][0], currCoord[1] + adjDirs[i][1] };
                    if (!IsInCoordList(ref houseCoords, ref coord))
                        continue;
                    adjCoords.Add(coord);
                }
                if (adjCoords.Count == 0)
                    break;
                if (adjCoords.Count > 1)
                    break;
                currentLength++;
                remnantHouseCoords.Add(adjCoords[0]);
                currCoord = adjCoords[0];

            }
            foreach (int[] coord in remnantHouseCoords)
            {
                
                ref Tile tile = ref tiles[coord[0], 0, coord[1]];
                tile.houseID = houseIDCounter;
                // DEBUG
                //tile.SetMaterial(debugMaterials[houseIDCounter % debugMaterials.Length]);
                tile.Update();
                int[] refCoord = coord;
                RemoveCoordInList(ref houseCoords, ref refCoord);
            }
            houseIDCounter++;
            Debug.Log("--- END REMNANT HOUSES ---");
        }

    }

    private void PlaceHouse(ref List<int[]> houseSpace, int[] startCoord, Vector2Int houseWS, int houseID)
    {
        //Debug.Log("Placing House at: " + startCoord[0] + ", " + startCoord[1] + "; Dimensions: " + houseWS);
        for (int i = 0; i < houseWS.x; i++)
            for (int j = 0; j < houseWS.y; j++)
            {
                int[] coord = { startCoord[0] + i, startCoord[1] + j };
                int[] coord3D = { coord[0], 0, coord[1] };
                if (!IsInBounds(ref coord3D))
                {
                    Debug.LogWarning("Coord not in bounds: " + coord[0] + ", " + coord[1] + "; Start Coord" + startCoord[0] + startCoord[1] + "; House Size: " + houseWS);
                }
                ref Tile tile = ref tiles[coord[0], 0, coord[1]];
                tile.houseID = houseID;
                tile.Update();
                // DEBUG
                //tile.SetMaterial(debugMaterials[houseID % debugMaterials.Length]);

                RemoveCoordInList(ref houseSpace, ref coord);
            }
    }

    //STILL THROWS ERRORS
    private void ElongateAndSplitHouse(ref List<int[]> houseSpace, ref List<int[]> inOutStartCoords, ref List<Vector2Int> inOutHousesWS, Vector2Int dir)
    {
        // Define MaxLength of House
        Vector2Int maxHouseSizeNormalized = new Vector2Int(
            Mathf.Min(maxHouseSize.x, maxHouseSize.y),
            Mathf.Max(maxHouseSize.x, maxHouseSize.y)
            );
        Vector2Int houseSizeNormalized = new Vector2Int(
            Mathf.Min(avgHouseSize.x, avgHouseSize.y),
            Mathf.Max(avgHouseSize.x, avgHouseSize.y)
            );
        Vector2Int longSideDirection = new Vector2Int(dir.y, dir.x);
        int longDirectionIndexNeg = dir.x < dir.y ? 1 : 5;
        int longDirectionIndexPos = dir.x < dir.y ? 0 : 4;
        int shortDirectionIndexPos = dir.x > dir.y ? 0 : 4;
        int maxLength = 2 * maxHouseSizeNormalized.y;
        int currentLength = houseSizeNormalized.y;
        Vector2Int house = inOutHousesWS[0];
        int[] newStartCoord = inOutStartCoords[0];
        // Check in -X/-Z Dir whether house tile is there
        // - check until maxLength is reached or no valid house Tiles Exist
        for (int i = 1; i< maxLength - houseSizeNormalized.y; i++ )
        {
            // Collect all Coords and check if house or existing
            bool allCoordsInList = true;
            List<int[]> neighborCoords = new List<int[]>(houseSizeNormalized.x);
            for (int j = 0; j < houseSizeNormalized.x; j++)
            {
                int[] coord = { inOutStartCoords[0][0] - i * dir.y + j * dir.x, inOutStartCoords[0][1] - i * dir.x + j * dir.y};
                if (!IsInCoordList(ref houseSpace, ref coord))
                {
                    allCoordsInList = false;
                    break;
                }
                neighborCoords.Add(coord);
            }
            if (!allCoordsInList)
                break;
            // Check if Tiles in LongDirection is connected
            if (!IsHouseConnected(ref neighborCoords, longDirectionIndexPos))
                break;
            if (!IsHouseInternalConnected(ref neighborCoords))
                break;
            house += -longSideDirection;
            currentLength++;
            newStartCoord[0] = newStartCoord[0] - longSideDirection.x;
            newStartCoord[1] = newStartCoord[1] - longSideDirection.y;
        }

        // If Max Length not reached, check +X/+Z Dir
        // - check until maxLength is reached or no valid house Tiles Exist
        int lengthAfterForward = currentLength;
        if (currentLength < maxLength)
        {
            for (int i = houseSizeNormalized.y; i < maxLength - lengthAfterForward + houseSizeNormalized.y; i++)
            {
                // Collect all Coords and check if house or existing
                bool allCoordsInList = true;
                List<int[]> neighborCoords = new List<int[]>(houseSizeNormalized.x);
                for (int j = 0; j < houseSizeNormalized.x; j++)
                {
                    int[] coord = { 
                        inOutStartCoords[0][0] + i * dir.y + j * dir.x, 
                        inOutStartCoords[0][1] + i * dir.x + j * dir.y };
                    if (!IsInCoordList(ref houseSpace, ref coord))
                    {
                        allCoordsInList = false;
                        break;
                    }
                    neighborCoords.Add(coord);
                }
                if (!allCoordsInList)
                    break;
                // Check if all Tiles Connect as House
                if (!IsHouseConnected(ref neighborCoords, longDirectionIndexNeg))
                    break;
                if (!IsHouseInternalConnected(ref neighborCoords))
                    break;
                house += longSideDirection;
                currentLength++;
            }
        }
        inOutStartCoords[0] = newStartCoord;
        
        // If length bigger than MaxHouseSize, split into two
        if(currentLength <= maxHouseSizeNormalized.y)
        {
            inOutHousesWS[0] = house;
            return;
        }
        int cutSize = Random.Range(2, currentLength - 1);
        Debug.Log("House Length: " + currentLength + "; Cutting at: " + cutSize);
        inOutHousesWS[0] = new Vector2Int(
            cutSize * dir.y + houseSizeNormalized.x * dir.x,
            cutSize * dir.x + houseSizeNormalized.x * dir.y);
        inOutHousesWS.Add(new Vector2Int(
            (currentLength - cutSize) * dir.y + houseSizeNormalized.x * dir.x,
            (currentLength - cutSize) * dir.x + houseSizeNormalized.x * dir.y));
        inOutStartCoords.Add(new int[] { newStartCoord[0] + longSideDirection.x * cutSize, newStartCoord[1] + longSideDirection.y * cutSize });
    }

    private bool IsHouseFit(ref List<int[]> houseSpace, ref int[] startCoord, Vector2Int houseWS)
    {
        for(int i = 0; i < houseWS.x; i++)
            for(int j = 0; j < houseWS.y; j++)
            {
                int[] coord = new int[] { startCoord[0] + i, startCoord[1] + j };
                if (!IsInCoordList(ref houseSpace, ref coord))
                    return false;

                ref Tile tile = ref tiles[coord[0], 0, coord[1]];
                if ((i == houseWS.x - 1) && (j == houseWS.y - 1))
                    continue;
                else if( i == houseWS.x - 1)
                {
                    // just check z
                    ref string[] adjacentTypes = ref tile.collapsed_prototype.typeAdjacency[4].types;

                    // Check if only house can be in this direction
                    if (adjacentTypes.Length > 1)
                        return false;
                    if (adjacentTypes[0] != "house")
                        return false;
                }
                else if(j == houseWS.y - 1)
                {
                    //just check x
                    ref string[] adjacentTypes = ref tile.collapsed_prototype.typeAdjacency[0].types;

                    // Check if only house can be in this direction
                    if (adjacentTypes.Length > 1)
                        return false;
                    if (adjacentTypes[0] != "house")
                        return false;
                }
                else
                {
                    // check x and z
                    ref string[] adjacentTypes = ref tile.collapsed_prototype.typeAdjacency[0].types;

                    // Check if only house can be in this direction
                    if (adjacentTypes.Length > 1)
                        return false;
                    if (adjacentTypes[0] != "house")
                        return false;

                    adjacentTypes = ref tile.collapsed_prototype.typeAdjacency[4].types;

                    // Check if only house can be in this direction
                    if (adjacentTypes.Length > 1)
                        return false;
                    if (adjacentTypes[0] != "house")
                        return false;
                }
            }
        // Check if all tiles connect via House
        return true;
    }

    private bool IsHouseConnected(ref List<int[]> houseSideCoords, int directionIndex)
    {
        foreach (int[] hSC in houseSideCoords)
        {
            ref string[] adjacentTypes = ref tiles[hSC[0], 0, hSC[1]].collapsed_prototype.typeAdjacency[directionIndex].types;
            if (adjacentTypes.Length > 1)
                return false;
            if (adjacentTypes[0] != "house")
                return false;
        }
        return true;
    }

    private bool IsConnectedToHouse(ref int[] coord, int directionIndex)
    {
        ref string[] adjacentTypes = ref tiles[coord[0], 0, coord[1]].collapsed_prototype.typeAdjacency[directionIndex].types;
        if (adjacentTypes.Length > 1)
            return false;
        if (adjacentTypes[0] != "house")
            return false;
        return true;
    }

    private bool IsHouseInternalConnected(ref List<int[]> houseCoords)
    {
        int[][] dirs = new int[][]
        {
            new int[] { 1, 0, 0},
            new int[] {-1, 0, 1},
            new int[] { 0, 1, 4},
            new int[] { 0,-1, 5}
        };
        foreach (int[] hC in houseCoords)
        {
            for (int i = 0; i < dirs.Length; i++)
            {
                // Get Adjacent Coord
                int[] adjCoord = new int[] { hC[0] + dirs[i][0], hC[1] + dirs[i][1] };
                //Check if adjCoord is in houseCoords list, if not ignore
                if (!IsInCoordList(ref houseCoords, ref adjCoord))
                    continue;
                //Check if connection in this direction is only "house"
                ref string[] types = ref tiles[hC[0], 0, hC[1]].collapsed_prototype.typeAdjacency[dirs[i][2]].types;
                if (types.Length > 1)
                    return false;
                if (!(types[0] == "house"))
                    return false;
            }
        }
        return true;
    }

    private int CalcPossibleHouseAmount(ref List<int[]> houseCoords, ref int[] startCoord, ref List<int> coordShifts, Vector2Int houseSizeNormalized, Vector2Int dir)
    {
        int[] coordShift = new int[] { -1, 0, 1 };

        bool housesFit = true;
        int[] currCoord = startCoord;
        int houseCount = 0;
        int adjacencyIndex = dir.x < dir.y ? 4 : 0;
        Vector2Int houseWS = new Vector2Int(
            dir.x * houseSizeNormalized.x + dir.y * houseSizeNormalized.y, 
            dir.x * houseSizeNormalized.y + dir.y * houseSizeNormalized.x
            );

        while (housesFit)
        {
            // - if fits place another one until does not fit
            bool currentHouseFits = false;
            int[] coord = new int[2];
            for (int i = 0; i < coordShift.Length; i++)
            {
                coord = new int[] { currCoord[0] + coordShift[i] * dir.y, currCoord[1] + coordShift[i] * dir.x };
                if (IsHouseFit(ref houseCoords, ref coord, houseWS))
                {
                    
                    
                    houseCount++;
                    currCoord = new int[] { coord[0] + dir.x * houseSizeNormalized.x, coord[1] + dir.y * houseSizeNormalized.x };

                    coordShifts.Add(coordShift[i]);
                    currentHouseFits = true;
                    break;
                }
            }
            if (!currentHouseFits)
                break;

            //Check if next house is connected
            List<int[]> connectedCoords = new List<int[]>(houseSizeNormalized.x);
            for (int j = 0; j < houseSizeNormalized.y; j++)
            {
                connectedCoords.Add(new int[] {
                            coord[0] + j * dir.y + dir.x * (houseSizeNormalized.x - 1),
                            coord[1] + j * dir.x + dir.y * (houseSizeNormalized.x - 1)
                        });
            }
            if (!IsHouseConnected(ref connectedCoords, adjacencyIndex))
                currentHouseFits = false;

            housesFit = currentHouseFits;
        }
        return houseCount;
    }

    public bool PlaceSocketMeshes()
    {
        Debug.Log("--- PLACING SOCKET MESHES ---");
        FunctionTimer timer = new FunctionTimer();
        foreach(Tile tile in tiles)
        {
            if (!tile.gameObject)
                continue;
            int childCount = tile.gameObject.transform.childCount;
            if (childCount == 0)
                continue;

            ref BasePrototype bp = ref tile.collapsed_prototype.basePrototype;
            Dictionary<int, int> placement = new Dictionary<int, int>();
            for( int i=0; i<bp.sockets.Length; i++)
            {
                int socketIndex = -1;
                if (bp.sockets[i].adjTileOffset == Vector2Int.zero)
                    socketIndex = bp.sockets[i].GeneratePlacement();
                else
                {
                    Vector2Int offset = bp.sockets[i].adjTileOffset;
                    //Rotate Offset
                    offset = RotateBy90(offset, tile.collapsed_prototype.rotation);
                    ref Tile adjTile = ref tiles[tile.tilePosition.x + offset.x, tile.tilePosition.y, tile.tilePosition.z + offset.y];
                    socketIndex = bp.sockets[i].GeneratePlacement(tile, ref adjTile);
                }
                if (socketIndex == -1)
                    continue;
                placement.Add(i, socketIndex);
            }

            Dictionary<string, Transform> tileChildren = new Dictionary<string, Transform>(childCount);
            for(int i = 0; i < childCount; i++)
            {
                Transform t = tile.gameObject.transform.GetChild(i);
                tileChildren[t.gameObject.name] = t;
            }
            foreach (KeyValuePair<int, int> pair in placement)
            {
                Transform parent = tileChildren[bp.sockets[pair.Key].name];
                ref Socket socket = ref bp.sockets[pair.Key];
                GameObject prefab = socket.receptacles[pair.Value].prefab;
                GameObject socketMesh = GameObject.Instantiate(prefab, parent);
                if (socket.receptacles[pair.Value].useHousePaint)
                {
                    if (tile.houseID != -1)
                    {
                        MeshRenderer mr = socketMesh.GetComponent<MeshRenderer>();
                        Material[] mats = mr.sharedMaterials;
                        mats[0] = houseColors[tile.houseID % houseColors.Length];
                        mr.sharedMaterials = mats;
                    }
                }
                // Rotate and Move Socket
                float zRot = 0.0f;
                switch(socket.rotationType)
                {
                    case Rotatable.RotateBy90:
                        zRot = Random.Range(0, 4) * 90f;
                        break;
                    case Rotatable.FreeRotation:
                        zRot = Random.Range(socket.rotation.x, socket.rotation.y);
                        break;
                }
                if(socket.rotationType != Rotatable.NoRotation)
                    socketMesh.transform.Rotate(Vector3.up, zRot);

                if(socket.movable)
                {
                    Vector3 delta = new Vector3(
                        Random.Range(-socket.maxMoveAmount.x, socket.maxMoveAmount.x),
                        0,
                        Random.Range(-socket.maxMoveAmount.y, socket.maxMoveAmount.y)
                        );
                    socketMesh.transform.localPosition = delta;
                }
            }
        }
        timer.StopTimer("Finished Socket Mesh Placement in");
        Debug.Log("--- SOCKET PLACEMENT FINISHED ---");
        return true;
    }

    private void Collapse(int[] coords)
    {
        ref Tile tile = ref tiles[coords[0], coords[1], coords[2]];
        // Get Adjacent Collapsed Coords
        int[][] adjCollapsedCoords = GetAdjacentCollapsedCoords(coords);
        // Check if they contain Prototype from Overwrite Probs
        for (int i = 0; i < adjCollapsedCoords.Length; i++)
        {
            ref Tile adjTile = ref tiles[adjCollapsedCoords[i][0], adjCollapsedCoords[i][1], adjCollapsedCoords[i][2]];
            for (int j = 0; j < overwriteProbabilities.Get().Length; j++)
            {
                if (overwriteProbabilities.Get()[j].first == adjTile.collapsed_prototype)
                {
                    //Check if second exists at coords and overwrite Probability
                    float prob;
                    if (tile.prototypes.TryGetValue(overwriteProbabilities.Get()[j].second, out prob))
                        tile.prototypes[overwriteProbabilities.Get()[j].second] = overwriteProbabilities.Get()[j].probability;
                    
                    break;
                }
            }
        }
        // If so, modify probability before Collapsing
        //Debug.Log("Collapsing at:" + coords[0] + ", " + coords[1] + ", " + coords[2], tiles[coords[0], coords[1], coords[2]].gameObject);
        tiles[coords[0], coords[1], coords[2]].Collapse();
    }

    private int[] GetMinimumUncertaintyCoords()
    {
        List<int[]> coords = new List<int[]>();
        int minUncertainty = groundPrototypes.Length + housePrototypes.Length;
        for(int i=0; i< _internalSize.x; i++) // i == x
        {
            for(int j=0; j< _internalSize.z; j++) // j == z
            {
                    if (tiles[i, _currentLayer, j].isCollapsed()) continue;
                    int tileUncertainty = tiles[i, _currentLayer, j].GetUncertainty();
                    if (tileUncertainty < minUncertainty)
                    {
                        coords.Clear();
                        coords.Add(new int[] { i, _currentLayer, j });
                    }
                    else if(tileUncertainty == minUncertainty)
                    {
                        coords.Add(new int[]{ i, _currentLayer, j });
                    }
            }
        }
        if (coords.Count == 1)
        {
            return coords[0];
        }
        else if (coords.Count == 0)
        {
            return new int[] {
                Random.Range(0, _internalSize.x),
                _currentLayer,
                Random.Range(0, _internalSize.z)
            };
        }
        else
        {
            return coords[Random.Range(0, coords.Count)];
        }
    }

    private bool IsCollapsed()
    {
        foreach(Tile tile in tiles)
        {
            if (!tile.isCollapsed())
            {
                return false;
            }
        }
        return true;
    }

    private bool IsCurrentLayerCollapsed()
    {
        for (int i = 0; i < _internalSize.x; i++)
            for (int j = 0; j < _internalSize.z; j++)
            {
                if (!tiles[i, _currentLayer, j].isCollapsed())
                    return false;
            }
        return true;
    }

    private int[] RotateBy90(int[] vector, int dir)
    {
        return new int[] {
             vector[1] * dir,
            -vector[0] * dir,
        };
    }

    private Vector2Int RotateBy90(Vector2Int vec, int rotIndex)
    {
        if (rotIndex == 0)
            return vec;
        Vector2Int[] rots = { 
            new Vector2Int( 0,  1), // 0
            new Vector2Int(-1,  0), // -270 ccw
            new Vector2Int( 0, -1), // -180 ccw
            new Vector2Int( 1,  0), // -90 ccw
        };
        return new Vector2Int(
            vec.x * rots[rotIndex].y - vec.y * rots[rotIndex].x,
            vec.x * rots[rotIndex].x + vec.y * rots[rotIndex].y
            );
    }

    private void PlaceRiverMesh(int[] coords)
    {
        if (!useRiverMesh)
            return;
        GameObject riverGo = GameObject.Instantiate(riverMesh, gameObject.transform);
        riverGo.transform.localPosition = new Vector3(coords[0] * tileSize, coords[1] * tileSize, coords[2] * tileSize);
    }

    private bool IsInBounds(ref int[] coord)
    {
        for(int i = 0; i < coord.Length; i++)
            if (coord[i] < 0 || coord[i] >= _internalSize[i])
                return false;
        return true;
    }

    private bool IsInBounds2D(ref int[] coord, int[] axis)
    {
        for (int i = 0; i < coord.Length; i++)
            if (coord[i] < 0 || coord[i] >= _internalSize[i + axis[i]])
                return false;
        return true;
    }

    private bool RemoveCoordInList(ref List<int[]> coordList, ref int[] coord)
    {
        for(int i=0; i<coordList.Count; i++)
        {
            if (IsSameCoord(coordList[i], coord))
            {
                coordList.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    
    private bool IsInCoordList(ref List<int[]> coordList, ref int[] coord)
    {
        foreach (int[] c in coordList)
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
            if (a[i] != b[i])
                return false;
        return true;
    }
}

[System.Serializable]
public class Tile
{
    public string[] types;
    private string name;
    public Vector3 position;
    public Vector3Int tilePosition;

    public Transform parent;
    public GameObject gameObject;
    public Dictionary<Prototype, float> prototypes;
    public Prototype collapsed_prototype;
    public WFC_Tile _dataViz;
    public Vector2 riverDirection;
    public int houseID;
    public int houseHeight;

    public Tile(Prototype[] prototypes, Vector3 position, Vector3Int tilePosition, Transform parent)
    {
        this.name = tilePosition.x + "_" + tilePosition.y + "_" + tilePosition.z;
        this.tilePosition = tilePosition;
        this.position = position;
        //this._dataViz = gameObject.GetComponent<WFC_Tile>();
        //this._dataViz.position = tilePosition;
        List<string> prototypeTypes = new List<string>();
        foreach (Prototype p in prototypes)
        {
            string type = p.basePrototype.type;
            if (!prototypeTypes.Contains(type))
                prototypeTypes.Add(type);
        }
        this.types = prototypeTypes.ToArray();
        this.riverDirection = new Vector2(0, 0);
        this.prototypes = new Dictionary<Prototype, float>();
        foreach (Prototype p in prototypes)
            this.prototypes.Add(p, p.basePrototype.probability);
        houseID = -1;
        Update();
        this.parent = parent;
    }

    public int GetUncertainty()
    {
        if (collapsed_prototype == null)
            return prototypes.Count;
        else
            return 1;
    }

    public void Collapse()
    {
        collapsed_prototype = ChooseWeighted();
        List<Prototype> uncollapsedPrototypes = new List<Prototype>(prototypes.Keys);
        foreach (Prototype p in uncollapsedPrototypes)
        {
            if (p == collapsed_prototype)
                continue;
            else
                prototypes.Remove(p);
        }
        if (collapsed_prototype.basePrototype.mesh != null)
        {
            gameObject = GameObject.Instantiate(collapsed_prototype.basePrototype.mesh, parent);
            Vector3 rotation = new Vector3(0.0f, 90.0f * collapsed_prototype.rotation, 0.0f);
            gameObject.transform.localPosition = position;
            gameObject.transform.eulerAngles = rotation;
            gameObject.name = name + "_" + collapsed_prototype.name;
            this._dataViz = gameObject.AddComponent<WFC_Tile>();
        }
        Update();
        
    }

    public void Collapse(Prototype prototype)
    {
        if(!prototypes.ContainsKey(prototype))
        {
            Debug.LogWarning("Prototype is not a possible solution, aborting!");
            return;
        }
        collapsed_prototype = prototype;
        List<Prototype> uncollapsedPrototypes = new List<Prototype>(prototypes.Keys);
        foreach(Prototype p in uncollapsedPrototypes)
        {
            if (!(p == prototype))
                prototypes.Remove(p);
        }
        if (collapsed_prototype.basePrototype.mesh != null)
        {
            gameObject = GameObject.Instantiate(collapsed_prototype.basePrototype.mesh, parent);
            Vector3 rotation = new Vector3(0.0f, 90.0f * collapsed_prototype.rotation, 0.0f);
            gameObject.transform.localPosition = position;
            gameObject.transform.eulerAngles = rotation;
            gameObject.name = name + "_" + collapsed_prototype.name;
            this._dataViz = gameObject.AddComponent<WFC_Tile>();
        }
        Update();
    }

    public void SetMaterial(Material material, int index)
    {
        MeshRenderer mr = gameObject.GetComponent<MeshRenderer>();
        Material[] mats = mr.sharedMaterials;
        mats[index] = material;
        mr.sharedMaterials = mats;
        _dataViz.SetDebugCubeMaterial(material);
    }

    public void KeepPrototypesByType(string type)
    {
        if (!containsType(type))
            return;
        if (types.Length == 1)
            return;
        List<Prototype> prototypeList = new List<Prototype>(prototypes.Keys);
        for (int i = 0; i < prototypeList.Count; i++)
        {
            if (!(prototypeList[i].basePrototype.type == type))
                prototypes.Remove(prototypeList[i]);
        }
        this.types = new string[] { type };
        Update();
    }

    public void RemovePrototypesByType(string type)
    {
        if (!containsType(type))
            return;
        List<Prototype> prototypeList = new List<Prototype>(prototypes.Keys);
        for (int i=0; i< prototypeList.Count;)
        {
            if (prototypeList[i].basePrototype.type == type)
            {
                prototypes.Remove(prototypeList[i]);
                prototypeList.RemoveAt(i);
            }
            else
                i++;
        }
        List<string> newTypes = new List<string>(types);
        newTypes.Remove(type);
        types = newTypes.ToArray();
        Update();
    }

    private Prototype ChooseWeighted()
    {
        float max = 0;
        foreach(KeyValuePair<Prototype, float> p in prototypes)
        {
            max += p.Value;
        }
        float random = Random.Range(0.0f, max);
        
        foreach(KeyValuePair<Prototype, float> p in prototypes)
        {
            if (random < p.Value)
                return p.Key;
            random -= p.Value;
        }
        return null;
    }

    public void Update()
    {
        if (!isCollapsed())
            return;
        if (gameObject == null)
            return;
        if (_dataViz.possibleTiles == null)
            _dataViz.possibleTiles = new List<string>();
        _dataViz.possibleTiles.Clear();
        foreach(KeyValuePair<Prototype, float> prototype in prototypes)
        {
            _dataViz.possibleTiles.Add(prototype.Key.name);
        }
        _dataViz.riverDirection = riverDirection;
        _dataViz.types = types;
        _dataViz.isCollapsed = isCollapsed();
        _dataViz.houseID = houseID;
        _dataViz.height = houseHeight;
    }

    public bool isCollapsed()
    {
        return collapsed_prototype != null;
    }

    public bool containsType(string type)
    {
        for( int i=0; i<types.Length; i++)
        {
            if (types[i] == type)
                return true;
        }
        return false;
    }

    public void RemovePrototypesByAdjacency(Vector3Int dir, string[] types)
    {
        string typeString = "";
        for (int i = 0; i < types.Length; i++)
            typeString += types[i] + ",";
        //Debug.Log("Checking in Direction: " + dir + "; Accepted Types: " + typeString);
        List<Prototype> prototypeList = new List<Prototype>(prototypes.Keys);
        for(int i=0; i< prototypeList.Count; )
        {
            // Create Direction-PossibleAdjacentTypes Dictionary
            Dictionary<Vector3Int, string[]> typeDict = new Dictionary<Vector3Int, string[]>();
            for (int j = 0; j < prototypeList[i].typeAdjacency.Length; j++)
                typeDict.Add(prototypeList[i].typeAdjacency[j].direction, prototypeList[i].typeAdjacency[j].types);

            string[] pAdjTypes = typeDict[dir];

            
            if (types.Length == 1)
            {
                if(!(pAdjTypes.Length == 1))
                {
                    prototypes.Remove(prototypeList[i]);
                    prototypeList.RemoveAt(i);

                    continue;
                }
                if (!(pAdjTypes[0] == types[0]))
                {
                    prototypes.Remove(prototypeList[i]);
                    prototypeList.RemoveAt(i);
                    continue;
                }
                i++;
                continue;
            }
            bool protHasType = false;
            for (int k = 0; k < types.Length; k++)
            {
                for (int m = 0; m< pAdjTypes.Length; m++)
                {
                    if (types[k] == pAdjTypes[m])
                    {
                        protHasType = true;
                        break;
                    }
                }
                if (protHasType)
                    break;
            }

            if (!protHasType)
            {
                prototypes.Remove(prototypeList[i]);
                prototypeList.RemoveAt(i);
            }
            else
                i++;
        }
        //string prototypeString = "PrototypesLeft: ";
        //foreach(KeyValuePair<Prototype, float> p in prototypes)
        //    prototypeString = prototypeString + "," + p.Key.name;
        //Debug.Log(prototypeString);
        Update();
    }

    //DEBUG
    public void EnableDebugCube()
    {
        _dataViz.EnableDebugCube();
    }

    public void PlacePrototypeMeshes()
    {
        for (int i = 0; i< gameObject.transform.childCount;)
        {
            Transform child = gameObject.transform.GetChild(i);
            if (child.gameObject.name == "DebugCube")
                i++;
            else
                GameObject.DestroyImmediate(child.gameObject);
        }
        int count = 1;
        foreach(KeyValuePair<Prototype, float> p in prototypes)
        {
            GameObject go = new GameObject();
            //GameObject.Instantiate(go, gameObject.transform);
            go.name = p.Key.name;
            go.transform.parent = gameObject.transform;
            MeshFilter goMf = go.AddComponent<MeshFilter>();
            goMf.sharedMesh = p.Key.mesh;
            MeshRenderer goMr = go.AddComponent<MeshRenderer>();
            goMr.sharedMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;
            go.transform.localPosition = new Vector3(0, count * 5, 0);
            go.transform.eulerAngles = new Vector3(0, p.Key.rotation * 90f, 0);
            count++;
        }
    }

    public void SetHeight(int height)
    {
        houseHeight = height;
        if (!isCollapsed())
            return;
        if (gameObject == null)
            return;
        _dataViz.height = height;
    }
}
