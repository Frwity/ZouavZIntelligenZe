using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_TASK_STATE
{
    Free,
    Ongoing, // ongoing task but can be assign to another task
    Busy       // cannot be assign another task
}

public class Squad
{
    public List<Unit> members = new List<Unit>();
    private Formation SquadFormation;
    private float MoveSpeed = 100.0f;
    private UnitController Controller;
    //use to break formation and attack
    public bool CanBreakFormation = false;
    private bool SquadCapture = false;
    private bool SquadAttack = false;
    private TargetBuilding targetBuilding;
    public int totalCost = 0;
    
    public E_MODE SquadMode;
    //Target unit to destroy
    private BaseEntity SquadTarget = null;
    private E_TASK_STATE InternalState;

    public E_TASK_STATE State
    {
        get => InternalState;
    }

    public Squad(UnitController controller)
    {
         SquadFormation = new Formation(this);
         Controller = controller;
         SquadMode = E_MODE.Defensive;
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
        totalCost += unit.Cost;
        unit.OnUnitDeath += RemoveUnit;
        //assign first unit to be the leader
        SquadFormation.UpdateFormationLeader();
    }
    
    /*
     * Clear the current squad members and add the new units in the squad
     */
    public void AddUnits(List<Unit> units)
    {
        members.Clear();
        foreach(Unit unit in units)
            AddUnit(unit);
    }

    public void ClearUnits()
    {
        foreach (Unit unit in members)
        {
            RemoveUnit(unit);
        }
    }

    public void RemoveUnit(Unit unit)
    {
        if (!members.Remove(unit)) 
            return;
        
        unit.OnUnitDeath -= RemoveUnit;
        SquadFormation.UpdateFormationLeader();
        //temp when unit is removed from squad recalculate formation based on the new leader grid position
        if(members.Count > 0)
            MoveSquad(members[0].transform.position);
    }

    public void UpdateSquad()
    {
        if (SquadCapture)
            SquadStartCapture(targetBuilding);

        if (SquadAttack && SquadTarget)
            SquadAttackTarget();
    }
    
    /*
     * Set target pos of NavMesh Agent of units
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

    public int GetSquadValue()
    {
        int cost = 0;
        for (int i = 0; i < members.Count; ++i)
            cost += members[i].Cost;
        return cost;
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
        if (target == null)
            return;

        if (target.GetTeam() != Controller.GetTeam())
        {
            SquadNeedToCapture(target);

            InternalState = E_TASK_STATE.Busy;
            target.OnBuiilduingCaptured.AddListener(OnSquadCaptureTarget);
            SquadFormation.ChooseLeader(target.transform.position);
            SquadCapture = true;
            MoveSquad(target.transform.position);
            targetBuilding = target;

            if (CanCapture(target))
            {
                CanBreakFormation = true;
                SquadStartCapture(target);
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
            if (unit.needToCapture && (unit.IsAtDestination() || unit.CanCapture(target)))
                unit.StartCapture(target);
        }
    }

    public void SquadStopCapture()
    {
        SquadCapture = false;
        foreach (Unit unit in members)
        {
            unit.StopCapture();
        }
    }

    private bool CanCapture(TargetBuilding target)
    {
        if (target == null || (target.transform.position - SquadFormation.FormationLeader.gameObject.transform.position).sqrMagnitude > SquadFormation.FormationLeader.GetUnitData.CaptureDistanceMax * SquadFormation.FormationLeader.GetUnitData.CaptureDistanceMax)
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

    public void SquadTaskAttack(BaseEntity target)
    {
        InternalState = E_TASK_STATE.Busy;
        SetMode(E_MODE.Agressive);
        SquadTarget = target;
        SquadAttack = true;
        SetSquadTarget();
        SquadTarget.OnDeadEvent += StopAttack;
    }

    public void SwitchFormation(E_FORMATION_TYPE newFormationType)
    {
        SquadFormation.SetFormationType = newFormationType;
        SquadFormation.CreateFormation(SquadFormation.FormationLeader.transform.position);
    }

    private void OnSquadCaptureTarget()
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
            unit.SetMode(SquadMode);
        }
    }

    public void StopAttack()
    {
        SquadTarget = null;
        SetSquadTarget();
        SquadAttack = false;
        InternalState = E_TASK_STATE.Free;
        StopSquadMovement();
        CanBreakFormation = false;
    }

    private void SetSquadTarget()
    {
        foreach (Unit unit in members)
        {
            unit.SetAttackTarget(SquadTarget);
        }
    }

    void SquadAttackTarget()
    {
        if (SquadFormation.FormationLeader.CanAttack(SquadTarget))
        {
            foreach (Unit unit in members)
            {
                if (!SquadTarget)
                    continue;

                if (unit.CanAttack(SquadTarget))
                    unit.ComputeAttack();
                else
                {
                    unit.SetTargetPos(SquadTarget.transform.position);
                    unit.EntityTarget = SquadTarget;
                }
            }
        }
        else
            MoveSquad(SquadTarget.transform.position);
    }

    /*
     * Reset Squad task on new order
     */
    public void ResetTask()
    {
        SquadTarget = null;
        SquadStopCapture();
        SetSquadTarget();
        SquadCapture = false;
        SquadAttack = false;
        InternalState = E_TASK_STATE.Free;
        CanBreakFormation = false;
        StopSquadMovement();
    }
}
