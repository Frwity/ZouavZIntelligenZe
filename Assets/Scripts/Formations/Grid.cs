using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    private List<Vector3> GridPoints;

    public void InitGrid(int nbUnitInFormation)
    {
        for (int i = 0; i < nbUnitInFormation; i++)
        {
            GridPoints.Add(new Vector3(0f, 0f, 0f));
        }
    }
    
    public void RecalculatePos(Unit formationLeader, Unit formationFollower)
    {
        
    }
}
