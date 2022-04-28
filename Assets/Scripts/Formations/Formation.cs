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
        //Grid = GetComponent<Grid>();
        Squad = GetComponent<Squad>();
        //For testing
        FormationType = E_FORMATION_TYPE.Circle;
    }

    public void CreateFormation(Vector3 targetPos)
    {
        if (Squad.members.Count == 0)
            return;
        
        Debug.Log("Formation Creation");
        FormationLeader = Squad.members[0];
        
        switch (FormationType)
        {
            case E_FORMATION_TYPE.Circle:
                CreateCircleFormation(targetPos);
                break;
        }
    }
    
    void CreateCircleFormation(Vector3 targetPos)
    {
        //OldLeaderPos = FormationLeader.transform.position;
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
            
            //Vector3 dir = FormationLeader.GridPosition - OldLeaderPos;
            //Vector3 right = new Vector3(dir.z, 0, -dir.x);
            Squad.members[i].GridPosition = FormationLeader.GridPosition + rotationOffset;
        }
        
        Squad.MoveUnitToPosition();
    }

    public void CalculateFormationPos(Vector3 pos)
    {
        
    }
}