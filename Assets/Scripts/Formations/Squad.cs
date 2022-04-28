using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Squad : MonoBehaviour
{
    [HideInInspector] public List<Unit> members = new List<Unit>();
    private Formation SquadFormation;
    [HideInInspector] public Unit SquadLeader;

    private void Awake()
    {
        SquadFormation = GetComponent<Formation>();
        SquadFormation.Squad = this;
    }

    public void CreateSquad(List<Unit> selectedUnits)
    {
        foreach (Unit unit in selectedUnits)
        {
            members.Add(unit);
        }
        //SquadFormation.CreateFormation();
    }

    public void CreateSquadFormation(Vector3 targetPos)
    {
        SquadFormation.CreateFormation(targetPos);
    }

    public void AddUnit(Unit unit)
    {
        members.Add(unit);
    }

    public void RemoveMember(Unit unit)
    {
        members.Remove(unit);
    }
    
    public void MoveUnitToPosition()
    {
        foreach (Unit unit in members)   
        {
            unit.SetTargetPos(unit.GridPosition);
        }
    }
}
