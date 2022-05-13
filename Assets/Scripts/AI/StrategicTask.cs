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
            score = (_controller.taskDatas[id].Distance.Evaluate(distance) + _controller.taskDatas[id].Ratio.Evaluate(((float)enemyTarget / ownedTarget) <= 0.01f ? 0.1f : ((float)enemyTarget / ownedTarget))) * _controller.taskDatas[id].Time.Evaluate(Time.time); //TODO ratio evaluate
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
        if (squad == null)
            return;

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
    protected Factory factory = null;

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
        while (moneyTemp > 0)
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
        if (squad.totalCost == targetCost)
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

public class CreateFactoryTask : StrategicTask
{
    public static int id { get; private set; } = 3;

    Vector3 pos;

    int type = 0;

    Factory factory;

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        if (_controller.FactoryList.Count < 0)
            return false;

        Factory buildingFactory = null;
        for (int i = 0; i < _controller.FactoryList.Count; ++i)
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
        * _controller.taskDatas[id].Ratio.Evaluate(_controller.FactoryList.Count / _controller.playerController.FactoryList.Count)) 
        * _controller.taskDatas[id].Time.Evaluate(Time.time);

        if (score > currentScore)
        {
            if (_controller.GetHFactoryCount() / _controller.GetLFactoryCount() > 0.667f)
                type = 0;
            else
                type = 1;
            Debug.Log(type);

            pos = Vector3.zero;

            Tile stratTile = null;

            List<Tile> ValueTile = new List<Tile>();

            foreach (Tile tile in Map.Instance.tilesWithBuild)
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

            foreach (Tile tile in ValueTile)
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
    public static int id { get; private set; } = 4;

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

public class PlaceDefendUnitTask : StrategicTask
{
    public static int id { get; private set; } = 5;

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