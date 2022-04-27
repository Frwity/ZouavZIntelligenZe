using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrategicTask
{
    public bool isComplete = false;
    UnitController controller;

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
    Unit unit;
    public CreateSquadTask(ref Unit ExplorationSquadPlaceHolder)
    {
        unit = ExplorationSquadPlaceHolder;
        Debug.Log(unit);
    }

    public override void StartTask(UnitController _controller)
    {
        base.StartTask(_controller);
        Debug.Log(unit);
        _controller.FactoryList[0].RequestUnitBuild(0);
    }
    public override void UpdateTask()
    {
        base.UpdateTask();
        if (unit != null)
        {
            EndTask();
            Debug.Log("end");
        }
    }
}

public class CapturePointTask : StrategicTask
{
    Unit unit;
    TargetBuilding targetBuilding;

    bool isCapturing = false;

    public CapturePointTask(Unit _ExplorationSquadPlaceHolder, TargetBuilding _targetBuilding)
    {
        targetBuilding = _targetBuilding;
        unit = _ExplorationSquadPlaceHolder;
    }

    public override void StartTask(UnitController _controller)
    {
        base.StartTask(_controller);
        
    }

    public override void UpdateTask()
    {
        Debug.Log(unit);
        base.UpdateTask();
        if (unit == null)
            return;
        Debug.Log(unit);

        Debug.Log("aze");

        if (!isCapturing)
        {
            if (unit.CanCapture(targetBuilding))
            {
                unit.StartCapture(targetBuilding);
                isCapturing = true;
            }
            else
                unit.SetTargetPos(targetBuilding.transform.position);
        }
    }

    public override void EndTask()
    {
        base.EndTask();
    }
}