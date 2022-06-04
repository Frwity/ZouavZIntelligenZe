using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StrategicTask
{
    [SerializeField] protected TaskData taskDate;
    protected AIController controller;
    public Squad squad;
    public bool isComplete = false;

    public abstract bool Evaluate(AIController _controller, ref float currentScore);

    public void SetUnit(Unit _unit)
    {
        squad.AddUnit(_unit);
    }

    public virtual void StartTask(AIController _controller)
    {
        isComplete = false;
        controller = _controller;
    }

    public virtual void UpdateTask() { }
    public virtual void EndTask() 
    {
        isComplete = true;
    }
}

public class CapturePointTask : StrategicTask
{
    public static int id { get; private set; } = 0;

    TargetBuilding targetBuilding;

    StrategicTask squadCreation = null;

    public CapturePointTask(Squad _squad)
    {
        squad = _squad;
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        float score = 0.0f;

        int captureIndex = int.MaxValue;
        float distance = float.MaxValue;

        for (int i = 0; i < _controller.capturableTargets.transform.childCount; ++i)
        {
            float tempdist = (_controller.capturableTargets.transform.GetChild(i).position - _controller.FactoryList[0].transform.position).magnitude;
            if (_controller.capturableTargets.transform.GetChild(i).GetComponent<TargetBuilding>().GetTeam() == ETeam.Neutral && tempdist < distance)
            {
                captureIndex = i;
                distance = tempdist;
            }
        }
        if (captureIndex != int.MaxValue)
        {
            targetBuilding = _controller.capturableTargets.transform.GetChild(captureIndex).GetComponent<TargetBuilding>();

            float ownedTarget = _controller.TargetBuildingList.Count + 1;
            float enemyTarget = _controller.playerController.TargetBuildingList.Count + 1;

            float targetBuildingRatio = ((enemyTarget + 0.25f) / ownedTarget) <= 0.01f ? 0.1f : (enemyTarget / ownedTarget);

            score = (_controller.taskDatas[id].Distance.Evaluate(distance / Map.Instance.mapSize) + _controller.taskDatas[id].Ratio.Evaluate(targetBuildingRatio)) 
                  *  _controller.taskDatas[id].Time.Evaluate(Time.time / 60.0f);
        }
        else
            return false;

        if (squad.GetSquadValue() <= Mathf.FloorToInt(Time.time / 60.0f))
        {
            squadCreation = new CreateExploSquadTask(squad);
            float f = 0.0f;
            squadCreation.Evaluate(_controller, ref f);
        }

        //Debug.Log("capture");
        //Debug.Log(score);

        if (score > currentScore)
        {
            currentScore = score;
            return true;
        }
        return false;
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        if (squadCreation != null)
        {
            squad.State = E_TASK_STATE.Busy;
            squadCreation.StartTask(_controller);
        }
        else
            StartCapture();
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (squad == null)
        {
            EndTask();
            return;
        }

        if (squadCreation != null)
        {
            squadCreation.UpdateTask();
            if (squadCreation.isComplete)
            {
                squad.State = E_TASK_STATE.Free;
                StartCapture();
                squadCreation = null;
            }
        }
        else // if squad complete, update
        {
            squad.UpdateSquad();

            if (targetBuilding.GetTeam() == controller.GetTeam() || squad.GetSquadValue() == 0)
                EndTask();
        }
    }

    public void StartCapture()
    {
        squad.SetMode(E_MODE.Flee);
        squad.CaptureTarget(targetBuilding);
    }

    public override void EndTask()
    {
        base.EndTask();
        squadCreation = null;
    }
}

// each diffenrent type of squad will be creating with hard code for a better control over the randomness and composition of the squad
public class CreateSquadTask : StrategicTask 
{
    public static int id { get; private set; } = 1;

    protected int money = 1;
    public int targetCost;
    public Factory factory = null;
    protected FACTORY_TYPE factoryType = FACTORY_TYPE.LIGHT;

    protected enum FACTORY_TYPE
    {
        LIGHT = 0,
        HEAVY,
        ANY
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        targetCost = squad.totalCost + money;
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (squad.totalCost >= targetCost)
            EndTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        Factory tempFactory = null;
        //Debug.Log(squad.GetSquadValue());
        //Debug.Log(squad.GetSquadLeader());
        //Debug.Log(_controller.FactoryList[0]);
        Vector3 squadPos = squad.GetSquadValue() > 0 ? squad.GetSquadLeader().transform.position : _controller.FactoryList[0].transform.position;
        foreach (Factory it in _controller.FactoryList)
        {
            if (it.CurrentState == Factory.State.Available
            &&  it.GetFactoryData.TypeId == (int)factoryType)
            {
                if (tempFactory == null)
                    factory = it;
                else if ((it.transform.position - squadPos).magnitude < (tempFactory.transform.position - squadPos).magnitude)
                    factory = it;
            }
        }
        if (factory != null)
            return true;
        return false;
    }

    public static bool HasToCompleteSquad(AIController _controller, int _id, float squadValue, float percentage)
    {
        if (squadValue <= _controller.taskDatas[_id].MilitaryPower.Evaluate((_controller.playerController.GetMilitaryPower() - _controller.GetMilitaryPower()) * percentage))
            return true;
        return false;
    }
} 

public class CreateExploSquadTask : CreateSquadTask
{
    new public static int id { get; private set; } = 2;

    public CreateExploSquadTask(Squad _squad)
    {
        squad = _squad;
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        int moneyTemp = money;
        while (moneyTemp > 0) // create squad with type 0 et 1 units with alocated money
        {
            if (moneyTemp > 1)
            {
                int newUnitCost = Random.Range(0, 2);
                factory.RequestUnitBuild(newUnitCost, this);
                moneyTemp -= newUnitCost + 1;
            }
            if (moneyTemp == 1)
            {
                factory.RequestUnitBuild(0, this);
                moneyTemp -= 1;
            }
        }
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (base.Evaluate(_controller, ref currentScore))
        {
            float score = _controller.taskDatas[id].Time.Evaluate(Time.time / 60.0f) * _controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints);
            if (score > currentScore)
            {
                money = Mathf.FloorToInt((_controller.TotalBuildPoints - 10) * 0.25f) + 2;
                targetCost = money + squad.totalCost;
                currentScore = score;
                return true;
            }
        }
        return false;
    }
}

public class CreateLAttackSquadTask : CreateSquadTask
{
    new public static int id { get; private set; } = 3;

    public CreateLAttackSquadTask(Squad _squad)
    {
        squad = _squad;
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        int moneyTemp = money;
        while (moneyTemp > 0) // create quad from alocated money
        {
            factory.RequestUnitBuild(2, this);
            moneyTemp -= 3;
        }
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (base.Evaluate(_controller, ref currentScore))
        {
            float score = _controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints) * _controller.taskDatas[id].Time.Evaluate(Time.time / 60.0f);
            if (score > currentScore)
            {
                money = Mathf.FloorToInt((_controller.TotalBuildPoints - 10) * 0.8f);
                money += 3 - money % 3;
                targetCost = money + squad.totalCost;
                currentScore = score;
                return true;
            }
        }
        return false;
    }
}

public class CreateHAttackSquadTask : CreateSquadTask
{
    new public static int id { get; private set; } = 4;

    public CreateHAttackSquadTask(Squad _squad)
    {
        squad = _squad;
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        int moneyTemp = money;
        while (moneyTemp > 0)
        {
            int newUnitCost = Random.Range(0, 2);
            factory.RequestUnitBuild(newUnitCost * 2 + 3, this);
            moneyTemp -= newUnitCost + 4;
        }
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        factoryType = FACTORY_TYPE.HEAVY;

        if (base.Evaluate(_controller, ref currentScore))
        {
            float score = _controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints) * _controller.taskDatas[id].Time.Evaluate(Time.time / 60.0f);
            if (score > currentScore)
            {
                money = Mathf.FloorToInt((_controller.TotalBuildPoints - 10) * 0.7f);
                targetCost = money + squad.totalCost;
                currentScore = score;
                return true;
            }
        }
        return false;
    }
}

public class CreateDefenseSquadTask : CreateSquadTask
{
    new public static int id { get; private set; } = 5;

    public CreateDefenseSquadTask(Squad _squad)
    {
        squad = _squad;
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        int moneyTemp = money;
        while (moneyTemp > 0)
        {
            factory.RequestUnitBuild(4, this);
            moneyTemp -= 5;
        }
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (base.Evaluate(_controller, ref currentScore))
        {
            float score = _controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints);
            if (score > currentScore)
            {
                money = Mathf.FloorToInt((_controller.TotalBuildPoints - 10) * 0.5f);
                money += 5 - money % 5;
                targetCost = money + squad.totalCost;
                currentScore = score;
                return true;
            }
        }
        return false;
    }
}

public class CreateMinerTask : StrategicTask
{
    public static int id { get; private set; } = 6;

    TargetBuilding targetBuilding = null;

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        int ownedTarget = 1;
        int ownedMine = 1;
        for (int i = 0; i < _controller.capturableTargets.transform.childCount; ++i)
        {
            TargetBuilding temp = _controller.capturableTargets.transform.GetChild(i).GetComponent<TargetBuilding>();
            if (temp.GetTeam() == _controller.GetTeam()) 
            {
                ++ownedTarget;
                if (temp.isProducingResources)
                    ++ownedMine;

                if (!temp.isProducingResources && temp.canProduceResources && !temp.isUpgrading && targetBuilding == null)
                    targetBuilding = temp;
                
                else if (!temp.isProducingResources && temp.canProduceResources && !temp.isUpgrading && (temp.gameObject.transform.position - _controller.FactoryList[0].transform.position).magnitude 
                        < (targetBuilding.gameObject.transform.position - _controller.FactoryList[0].transform.position).magnitude)
                {
                    targetBuilding = temp;
                }
            }
        }

        if (ownedTarget == 1 || targetBuilding == null)
            return false;

        float score = (_controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints)
                    *  _controller.taskDatas[id].Ratio.Evaluate(ownedTarget / ownedMine))
                    *  _controller.taskDatas[id].Time.Evaluate(Time.time / 60.0f);

        //Debug.Log("mine");
        //Debug.Log(score); 

        if (score > currentScore)
        {
            currentScore = score;
            return true;
        }
        return false;
    }
    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        targetBuilding.StartUpgrade();
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (targetBuilding.isProducingResources)
        {
            EndTask();
        }
    }
}

public class CreateFactoryTask : StrategicTask
{
    public static int id { get; private set; } = 7;

    Vector3 pos;

    int type = 0;

    Factory factory;

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (_controller.FactoryList.Count < 0)
            return false;

        Factory buildingFactory = null;
        for (int i = 0; i < _controller.FactoryList.Count; ++i) // get availible factory
        {
            if (_controller.FactoryList[i].CurrentState == Factory.State.Available)
            {
                buildingFactory = _controller.FactoryList[i];
                break;
            }
        }
        if (buildingFactory != null)
            _controller.SelectFactory(buildingFactory);
        else
            return false;
        
        float score = (_controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints) 
                    *  _controller.taskDatas[id].Ratio.Evaluate(_controller.FactoryList.Count / _controller.playerController.FactoryList.Count)) 
                    *  _controller.taskDatas[id].Time.Evaluate(Time.time / 60.0f);

        //Debug.Log("factory");
        //Debug.Log(score);

        if (score > currentScore)
        {
            if (_controller.GetHFactoryCount() / _controller.GetLFactoryCount() > 0.667f)
                type = 0;
            else
                type = 1;

            pos = Vector3.zero;

            Tile stratTile = null;

            List<Tile> ValueTile = new List<Tile>();

            foreach (Tile tile in Map.Instance.tilesWithBuild) // check for a set of best tiles
            {
                if (tile.GetTeam() == _controller.GetTeam())
                {
                    List<Tile> stratTiles = Map.Instance.GetTilesWithBuildAroundPoint(tile.position, 20.0f);

                    foreach (Tile it in stratTiles)
                        if (it.GetTeam() == _controller.GetTeam() && (it.buildType == E_BUILDTYPE.HEAVYFACTORY || it.buildType == E_BUILDTYPE.LIGHTFACTORY))
                            continue;
                    ValueTile.Add(tile);
                }
            }

            foreach (Tile tile in ValueTile) // check for THE best tile
            { 
                if (stratTile == null || tile.buildType <= stratTile.buildType
                || tile.buildType == stratTile.buildType && (tile.position - _controller.FactoryList[0].transform.position).magnitude < (stratTile.position - _controller.FactoryList[0].transform.position).magnitude)
                        stratTile = tile;
            }

            if (stratTile == null)
                return false;

            while (buildingFactory.CanPositionFactory(type, pos) == false)
                pos = stratTile.position + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * 15.0f;

            currentScore = score;
            return true;
        }
        return false;
    }
    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        factory = _controller.RequestFactoryBuild(type, pos);
        if (factory == null)
            EndTask();
        _controller.UnselectCurrentFactory();
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (factory.CurrentState == Factory.State.Available)
            EndTask();
    }
}

public class AttackTargetTask : StrategicTask
{
    public static int id { get; private set; } = 8;

    Tile targetTile = null;
    StrategicTask squadCreation = null;

    Vector3 rallyPoint;
    float checkIfEndInterval = 0.0f;

    public AttackTargetTask(Squad _squad)
    {
        squad = _squad;
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        float score = 0.0f;

        StrategicTask temp;

        // choose, if neened, what type of squad will complete the current to attack
        temp = new CreateLAttackSquadTask(squad);
        if (temp.Evaluate(_controller, ref score) && CreateSquadTask.HasToCompleteSquad(_controller, CreateLAttackSquadTask.id, squad.GetSquadValue(), 0.80f))
            squadCreation = temp;

        temp = new CreateHAttackSquadTask(squad);
        if (temp.Evaluate(_controller, ref score) && CreateSquadTask.HasToCompleteSquad(_controller, CreateHAttackSquadTask.id, squad.GetSquadValue(), 0.70f))
            squadCreation = temp;

        if (score <= 0.001)
            return false;

        if (squadCreation == null)
            rallyPoint = squad.GetSquadLeader().transform.position;
        else
            rallyPoint = (squadCreation as CreateSquadTask).factory.transform.position;

        E_BUILDTYPE tempType = E_BUILDTYPE.NOTHING;

        targetTile = null;

        foreach (Tile tile in Map.Instance.tilesWithBuild) // get the best target
        {
            if (tile.GetTeam() != _controller.GetTeam() && tile.buildType <= tempType)
            {
                if (targetTile == null)
                {
                    targetTile = tile;
                    continue;
                }
                else if (tile.buildType < tempType)
                {
                    targetTile = tile;
                }
                else if (tile.buildType == tempType 
                && (tile.position - rallyPoint).magnitude 
                < (targetTile.position - rallyPoint).magnitude)
                {
                    targetTile = tile;
                }
                tempType = targetTile.buildType;
            }
        }

        if (targetTile == null || score <= 0.001f)
            return false;

        // evaluate from time and distance to rallypoint
        score =  _controller.taskDatas[id].Time.Evaluate(Time.time / 60.0f) 
            * _controller.taskDatas[id].Distance.Evaluate((targetTile.position - rallyPoint).magnitude / Map.Instance.mapSize);

        //Debug.Log("attack");
        //Debug.Log(score);
        if (score > currentScore)
        {
            currentScore = score;
            return true;
        }
        return false;
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        if (squadCreation != null) 
            squadCreation.StartTask(_controller);
        else
            LaunchAttack();
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (squad == null)
        {
            Debug.Log("c pas bien jerem");
            return;
        }

        if (squadCreation != null)
        {
            squadCreation.UpdateTask();
            if (squadCreation.isComplete)
            {
                LaunchAttack();
                squadCreation = null;
            }
        }
        else // if squad complete, update attack : retreat if low ? complete squad ?
        {
            if (squad.GetSquadValue() == 0)
            {
                EndTask();
                return;
            }

            if (checkIfEndInterval < Time.time)
            {
                checkIfEndInterval = Time.time + 1.0f;
                if (targetTile.gameobject == null || targetTile.gameobject.GetComponent<BaseEntity>() == null)
                {
                    EndTask();
                    return;
                }
                else
                {
                    if (targetTile.buildType != E_BUILDTYPE.MINER)
                    {
                        if (!targetTile.gameobject.GetComponent<BaseEntity>().IsAlive)
                            EndTask();
                    }
                    else
                    {
                        if (targetTile.gameobject.GetComponent<TargetBuilding>().GetTeam() == controller.GetTeam())
                            EndTask();
                    }
                }
            }
        }
    }

    public void LaunchAttack()
    {
        checkIfEndInterval = Time.time;
        squad.SetMode(E_MODE.Agressive);
        if (targetTile.buildType != E_BUILDTYPE.MINER && targetTile.buildType != E_BUILDTYPE.CAPTUREPOINT)
            squad.SquadTaskAttack(targetTile.gameobject.GetComponent<BaseEntity>());
        else
            squad.CaptureTarget(targetTile.gameobject.GetComponent<TargetBuilding>());
    }
}

public class PlaceDefendUnitTask : StrategicTask
{
    public static int id { get; private set; } = 9;

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        float score = 0.0f;

        if (score > currentScore)
        {
            currentScore = score;
            return true;
        }
        return false;
    }
    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
    }
}

public class PlaceTurretTask : StrategicTask
{
    public static int id { get; private set; } = 10;

    Vector3 pos;

    Turret turret;

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (_controller.FactoryList.Count < 0)
            return false;

        Factory buildingFactory = null;
        for (int i = 0; i < _controller.FactoryList.Count; ++i) // get availible factory
        {
            if (_controller.FactoryList[i].CurrentState == Factory.State.Available)
            {
                buildingFactory = _controller.FactoryList[i];
                break;
            }
        }
        if (buildingFactory != null)
            _controller.SelectFactory(buildingFactory);
        else
            return false;

        float score = _controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints)
                    * _controller.taskDatas[id].EnemyPower.Evaluate(_controller.playerController.GetMilitaryPower() - _controller.GetMilitaryPower())
                    * _controller.taskDatas[id].Time.Evaluate(Time.time / 60.0f);

        //Debug.Log(score);

        if (score > currentScore)
        {
            pos = Vector3.zero;

            Tile stratTile = null;

            List<Tile> ValueTile = new List<Tile>();

            foreach (Tile tile in Map.Instance.tilesWithBuild) // check for a set of best tiles
            {
                if (tile.GetTeam() == _controller.GetTeam())
                {
                    List<Tile> stratTiles = Map.Instance.GetTilesWithBuildAroundPoint(tile.position, 20.0f);

                    foreach (Tile it in stratTiles)
                        if (it.GetTeam() == _controller.GetTeam() && it.buildType == E_BUILDTYPE.TURRET)
                            continue;
                    ValueTile.Add(tile);
                }
            }

            foreach (Tile tile in ValueTile) // check for THE best tile
            {
                if (stratTile == null || tile.buildType <= stratTile.buildType
                || tile.buildType == stratTile.buildType && (tile.position - _controller.FactoryList[0].transform.position).magnitude < (stratTile.position - _controller.FactoryList[0].transform.position).magnitude)
                    stratTile = tile;
            }

            if (stratTile == null)
                return false;

            while (buildingFactory.CanPositionTurret(pos) == false)
                pos = stratTile.position + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f)).normalized * 15.0f;

            currentScore = score;
            return true;
        }
        return false;
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        turret = _controller.RequestTurretBuild(pos);
        if (turret == null)
            EndTask();
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (!turret.isUnderConstruction)
            EndTask();
    }
}