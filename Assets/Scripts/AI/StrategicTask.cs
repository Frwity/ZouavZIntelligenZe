using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StrategicTask
{
    public bool isComplete = false;
    protected AIController controller;
    protected Squad squad;
    [SerializeField] protected TaskData taskDate;

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
            int ownedTarget = 1;
            int enemyTarget = 1;

            for (int i = 0; i < _controller.capturableTargets.transform.childCount; ++i)
            {
                if (_controller.capturableTargets.transform.GetChild(i).GetComponent<TargetBuilding>().GetTeam() == _controller.GetTeam())
                    ++ownedTarget;
                else if (_controller.capturableTargets.transform.GetChild(i).GetComponent<TargetBuilding>().GetTeam() == _controller.playerController.GetTeam())
                    ++enemyTarget;
            }
            score = (_controller.taskDatas[id].Distance.Evaluate(distance) + _controller.taskDatas[id].Ratio.Evaluate(((float)enemyTarget / ownedTarget) <= 0.01f ? 0.1f : ((float)enemyTarget / ownedTarget))) * _controller.taskDatas[id].Time.Evaluate(Time.time);
        }

        if (squad.GetSquadValue() <= Mathf.FloorToInt(Time.time / 60.0f))
        {
            squadCreation = new CreateExploSquadTask(squad);
            float f = 0.0f;
            squadCreation.Evaluate(_controller, ref f);
        }


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
            squad.CaptureTarget(targetBuilding);
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (squad == null || squad.GetSquadValue() == 0)
        {
            EndTask();
            return;
        }

        if (squadCreation != null)
        {
            squadCreation.UpdateTask();
            if (squadCreation.isComplete)
            {
                squad.CaptureTarget(targetBuilding);
                squadCreation = null;
            }
        }
        else
        {
            squad.UpdateSquad();

            if (targetBuilding.GetTeam() == controller.GetTeam())
                EndTask();
        }
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
    protected int targetCost;
    public Factory factory = null;

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        targetCost = squad.totalCost + money;
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        for (int i = 0; i < _controller.FactoryList.Count; ++i)
        {
            if (_controller.FactoryList[i].CurrentState == Factory.State.Available)
            {
                factory = _controller.FactoryList[i];
                break;
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

    public override void StartTask(AIController _controller) // TODO change squad mode
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
        if (squad.totalCost >= targetCost)
            EndTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (base.Evaluate(_controller, ref currentScore))
        {
            float score = _controller.taskDatas[id].Time.Evaluate(Time.time) * _controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints);
            if (score > currentScore)
            {
                money = Mathf.FloorToInt((_controller.TotalBuildPoints - 10) * 0.25f);
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
        if (squad.totalCost >= targetCost)
            EndTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (base.Evaluate(_controller, ref currentScore))
        {
            float score = _controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints);
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
        while (moneyTemp > 0) // TODO 
        {
            factory.RequestUnitBuild(2, this);
            moneyTemp -= 3;
        }
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (squad.totalCost >= targetCost)
            EndTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (base.Evaluate(_controller, ref currentScore))
        {
            float score = _controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints);
            if (score > currentScore) // TODO
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

public class CreateFactoryTask : StrategicTask
{
    public static int id { get; private set; } = 6;

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
                    *  _controller.taskDatas[id].Time.Evaluate(Time.time);

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
    public static int id { get; private set; } = 7;

    Tile targetTile = null;

    StrategicTask squadCreation = null;

    public AttackTargetTask(Squad _squad)
    {
        squad = _squad;
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        float score = 0.0f;
        if (_controller.taskDatas[CreateHAttackSquadTask.id].Time.Evaluate(Time.time) > _controller.taskDatas[CreateHAttackSquadTask.id].Time.Evaluate(Time.time)) // choose what types of squad will attack
            if (CreateSquadTask.HasToCompleteSquad(_controller, CreateLAttackSquadTask.id, squad.GetSquadValue(), 0.80f))
                squadCreation = new CreateLAttackSquadTask(squad);
        else
            if (CreateSquadTask.HasToCompleteSquad(_controller, CreateHAttackSquadTask.id, squad.GetSquadValue(), 0.70f))
                squadCreation = new CreateHAttackSquadTask(squad);

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
                && (tile.position - (squadCreation as CreateLAttackSquadTask).factory.transform.position).magnitude 
                < (targetTile.position - (squadCreation as CreateLAttackSquadTask).factory.transform.position).magnitude)
                {
                    targetTile = tile;
                }
                tempType = targetTile.buildType;
            }
        }

        if (targetTile == null || score <= 0.001f)
            return false;

        score *=  _controller.taskDatas[id].Time.Evaluate(Time.time) 
                * _controller.taskDatas[id].Distance.Evaluate((targetTile.position - (squadCreation as CreateLAttackSquadTask).factory.transform.position).magnitude);

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
            return;

        if (squadCreation != null)
        {
            squadCreation.UpdateTask();
            if (squadCreation.isComplete)
            {
                LaunchAttack();
                squadCreation = null;
            }
        }
        else // if squad complete, update the attack
        {
            
        }
    }

    public void LaunchAttack()
    {
        if (targetTile.buildType != E_BUILDTYPE.MINER)
            squad.SquadTaskAttack(targetTile.gameobject.GetComponent<BaseEntity>());
        else
            squad.CaptureTarget(targetTile.gameobject.GetComponent<TargetBuilding>());
    }
}

public class PlaceDefendUnitTask : StrategicTask
{
    public static int id { get; private set; } = 8;

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