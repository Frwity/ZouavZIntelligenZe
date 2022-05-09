using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Squad : MonoBehaviour
{
    [HideInInspector] public List<Unit> members = new List<Unit>();
    private Formation SquadFormation;
    [HideInInspector] public Unit SquadLeader;
    private float MoveSpeed = 100.0f;

    private void Awake()
    {
        SquadFormation = GetComponent<Formation>();
        SquadFormation.Squad = this;
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
        SetSquadSpeed();
        foreach (Unit unit in members)   
        {
            unit.CurrentMoveSpeed = MoveSpeed;
            unit.SetTargetPos(unit.GridPosition);
        }
    }

    /*
     * The move speed of the squad is the lowest within the squad members
     */
    void SetSquadSpeed()
    {
        foreach (Unit unit in members)
        {
            MoveSpeed = Mathf.Min(MoveSpeed, unit.GetUnitData.Speed);
        }
    }
}
