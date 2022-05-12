using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public Vector3 position = Vector3.zero;
    public float strategicInfluence = 0f;
    public float militaryInfluence = 0f;
    public int weight;

    public ETeam GetTeam()
    {
        if (strategicInfluence > 0.1f)
            return ETeam.Blue;

        if (strategicInfluence < -0.1f)
            return ETeam.Red;

        if (militaryInfluence > 0.1f)
            return ETeam.Blue;

        if (militaryInfluence < -0.1f)
            return ETeam.Red;

        return ETeam.Neutral;
    }
}

public class Connection
{
    public int cost;
    public Tile from;
    public Tile to;
}