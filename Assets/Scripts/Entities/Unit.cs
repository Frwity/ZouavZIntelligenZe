using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

public enum E_MODE
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
    TargetBuilding CaptureTarget = null;
    NavMeshAgent NavMeshAgent;
    public BaseEntity EntityTarget = null;
    public UnitDataScriptable GetUnitData { get { return UnitData; } }
    public int Cost { get { return UnitData.Cost; } }
    public int GetTypeId { get { return UnitData.TypeId; } }
    public bool needToCapture = false;
    
    [SerializeField] E_MODE mode = E_MODE.Defensive;
    private float passiveFleeDistance = 25f;
    private bool isFleeing = false;
    private BaseEntity tempEntityTarget = null;
    private BaseEntity entityToKill = null;

    public Vector3 GridPosition;
    //Move speed of the squad
    public float CurrentMoveSpeed;
    public Dictionary<Tile, float> currentTilesInfluence = new Dictionary<Tile, float>();
    [SerializeField]
    private float influence = 1;
    public float Influence { get { return Team == ETeam.Blue ? influence : -influence; } }

    public new Action<Unit> OnUnitDeath;

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
        OnUnitDeath.Invoke(this);
        if (IsCapturing())
            StopCapture();

        if (GetUnitData.DeathFXPrefab)
        {
            GameObject fx = Instantiate(GetUnitData.DeathFXPrefab, transform);
            fx.transform.parent = null;
        }

        foreach (KeyValuePair<Tile, float> t in currentTilesInfluence)
            t.Key.militaryInfluence -= t.Value;

        Destroy(gameObject);
    }
    #region MonoBehaviour methods
    override protected void Awake()
    {
        base.Awake();

        NavMeshAgent = GetComponent<NavMeshAgent>();
        BulletSlot = transform.Find("BulletSlot");

        // fill NavMeshAgent parameters
        CurrentMoveSpeed = GetUnitData.Speed;
        NavMeshAgent.speed = CurrentMoveSpeed;
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

        if (isFleeing)
            CheckForStop();

        if (entityToKill)
            ChaseEntityToKill();
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
        if (!entityToKill)
        {
            if (EntityTarget != null)
            {
                EntityTarget.OnDeadEvent -= OnModeActionEnd;
                EntityTarget = null;
            }
        }

        if (NavMeshAgent)
        {
            NavMeshAgent.speed = CurrentMoveSpeed;
            NavMeshAgent.SetDestination(pos);
            NavMeshAgent.isStopped = false;
        }
    }

    // Targetting Task - attack
    public void SetAttackTarget(BaseEntity target)
    {
        if (target == null)
            return;

        if (CaptureTarget != null && !needToCapture)
            StopCapture();

        if (target.GetTeam() != GetTeam())
        {
            if (!CanAttack(target))
                SetTargetPos(target.gameObject.transform.position);
            
            EntityTarget = target;
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

    public void NeedToCapture(TargetBuilding target)
    {
        CaptureTarget = target;
        needToCapture = true;
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
        if (target == null || (target.transform.position - transform.position).sqrMagnitude > GetUnitData.AttackDistanceMax * GetUnitData.AttackDistanceMax)
            return false;

        return true;
    }

    // Attack Task
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
                        tempEntityTarget = EntityTarget;
                        entityToKill = EntityTarget = unitCollider.GetComponent<Unit>();
                        EntityTarget.OnDeadEvent += OnModeActionEnd;
                        return;

                    case E_MODE.Defensive:
                        tempEntityTarget = EntityTarget;
                        EntityTarget = unitCollider.GetComponent<Unit>();
                        EntityTarget.OnDeadEvent += OnModeActionEnd;
                        return;

                    case E_MODE.Flee:
                        tempEntityTarget = EntityTarget;
                        RaycastHit hit;
                        Vector3 direction = Vector3.up + (transform.position - unitCollider.transform.position).normalized * passiveFleeDistance;
                        int layerMask = (1 << LayerMask.NameToLayer("Floor")) | (1 << LayerMask.NameToLayer("Factory")) | (1 << LayerMask.NameToLayer("Target"));

                        if (Physics.Raycast(transform.position + Vector3.up, direction.normalized, out hit, direction.magnitude, layerMask))
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

        else if (tempEntityTarget != null)
            SetAttackTarget(tempEntityTarget);
    }

    void CheckForStop()
    {
        if (NavMeshAgent.remainingDistance < NavMeshAgent.stoppingDistance && NavMeshAgent.remainingDistance > 0f)
        {
            isFleeing = false;
            OnModeActionEnd();
        }
    }

    void ChaseEntityToKill()
    {
        if ((entityToKill.transform.position - transform.position).magnitude > GetUnitData.AttackDistanceMax)
            SetAttackTarget(entityToKill);
    }

    public void StopMovement()
    {
        NavMeshAgent.isStopped = true;
    }

    public void SetMode(E_MODE newMode)
    {
        mode = newMode;
    }

    public bool IsAtDestination()
    {
        return NavMeshAgent.remainingDistance < NavMeshAgent.stoppingDistance && NavMeshAgent.remainingDistance > 0f;
    }

    public void UpdateTile(Tile tile, float currentInfluence)
    {
        if (Math.Abs(currentInfluence) < 0.1f || currentTilesInfluence.ContainsKey(tile))
            return;

        currentTilesInfluence.Add(tile, currentInfluence);
        
        tile.militaryInfluence += currentInfluence;

        foreach(Tile t in Map.Instance.GetNeighbours(tile))
            UpdateTile(t, currentInfluence / 2f);
    }
}
