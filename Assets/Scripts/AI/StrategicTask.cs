using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrategicTask
{
    public bool isComplete = false;
    protected UnitController controller;
    protected List<Unit> squad;

    //public virtual int GetId() = 0;

    public void SetUnit(Unit _unit)
    {
        squad.Add(_unit);
    }

    public virtual void StartTask(UnitController _controller)
    {
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
    //static int id = 0;

    public CreateSquadTask(List<Unit> ExplorationSquadPlaceHolder)
    {
        squad = ExplorationSquadPlaceHolder;
    }

    public override void StartTask(UnitController _controller)
    {
        base.StartTask(_controller);
        _controller.FactoryList[0].RequestUnitBuild(0, this);
    }
    public override void UpdateTask()
    {
        base.UpdateTask();
        if (squad != null)
            EndTask();
    }
}

public class CapturePointTask : StrategicTask
{
    TargetBuilding targetBuilding;

    bool isCapturing = false;

    public CapturePointTask(List<Unit> _ExplorationSquadPlaceHolder, TargetBuilding _targetBuilding)
    {
        targetBuilding = _targetBuilding;
        squad = _ExplorationSquadPlaceHolder;
    }

    public override void StartTask(UnitController _controller)
    {
        base.StartTask(_controller);
        
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
            else
                squad[0].SetTargetPos(targetBuilding.transform.position);
        }
        if (targetBuilding.GetTeam() == controller.GetTeam())
            EndTask();
    }

    public override void EndTask()
    {
        base.EndTask();
    }
}