using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Squad
{
    public List<Unit> members = new List<Unit>();
    private Formation SquadFormation;
    private float MoveSpeed = 100.0f;
    private UnitController Controller;
    //use to break formation and attack
    private bool CanBreakFormation = false;
    private bool SquadCapture = false;
    private TargetBuilding targetBuilding;
    public int totalCost = 0;
    
    public Squad(UnitController controller)
    {
         SquadFormation = new Formation(this);
         Controller = controller;
    }

    /*
     * Calculate position of the members
     */
    public void MoveSquad(Vector3 targetPos)
    {
        SquadFormation.CreateFormation(targetPos);
    }

    public void AddUnit(Unit unit)
    {
        members.Add(unit);
        totalCost += unit.Cost;
        //assign first unit to be the leader
        SquadFormation.UpdateFormationLeader();
    }

    public void AddUnits(List<Unit> units)
    {
        members.Clear();
        members = units;
    }

    public void ClearUnit()
    {
        members.Clear();
    }

    public void RemoveUnit(Unit unit)
    {
        if (!members.Remove(unit)) 
            return;
        SquadFormation.UpdateFormationLeader();
        //temp when unit is removed from squad recalculate formation based on the new leader grid position
        MoveSquad(members[0].GridPosition);
    }

    public void UpdateSquad()
    {
        if (SquadCapture)
        {
            if (!CanCapture(targetBuilding))
                return;

            SquadStartCapture(targetBuilding);
            SquadCapture = false;
        }
    }
    
    /*
     * Set target pos of NavMesh Agent
     */
    public void MoveUnitToPosition()
    {
        SetSquadSpeed();
        foreach (Unit unit in members)
        {
            unit.CurrentMoveSpeed = MoveSpeed;
            unit.SetTargetPos(unit.GridPosition);
        }
    }

    public int GetSquadValue()
    {
        int cost = 0;
        for (int i = 0; i < members.Count; ++i)
            cost += members[i].Cost;
        return cost;
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

    public void CaptureTarget(TargetBuilding target)
    {
        if (target == null)
            return;

        if (target.GetTeam() != Controller.GetTeam())
        {
            if(CanCapture(target))
                SquadStartCapture(target);
            else
            {
                SquadCapture = true;
                SquadNeedToCapture(target);
                targetBuilding = target;
                MoveSquad(target.transform.position);
            }
        }
    }

    void SquadNeedToCapture(TargetBuilding target)
    {
        foreach (Unit unit in members)
        {
            unit.NeedToCapture(target);
        }
    }

    void SquadStartCapture(TargetBuilding target)
    {
        Debug.Log("Squad Start capture");
        foreach (Unit unit in members)
        {
            unit.StartCapture(target);
        }
    }

    private bool CanCapture(TargetBuilding target)
    {
        if (target == null || (target.transform.position - SquadFormation.FormationLeader.gameObject.transform.position).sqrMagnitude > SquadFormation.FormationLeader.GetUnitData.CaptureDistanceMax * SquadFormation.FormationLeader.GetUnitData.CaptureDistanceMax)
            return false;

        return true;
    }

    private void StopSquadMovement()
    {
        foreach (Unit unit in members)
        {
            unit.StopMovement();
        }
    }

    public void AttackTarget(Vector3 targetPos)
    {
        
    }
    
    public void SwitchFormation(E_FORMATION_TYPE newFormationType)
    {
        SquadFormation.SetFormationType = newFormationType;
    }
}
