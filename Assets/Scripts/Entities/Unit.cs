﻿using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

enum E_MODE
{
    Agressive,
    Defensive,
    Flee,
    FollowInstruction
}

public class Unit : BaseEntity
{
    [SerializeField]
    UnitDataScriptable UnitData = null;

    Transform BulletSlot;
    float LastActionDate = 0f;
    BaseEntity EntityTarget = null;
    TargetBuilding CaptureTarget = null;
    NavMeshAgent NavMeshAgent;
    public UnitDataScriptable GetUnitData { get { return UnitData; } }
    public int Cost { get { return UnitData.Cost; } }
    public int GetTypeId { get { return UnitData.TypeId; } }
    public bool needToCapture = false;
    E_MODE mode = E_MODE.Flee;
    private float passiveFleeDistance = 25f;
    private bool isFleeing = false;

    override public void Init(ETeam _team)
    {
        if (IsInitialized)
            return;

        base.Init(_team);

        HP = UnitData.MaxHP;
        OnDeadEvent += Unit_OnDead;
    }
    void Unit_OnDead()
    {
        if (IsCapturing())
            StopCapture();

        if (GetUnitData.DeathFXPrefab)
        {
            GameObject fx = Instantiate(GetUnitData.DeathFXPrefab, transform);
            fx.transform.parent = null;
        }

        Destroy(gameObject);
    }
    #region MonoBehaviour methods
    override protected void Awake()
    {
        base.Awake();

        NavMeshAgent = GetComponent<NavMeshAgent>();
        BulletSlot = transform.Find("BulletSlot");

        // fill NavMeshAgent parameters
        NavMeshAgent.speed = GetUnitData.Speed;
        NavMeshAgent.angularSpeed = GetUnitData.AngularSpeed;
        NavMeshAgent.acceleration = GetUnitData.Acceleration;
        NavMeshAgent.stoppingDistance = 1f;
    }
    override protected void Start()
    {
        // Needed for non factory spawned units (debug)
        if (!IsInitialized)
            Init(Team);

        base.Start();
        InvokeRepeating("CheckForEnemy", 1f, 1f);
    }
    override protected void Update()
    {
        // Attack / repair task debug test $$$ to be removed for AI implementation
        if (EntityTarget != null)
        {
            if (EntityTarget.GetTeam() != GetTeam())
                ComputeAttack();
            else
                ComputeRepairing();
        }
        if (needToCapture)
            StartCapture(CaptureTarget);

        if (isFleeing)
            CheckForStop();
	}
    #endregion

    #region IRepairable
    override public bool NeedsRepairing()
    {
        return HP < GetUnitData.MaxHP;
    }
    override public void Repair(int amount)
    {
        HP = Mathf.Min(HP + amount, GetUnitData.MaxHP);
        base.Repair(amount);
    }
    override public void FullRepair()
    {
        Repair(GetUnitData.MaxHP);
    }
    #endregion

    #region Tasks methods : Moving, Capturing, Targeting, Attacking, Repairing ...

    // $$$ To be updated for AI implementation $$$

    // Moving Task
    public void SetTargetPos(Vector3 pos)
    {
        if (EntityTarget != null)
        {
            EntityTarget.OnDeadEvent -= OnModeActionEnd;
            EntityTarget = null;
        }

        if (CaptureTarget != null)
        {
            if (needToCapture)
            {
                needToCapture = false;
                CaptureTarget = null;
            }
            else
                StopCapture();
        }
        if (NavMeshAgent)
        {
            NavMeshAgent.SetDestination(pos);
            NavMeshAgent.isStopped = false;
        }
    }

    // Targetting Task - attack
    public void SetAttackTarget(BaseEntity target)
    {
        if (target == null)
            return;

        if (CaptureTarget != null)
            StopCapture();

        if (target.GetTeam() != GetTeam())
        {
            if (!CanAttack(target))
                SetTargetPos(target.gameObject.transform.position);
            
            StartAttacking(target);
        }
    }

    // Targetting Task - capture
    public void SetCaptureTarget(TargetBuilding target)
    {
        if (target == null)
            return;
     
        if (EntityTarget != null)
            EntityTarget = null;

        if (IsCapturing())
            StopCapture();

        if (target.GetTeam() != GetTeam())
        {
            if (CanCapture(target))
                StartCapture(target);

            else
            {
                SetTargetPos(target.gameObject.transform.position);
                CaptureTarget = target;
                needToCapture = true;
            }
        }
    }

    // Targetting Task - repairing
    public void SetRepairTarget(BaseEntity entity)
    {
        if (CanRepair(entity) == false)
            return;

        if (CaptureTarget != null)
            StopCapture();

        if (entity.GetTeam() == GetTeam())
            StartRepairing(entity);
    }
    public bool CanAttack(BaseEntity target)
    {
        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > GetUnitData.AttackDistanceMax * GetUnitData.AttackDistanceMax)
            return false;

        return true;
    }

    // Attack Task
    public void StartAttacking(BaseEntity target)
    {
        EntityTarget = target;
    }
    public void ComputeAttack()
    {
        if (CanAttack(EntityTarget) == false)
            return;

        if (NavMeshAgent)
            NavMeshAgent.isStopped = true;

        transform.LookAt(EntityTarget.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - LastActionDate) > UnitData.AttackFrequency)
        {
            LastActionDate = Time.time;
            // visual only ?
            if (UnitData.BulletPrefab)
            {
                GameObject newBullet = Instantiate(UnitData.BulletPrefab, BulletSlot);
                newBullet.transform.parent = null;
                newBullet.GetComponent<Bullet>().ShootToward(EntityTarget.transform.position - transform.position, this);
            }
            // apply damages
            int damages = Mathf.FloorToInt(UnitData.DPS * UnitData.AttackFrequency);
            EntityTarget.AddDamage(damages);
        }
    }
    public bool CanCapture(TargetBuilding target)
    {
        // distance check
        if (target == null || (target.transform.position - transform.position).sqrMagnitude > GetUnitData.CaptureDistanceMax * GetUnitData.CaptureDistanceMax)
            return false;

        return true;
    }

    // Capture Task
    public void StartCapture(TargetBuilding target)
    {
        if (CanCapture(target) == false)
            return;

        if (NavMeshAgent)
            NavMeshAgent.isStopped = true;

        CaptureTarget = target;
        CaptureTarget.StartCapture(this);
        needToCapture = false;
    }
    public void StopCapture()
    {
        if (CaptureTarget == null)
            return;

        CaptureTarget.StopCapture(this);
        CaptureTarget = null;
    }

    public bool IsCapturing()
    {
        return CaptureTarget != null && !needToCapture;
    }

    // Repairing Task
    public bool CanRepair(BaseEntity target)
    {
        if (GetUnitData.CanRepair == false || target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > GetUnitData.RepairDistanceMax * GetUnitData.RepairDistanceMax)
            return false;

        return true;
    }
    public void StartRepairing(BaseEntity entity)
    {
        if (GetUnitData.CanRepair)
        {
            EntityTarget = entity;
        }
    }

    // $$$ TODO : add repairing visual feedback
    public void ComputeRepairing()
    {
        if (CanRepair(EntityTarget) == false)
            return;

        if (NavMeshAgent)
            NavMeshAgent.isStopped = true;

        transform.LookAt(EntityTarget.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - LastActionDate) > UnitData.RepairFrequency)
        {
            LastActionDate = Time.time;

            // apply reparing
            int amount = Mathf.FloorToInt(UnitData.RPS * UnitData.RepairFrequency);
            EntityTarget.Repair(amount);
        }
    }
    #endregion

    void CheckForEnemy()
    {
        if (EntityTarget != null && EntityTarget is Unit)
            return;

        Collider[] unitsCollider = Physics.OverlapSphere(transform.position, 15f, 1 << LayerMask.NameToLayer("Unit"));
        foreach(Collider unitCollider in unitsCollider)
        {
            if (unitCollider.GetComponent<Unit>().Team != Team && (!EntityTarget || !(EntityTarget is Unit)))
            {
                switch (mode)
                {
                    case E_MODE.Agressive:
                        EntityTarget = unitCollider.GetComponent<Unit>();
                        EntityTarget.OnDeadEvent += OnModeActionEnd;
                        return;

                    case E_MODE.Flee:
                        RaycastHit hit;
                        Vector3 direction = Vector3.up + (transform.position - unitCollider.transform.position).normalized * passiveFleeDistance;

                        if (Physics.Raycast(transform.position + Vector3.up, direction.normalized, out hit, direction.magnitude, 1 << LayerMask.NameToLayer("Floor")))
                            direction = hit.point - transform.position;

                        TargetBuilding temp = CaptureTarget;
                        CaptureTarget = null;
                        SetTargetPos(direction + transform.position);
                        CaptureTarget = temp;
                        isFleeing = true;
                        return;
                }
            }
        }
    }

    void OnModeActionEnd()
    {
        if (needToCapture)
        {
            TargetBuilding temp = CaptureTarget;
            CaptureTarget = null;
            SetCaptureTarget(temp);
        }
    }

    void CheckForStop()
    {
        if (NavMeshAgent.remainingDistance < NavMeshAgent.stoppingDistance && NavMeshAgent.remainingDistance > 0f)
        {
            isFleeing = false;
            OnModeActionEnd();
        }
    }
}
