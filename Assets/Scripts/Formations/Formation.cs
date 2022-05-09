using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_FORMATION_TYPE
{
    Circle,
    Square,
    Line,
    None,
    Custom
}

/*
 * Class that calculate the position of squad members base of the type of formation selected
 */
public class Formation
{
    private E_FORMATION_TYPE FormationType;
    private float Radius = 5.0f;

    [HideInInspector] public Squad Squad;

    private float GridDistance = 5.0f;
    private Vector3 OldLeaderPos;

    private Unit FormationLeader = null;

    //Like nb of parallel line in the formation
    private int SpecialFormationValue = 3;

    public E_FORMATION_TYPE SetFormationType
    {
        set => FormationType = value;
    }

    public Formation()
    {
        //For testing
        FormationType = E_FORMATION_TYPE.Circle;
    }

    public void UpdateFormationLeader()
    {
        if (Squad.members.Count != 0)
            FormationLeader = Squad.members[0];
    }

    public void CreateFormation(Vector3 targetPos)
    {
        if (Squad.members.Count == 0)
            return;
        
        ChooseLeader(targetPos);

        switch (FormationType)
        {
            case E_FORMATION_TYPE.Circle:
                CreateCircleFormation(targetPos);
                break;
            case E_FORMATION_TYPE.Line:
                CreateLineFormation(targetPos);
                break;
            case E_FORMATION_TYPE.Custom:
                break;
        }
        
        
        Squad.MoveUnitToPosition();
    }

    void CreateCircleFormation(Vector3 targetPos)
    {
        int numberOfSectors = Squad.members.Count;
        float radius = numberOfSectors * GridDistance / Mathf.PI;

        FormationLeader.GridPosition = targetPos;
        
        float rotY = FormationLeader.transform.eulerAngles.y;

        for (int i = 0; i < Squad.members.Count; i++)
        {
            float angle = i * 2 * Mathf.PI / numberOfSectors;
            Vector3 positionOffset = new Vector3(radius * Mathf.Sin(angle), 0, -radius + radius * Mathf.Cos(angle));
            Vector3 rotationOffset = Quaternion.Euler(0, rotY, 0) * positionOffset;
            
            //fix first unit of the squad so that it takes the empty slot
            if (FormationLeader.Equals(Squad.members[i]))
            {
                Squad.members[0].GridPosition = FormationLeader.GridPosition + rotationOffset;
                continue;
            }

            Squad.members[i].GridPosition = FormationLeader.GridPosition + rotationOffset;
        }
    }

    void CreateLineFormation(Vector3 targetPos)
    {
        //3 = number of parallel lines
        //TODO make a variable
        const float half = (3f - 1f) / 2f;

        FormationLeader.GridPosition = targetPos;
        for (int i = 0; i < Squad.members.Count; i++)
        {
            if (FormationLeader.Equals(Squad.members[i]))
                continue;
            
            int row = i / 3;
            int x = i % 3;

            Vector2 offset = new Vector2((x - half) * GridDistance, -1 * (row + 1) * GridDistance);
            float rotY = FormationLeader.transform.eulerAngles.y;
            Vector3 positionOffset = new Vector3(offset.x, 0, offset.y);
            Vector3 rotationOffset = Quaternion.Euler(0, rotY, 0) * positionOffset;
            Squad.members[i].GridPosition = FormationLeader.GridPosition + rotationOffset;
        }
    }
    
    void CalculatePosDuringMove()
    {
        
    }

    /*
     * Choose the leader when a move order is issue
     * The leader is the unit closest to the destination
     */
    void ChooseLeader(Vector3 pos)
    {
        float distance;
        
        if(FormationLeader != null)
            distance = Vector3.Distance(FormationLeader.transform.position, pos);
        else
        {
            FormationLeader = Squad.members[0];
            distance = Vector3.Distance(FormationLeader.transform.position, pos);
        }

        foreach (Unit unit in Squad.members)
        {
            float newDistance = Vector3.Distance(unit.transform.position, pos);

            if (newDistance < distance)
            {
                distance = newDistance;
                FormationLeader = unit;
            }
        }
    }
}