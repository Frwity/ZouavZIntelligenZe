using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_TASK_STATE
{
    Busy,       // cannot be assign another task
    Ongoing,  // ongoing task but can be assign to another task
    Free
}

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
    public E_MODE SquadMode;

    private E_TASK_STATE InternalState;

    public E_TASK_STATE State
    {
        get => InternalState;
        set
        {
            if (value != E_TASK_STATE.Busy)
                InternalState = value;
        }
    }

    public Squad(UnitController controller)
    {
         SquadFormation = new Formation(this);
         Controller = controller;
         SquadMode = E_MODE.Agressive;
         InternalState = E_TASK_STATE.Free;
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
        unit.SetMode(SquadMode);
        members.Add(unit);
        //assign first unit to be the leader if Leader is not set
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
        MoveSquad(members[0].transform.position);
    }

    public void UpdateSquad()
    {
        if (SquadCapture)
            SquadStartCapture(targetBuilding);
    }
    
    /*
     * Set target pos of NavMesh Agent
     */
    public void MoveUnitToPosition()
    {
        InternalState = E_TASK_STATE.Ongoing;
        SetSquadSpeed();
        foreach (Unit unit in members)
        {
            unit.CurrentMoveSpeed = MoveSpeed;
            unit.SetTargetPos(unit.GridPosition);
        }
    }

    /*
     * The move speed of the squad is the lowest move speed within the squad members
     */
    void SetSquadSpeed()
    {
        foreach (Unit unit in members)
        {
            MoveSpeed = Mathf.Min(MoveSpeed, unit.GetUnitData.Speed);
        }
    }
    
    /*
     * Capture task 
     */
    public void CaptureTarget(TargetBuilding target)
    {
        if (target == null && !CanChangeTask())
            return;

        if (target.GetTeam() != Controller.GetTeam())
        {
            State = E_TASK_STATE.Busy;
            target.OnBuiilduingCaptured.AddListener(OnSquadCaptureTarget);
            if (CanCapture(target))
            {
                SquadStartCapture(target);
            }
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
        foreach (Unit unit in members)
        {
            if (unit.IsAtDestination() && unit.needToCapture)
                unit.StartCapture(target);
        }
    }

    private bool CanCapture(TargetBuilding target)
    {
        if (target == null || 
            (target.transform.position - SquadFormation.FormationLeader.gameObject.transform.position).sqrMagnitude > SquadFormation.FormationLeader.GetUnitData.CaptureDistanceMax * SquadFormation.FormationLeader.GetUnitData.CaptureDistanceMax)
            return false;

        return true;
    }

    public void StopSquadMovement()
    {
        foreach (Unit unit in members)
        {
            unit.StopMovement();
        }
    }

    public void AttackTarget(Vector3 targetPos)
    {
        if (!CanChangeTask())
            return;
        
        
    }
    
    public void SwitchFormation(E_FORMATION_TYPE newFormationType)
    {
        SquadFormation.SetFormationType = newFormationType;
    }

    public bool CanChangeTask()
    {
        return InternalState != E_TASK_STATE.Busy;
    }

    public void OnSquadCaptureTarget()
    {
        SquadCapture = false;
        InternalState = E_TASK_STATE.Free;
        targetBuilding.OnBuiilduingCaptured.RemoveListener(OnSquadCaptureTarget);
    }

    public void SetMode(E_MODE newMode)
    {
        SquadMode = newMode;
        foreach(Unit unit in members)
        {
            unit.SetMode(newMode);
        }
    }
}
