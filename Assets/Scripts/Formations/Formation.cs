using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum E_FORMATION_TYPE
{
    Circle,
    Square
}

public class Formation : MonoBehaviour
{
    private E_FORMATION_TYPE FormationType;

    private float radius = 5.0f;
    
    // Time to check the formation
    [SerializeField] private float CheckFormationTime = 0.0f;

    private List<BaseEntity> Entities;
    private List<Vector3> gridPoints;
    private BaseEntity FormationLeader
    {
        get => FormationLeader;
        set => FormationLeader = value;
    }
    
    /**
     * Create a squad with selected entities
     */
    void CreateSquad()
    {
        
    }

    void CreateFormation()
    {
        switch (FormationType)
        {
            case E_FORMATION_TYPE.Circle:
                CreateCircleFormation();
                break;
        }
    }
    
    void CreateCircleFormation()
    {
        int n = Entities.Count;
        int numberOfSector = n + 1;
        //float radius = numberOfSector * gridDistance / Mathf.PI;
        
        for (int i = 0; i < n; i++)
        {
            float angle = (i + 1) * 2 * Mathf.PI / numberOfSector;
            Vector2 offset = new Vector2(radius * Mathf.Sin(angle), -radius + radius * Mathf.Cos(angle));
            AddFormationGridPoint(Entities[i], offset);
        }
    }

    void AddFormationGridPoint(BaseEntity entity, Vector2 offset)
    {
        gridPoints.Add(offset);
    }

    void ClearFormation()
    {
        Entities.Clear();
    }

    void ClearGrid()
    {
        gridPoints.Clear();
    }
}