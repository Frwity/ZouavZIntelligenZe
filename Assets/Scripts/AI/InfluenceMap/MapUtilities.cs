using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public Vector3 position = Vector3.zero;
    public float strategicInfluence = 0f;
    public float militaryInfluence = 0f;
    public ETeam team = ETeam.Neutral;
    public int weight;
}

public class Connection
{
    public int cost;
    public Tile from;
    public Tile to;
}