using System;
using System.Collections.Generic;
using UnityEngine;

// points system for units creation (Ex : light units = 1 pt, medium = 2pts, heavy = 3 pts)
// max points can be increased by capturing TargetBuilding entities
public class UnitController : MonoBehaviour
{
    [SerializeField]
    protected ETeam Team;
    public ETeam GetTeam() { return Team; }

    [SerializeField]
    protected int StartingBuildPoints = 15;

    protected int _TotalBuildPoints = 0;

    protected List<Squad> Squads = new List<Squad>();

    protected Squad SelectedSquad;

    public E_MODE SelectedSquadMode { get { return SelectedSquad.SquadMode; } }
    
    public int TotalBuildPoints
    {
        get { return _TotalBuildPoints; }
        set
        {
            //Debug.Log("TotalBuildPoints updated");
            _TotalBuildPoints = value;
            OnBuildPointsUpdated?.Invoke();
        }
    }

    protected int _CapturedTargets = 0;
    public int CapturedTargets
    {
        get { return _CapturedTargets; }
        set
        {
            _CapturedTargets = value;
            OnCaptureTarget?.Invoke();
        }
    }

    protected Transform TeamRoot = null;
    public Transform GetTeamRoot() { return TeamRoot; }

    public List<Unit> UnitList = new List<Unit>();
    protected List<Unit> SelectedUnitList = new List<Unit>();
    public List<Factory> FactoryList = new List<Factory>();
    protected List<Factory> SelectedFactoryList = new List<Factory>();

    // events
    protected Action OnBuildPointsUpdated;
    protected Action OnCaptureTarget;

    #region Unit methods
    protected void UnselectAllUnits()
    {
        foreach (Unit unit in SelectedUnitList)
            unit.SetSelected(false);
        SelectedUnitList.Clear();
        if (SelectedSquad != null)
            SelectedSquad.members.Clear();
    }
    protected void SelectAllUnits()
    {
        foreach (Unit unit in UnitList)
            unit.SetSelected(true);

        SelectedUnitList.Clear();
        SelectedUnitList.AddRange(UnitList);
    }
    protected void SelectAllUnitsByTypeId(int typeId)
    {
        UnselectCurrentFactory();
        UnselectAllUnits();
        SelectedUnitList = UnitList.FindAll(delegate (Unit unit)
            {
                return unit.GetTypeId == typeId;
            }
        );
        foreach (Unit unit in SelectedUnitList)
        {
            unit.SetSelected(true);
        }
    }
    protected void SelectUnit(Unit unit)
    {
        unit.SetSelected(true);
        SelectedUnitList.Add(unit);
        SelectedSquad.AddUnit(unit);
    }
    protected void UnselectUnit(Unit unit)
    {
        unit.SetSelected(false);
        SelectedUnitList.Remove(unit);
    }
    virtual public void AddUnit(Unit unit)
    {
        unit.OnDeadEvent += () =>
        {
            TotalBuildPoints += unit.Cost;
            if (unit.IsSelected)
                SelectedUnitList.Remove(unit);
            UnitList.Remove(unit);
        };
        UnitList.Add(unit);
    }
    public void CaptureTarget(int points)
    {
        //Debug.Log("CaptureTarget");
        TotalBuildPoints += points;
        CapturedTargets++;
    }
    public void LoseTarget(int points)
    {
        TotalBuildPoints -= points;
        CapturedTargets--;
    }

    public int GetMilitaryPower()
    {
        int power = 0;
        foreach (Unit unit in UnitList)
            power += unit.Cost;
        return power;
    }

    #endregion

    #region Factory methods
    void AddFactory(Factory factory)
    {
        if (factory == null)
        {
            Debug.LogWarning("Trying to add null factory");
            return;
        }

        factory.OnDeadEvent += () =>
        {
            TotalBuildPoints += factory.Cost;
            if (factory.IsSelected)
                SelectedFactoryList.Remove(factory);
            FactoryList.Remove(factory);
        };
        FactoryList.Add(factory);
    }
    virtual public void SelectFactory(Factory factory)
    {
        if (factory == null || factory.IsUnderConstruction)
            return;

        factory.SetSelected(true);
        SelectedFactoryList.Add(factory);
        UnselectAllUnits();
    }
    virtual public void UnselectCurrentFactory()
    {
        foreach (Factory factory in SelectedFactoryList)
            factory.SetSelected(false);
        SelectedFactoryList.Clear();
    }
    protected bool RequestUnitBuild(int unitMenuIndex, Factory factory)
    {
        return factory.RequestUnitBuild(unitMenuIndex, null);
    }
    public Factory RequestFactoryBuild(int factoryIndex, Vector3 buildPos)
    {
        if (SelectedFactoryList.Count == 0)
            return null;

        int cost = SelectedFactoryList[0].GetFactoryCost(factoryIndex);
        if (TotalBuildPoints < cost)
            return null;

        // Check if positon is valid
        if (SelectedFactoryList[0].CanPositionFactory(factoryIndex, buildPos) == false)
            return null;

        Factory newFactory = SelectedFactoryList[0].StartBuildFactory(factoryIndex, buildPos);
        if (newFactory != null)
        {
            AddFactory(newFactory);
            TotalBuildPoints -= cost;

            return newFactory;
        }
        return null;
    }
    public int GetLFactoryCount()
    {
        int count = 0;
        foreach (Factory factory in FactoryList)
            if (factory.GetFactoryData.TypeId == 0)
                count += factory.Cost;
        return count;
    }
    public int GetHFactoryCount()
    {
        int count = 0;
        foreach (Factory factory in FactoryList)
            if (factory.GetFactoryData.TypeId == 1)
                count += factory.Cost;
        return count;
    }
    #endregion

    #region MonoBehaviour methods
    virtual protected void Awake()
    {
        string rootName = Team.ToString() + "Team";
        TeamRoot = GameObject.Find(rootName)?.transform;
        //if (TeamRoot)
            //Debug.LogFormat("TeamRoot {0} found !", rootName);
    }
    virtual protected void Start ()
    {
        CapturedTargets = 0;
        TotalBuildPoints = StartingBuildPoints;

        // get all team factory already in scene
        Factory [] allFactories = FindObjectsOfType<Factory>();
        foreach(Factory factory in allFactories)
        {
            if (factory.GetTeam() == GetTeam())
            {
                AddFactory(factory);
            }
        }

        //Debug.Log("found " + FactoryList.Count + " factory for team " + GetTeam().ToString());
    }
    virtual protected void Update ()
    {
		
	}
    #endregion
}
