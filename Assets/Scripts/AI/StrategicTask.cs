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
            score = (_controller.taskDatas[id].Distance.Evaluate(distance) + _controller.taskDatas[id].Ratio.Evaluate(0)) * _controller.taskDatas[id].Time.Evaluate(Time.time); //TODO ratio evaluate
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
        squad.CaptureTarget(targetBuilding);
    }

    public override void UpdateTask()
    {
        base.UpdateTask();
        if (squad == null)
            return;

        squad.UpdateSquad();

        if (targetBuilding.GetTeam() == controller.GetTeam())
            EndTask();
    }
}

// each diffenrent type of squad will be creating with hard code for a better control over the randomness
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
        float score = 0.0f;

        if (base.Evaluate(_controller, ref currentScore))
        {
             
            targetCost = money;
            if (score > currentScore)
            {
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
        float score = 0.0f;

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

        score = (_controller.taskDatas[id].Resources.Evaluate(_controller.TotalBuildPoints) 
        * _controller.taskDatas[id].Ratio.Evaluate(_controller.FactoryList.Count / _controller.playerController.FactoryList.Count)) 
        * _controller.taskDatas[id].Time.Evaluate(Time.time);


        if (score > currentScore)
        {
            if (_controller.GetHFactoryCount() / _controller.GetLFactoryCount() > 0.667f)
                type = 0;
            else
                type = 1;

            pos = Vector3.zero;

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