using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum E_FORMATION_TYPE
{
    Circle,
    Square,
    None,
    Custom
}

/*
 * Class that calculate the position of squad members base of the type of formation selected
 */
public class Formation : MonoBehaviour
{
    private E_FORMATION_TYPE FormationType;
    private float Radius = 5.0f;
    private Grid Grid;

    public Squad Squad;

    private float GridDistance = 5.0f;
    private Vector3 OldLeaderPos;

    private Unit FormationLeader;


    private void Awake()
    {
        Squad = GetComponent<Squad>();
        //For testing
        FormationType = E_FORMATION_TYPE.Circle;
    }

    public void CreateFormation(Vector3 targetPos)
    {
        if (Squad.members.Count == 0)
            return;
        
        FormationLeader = Squad.members[0];
        
        switch (FormationType)
        {
            case E_FORMATION_TYPE.Circle:
                CreateCircleFormation(targetPos);
                break;
            case E_FORMATION_TYPE.Square:
                CreateSquareFormation(targetPos);
                break;
            case E_FORMATION_TYPE.Custom:
                break;
        }
    }
    
    void CreateCircleFormation(Vector3 targetPos)
    {
        int numberOfSectors = Squad.members.Count;
        float radius = numberOfSectors * GridDistance / Mathf.PI;

        FormationLeader.GridPosition = targetPos;
        
        for (int i = 1; i < Squad.members.Count; i++)
        {
            float angle = i * 2 * Mathf.PI / numberOfSectors;
            Vector2 offset = new Vector2(radius * Mathf.Sin(angle), -radius + radius * Mathf.Cos(angle));

            float rotY = FormationLeader.transform.eulerAngles.y;
            Vector3 positionOffset = new Vector3(offset.x, 0, offset.y);
            Vector3 rotationOffset = Quaternion.Euler(0, rotY, 0) * positionOffset;
            
            Squad.members[i].GridPosition = FormationLeader.GridPosition + rotationOffset;
        }
        
        Squad.MoveUnitToPosition();
    }

    void CreateSquareFormation(Vector3 targetPos)
    {
        
    }
}