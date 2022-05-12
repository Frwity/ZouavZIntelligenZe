using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum E_BUILDTYPE
{
    MINER,
    CAPTUREPOINT,
    HEAVYFACTORY,
    LIGHTFACTORY,
    TURRET,
    NOTHING
}

public class Map : MonoBehaviour
{
    private static Map _instance;
    public static Map Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Map>();

                if (_instance == null)
                {
                    GameObject container = new GameObject("Map");
                    _instance = container.AddComponent<Map>();
                }
            }

            return _instance;
        }
    }

    [SerializeField]
    private int grassCost = 1;
    [SerializeField]
    private int unreachableCost = int.MaxValue;

    [SerializeField]
    private int gridSizeH = 100;
    [SerializeField]
    private int gridSizeV = 100;
    [SerializeField]
    private int squareSize = 5;
    [SerializeField]
    private int maxHeight = 10;
    [SerializeField]
    private int maxWalkableHeight = 4;

    List<Tile> tileList = new List<Tile>();
    public List<Tile> tilesWithBuild = new List<Tile>();
    private Dictionary<Tile, List<Connection>> ConnectionsGraph = new Dictionary<Tile, List<Connection>>();

    Vector3 gridStartPos = Vector3.zero;
    private int nbTilesH = 0;
    private int nbTilesV = 0;

    private UnitController[] unitControllers;

    private void Awake()
    {
        CreateMap();
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateGraph();
        InvokeRepeating("UpdateMap", 1f, 1f);
        unitControllers = FindObjectsOfType<UnitController>();
    }

    public void AddTargetBuilding(TargetBuilding targetBuilding, ETeam team)
    {
        Tile tile = GetTile(targetBuilding.transform.position);
        tile.strategicInfluence = targetBuilding.GetTeam() == ETeam.Blue ? targetBuilding.influence : -targetBuilding.influence;
        tile.buildType = E_BUILDTYPE.CAPTUREPOINT;
        if (!tilesWithBuild.Contains(tile))
            tilesWithBuild.Add(tile);
    }

    public void AddFactory(Factory factory, ETeam team)
    {
        Tile tile = GetTile(factory.transform.position);
        tile.strategicInfluence = factory.GetTeam() == ETeam.Blue ? factory.influence : -factory.influence;
        tile.buildType = factory.GetFactoryData.TypeId == 0 ? E_BUILDTYPE.LIGHTFACTORY : E_BUILDTYPE.HEAVYFACTORY;
        tilesWithBuild.Add(tile);
    }

    // Update is called once per frame
    void UpdateMap()
    {
        float outValue = 0f;
        foreach (UnitController unitController in unitControllers)
        {
            foreach (Unit unit in unitController.UnitList)
            {
                Tile tile = GetTile(unit.transform.position);
                if (unit.currentTilesInfluence.TryGetValue(tile, out outValue) && Math.Abs(unit.Influence - outValue) < 0.0001f)
                    continue;

                else
                {
                    foreach (KeyValuePair<Tile, float> t in unit.currentTilesInfluence)
                        t.Key.militaryInfluence -= t.Value;

                    unit.currentTilesInfluence.Clear();
                    unit.UpdateTile(tile, unit.Influence);
                }
            }
        }
    }

    private void CreateMap()
    {
        tileList.Clear();

        gridStartPos = transform.position + new Vector3(-gridSizeH / 2f, 0f, -gridSizeV / 2f);

        nbTilesH = gridSizeH / squareSize;
        nbTilesV = gridSizeV / squareSize;

        for (int i = 0; i < nbTilesV; i++)
        {
            for (int j = 0; j < nbTilesH; j++)
            {
                Tile tile = new Tile();
                Vector3 tilePos = gridStartPos + new Vector3((j + 0.5f) * squareSize, 0f, (i + 0.5f) * squareSize);

                int Weight = 0;
                RaycastHit hitInfo = new RaycastHit();

                // Always compute tile Y pos from floor collision
                if (Physics.Raycast(tilePos + Vector3.up * maxHeight, Vector3.down, out hitInfo, maxHeight + 1, 1 << LayerMask.NameToLayer("Floor")))
                {
                    if (Weight == 0)
                        Weight = hitInfo.point.y >= maxWalkableHeight ? unreachableCost : grassCost;
                    tilePos.y = hitInfo.point.y;
                }

                tile.weight = Weight;
                tile.position = tilePos;
                tileList.Add(tile);
            }
        }
    }

    private void CreateGraph()
    {
        foreach (Tile tile in tileList)
        {
            if (IsTileWalkable(tile))
            {
                ConnectionsGraph.Add(tile, new List<Connection>());
                foreach (Tile neighbour in GetNeighbours(tile))
                {
                    Connection connection = new Connection();
                    connection.cost = tile.weight + neighbour.weight;
                    connection.from = tile;
                    connection.to = neighbour;
                    ConnectionsGraph[tile].Add(connection);
                }
            }
        }
    }

    public List<Tile> GetNeighbours(Tile tile)
    {
        Vector2Int tileCoord = GetTileCoordFromPos(tile.position);
        int x = tileCoord.x;
        int y = tileCoord.y;

        List<Tile> tiles = new List<Tile>();

        if (x > 0)
        {
            if (y > 0)
                TryToAddTile(tiles, GetTile(x - 1, y - 1));
            TryToAddTile(tiles, tileList[(x - 1) + y * nbTilesH]);
            if (y < nbTilesV - 1)
                TryToAddTile(tiles, tileList[(x - 1) + (y + 1) * nbTilesH]);
        }

        if (y > 0)
            TryToAddTile(tiles, tileList[x + (y - 1) * nbTilesH]);
        if (y < nbTilesV - 1)
            TryToAddTile(tiles, tileList[x + (y + 1) * nbTilesH]);

        if (x < nbTilesH - 1)
        {
            if (y > 0)
                TryToAddTile(tiles, tileList[(x + 1) + (y - 1) * nbTilesH]);
            TryToAddTile(tiles, tileList[(x + 1) + y * nbTilesH]);
            if (y < nbTilesV - 1)
                TryToAddTile(tiles, tileList[(x + 1) + (y + 1) * nbTilesH]);
        }

        return tiles;
    }

    private Vector2Int GetTileCoordFromPos(Vector3 pos)
    {
        Vector3 realPos = pos - gridStartPos;
        Vector2Int tileCoords = Vector2Int.zero;
        tileCoords.x = Mathf.FloorToInt(realPos.x / squareSize);
        tileCoords.y = Mathf.FloorToInt(realPos.z / squareSize);
        return tileCoords;
    }

    private void TryToAddTile(List<Tile> list, Tile tile)
    {
        if (IsTileWalkable(tile))
        {
            list.Add(tile);
        }
    }

    public Tile GetTile(Vector3 position)
    {
        Vector2Int pos = GetTileCoordFromPos(position);
        return GetTile(pos.x, pos.y);
    }

    private Tile GetTile(int x, int y)
    {
        int index = y * nbTilesH + x;
        if (index >= tileList.Count || index < 0)
            return null;

        return tileList[index];
    }

    private bool IsTileWalkable(Tile tile)
    {
        return tile.weight < unreachableCost;
    }

    public List<Tile> GetTilesWithBuildAroundPoint(Vector3 startPos, float range)
    {
        List<Tile> list = new List<Tile>();

        Tile startTile = GetTile(startPos);

        startTile.GetBuild(list, range, startPos);

        return list;
    }

    private void OnDrawGizmos()
    {
        GUIStyle gUIStyle = new GUIStyle();
        for (int i = 0; i < tileList.Count; i++)
        {
            ETeam tileTeam = tileList[i].GetTeam();
            gUIStyle.normal.textColor = tileTeam == ETeam.Blue ? Color.blue : tileTeam == ETeam.Red ? Color.red : Color.green;
            gUIStyle.fontStyle = FontStyle.Bold;
            Handles.Label(tileList[i].position, tileList[i].militaryInfluence.ToString() + " " + tileList[i].strategicInfluence.ToString(), gUIStyle);
        }
    } 
}
