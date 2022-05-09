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
// each diffenrent type of squad will be creating with hard code for a better control over the randomness
public class CreateSquadTask : StrategicTask 
{
    public static int id { get; private set; } = 0;

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
    public static int id { get; private set; } = 2;

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
                moneyTemp -= newUnitCost;
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

            if (score > currentScore)
            {
                currentScore = score;
                return true;
            }
        }
        return false;
    }
}

public class CapturePointTask : StrategicTask
{
    public static int id { get; private set; } = 1;

    TargetBuilding targetBuilding;

    bool isCapturing = false;
    bool isWalking = false;

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
            score = (_controller.taskDatas[id].Distance.Evaluate(distance) + _controller.taskDatas[id].Ratio.Evaluate(0)) * _controller.taskDatas[id].Time.Evaluate(Time.time);
        }

        if (score > currentScore)
        {
            currentScore = score;
            return true;
        }
        return false;
    }

    public override void UpdateTask()
    {
                Debug.Log(squad);
        base.UpdateTask();
        if (squad == null)
            return;
        if (!isCapturing)
        {
            if (squad.members[0].CanCapture(targetBuilding))
            {
                squad.members[0].StartCapture(targetBuilding);
                isCapturing = true;
            }
            else if (!isWalking)
            {
                isWalking = true;
                squad.CreateSquadFormation(targetBuilding.transform.position);
            }
        }
        if (targetBuilding.GetTeam() == controller.GetTeam())
            EndTask();
    }
}