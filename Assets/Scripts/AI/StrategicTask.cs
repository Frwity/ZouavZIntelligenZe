using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class StrategicTask
{
    public bool isComplete = false;
    protected AIController controller;
    protected List<Unit> squad;
    [SerializeField] protected TaskData taskDate;

    public abstract bool Evaluate(AIController _controller, ref float currentScore);

    public void SetUnit(Unit _unit)
    {
        squad.Add(_unit);
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

public class CreateSquadTask : StrategicTask
{
    public static int id { get; private set; } = 0;

    public CreateSquadTask(List<Unit> ExplorationSquadPlaceHolder)
    {
        squad = ExplorationSquadPlaceHolder;
    }

    public override void StartTask(AIController _controller)
    {
        base.StartTask(_controller);
        if (_controller.FactoryList[0] != null)
            _controller.FactoryList[0].RequestUnitBuild(0, this);
    }
    public override void UpdateTask()
    {
        base.UpdateTask();
        if (squad != null)
            EndTask();
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        return false;
    }
}

public class CapturePointTask : StrategicTask
{
    public static int id { get; private set; } = 1;

    TargetBuilding targetBuilding;

    bool isCapturing = false;
    bool isWalking = false;

    public CapturePointTask(List<Unit> _ExplorationSquadPlaceHolder)
    {
        squad = _ExplorationSquadPlaceHolder;
    }

    public override bool Evaluate(AIController _controller, ref float currentScore)
    {
        float score = 0.0f;

        int captureIndex = int.MaxValue;
        float distance = float.MaxValue;

        for (int i = 0; i < _controller.CapturableTargets.transform.childCount; ++i)
        {
            float tempdist = (_controller.CapturableTargets.transform.GetChild(i).position - _controller.FactoryList[0].transform.position).magnitude;
            if (_controller.CapturableTargets.transform.GetChild(i).GetComponent<TargetBuilding>().GetTeam() == ETeam.Neutral && tempdist < distance)
            {
                captureIndex = i;
                distance = tempdist;
            }
        }
        if (captureIndex != int.MaxValue)
        {
            targetBuilding = _controller.CapturableTargets.transform.GetChild(captureIndex).GetComponent<TargetBuilding>();
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
        base.UpdateTask();
        if (squad == null)
            return;
        if (!isCapturing)
        {
            if (squad[0].CanCapture(targetBuilding))
            {
                squad[0].StartCapture(targetBuilding);
                isCapturing = true;
            }
            else if (!isWalking)
            {
                isWalking = true;
                squad[0].SetTargetPos(targetBuilding.transform.position);
            }
        }
        if (targetBuilding.GetTeam() == controller.GetTeam())
            EndTask();
    }
}