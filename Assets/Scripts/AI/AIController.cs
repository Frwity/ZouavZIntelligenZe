using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public sealed class AIController : UnitController
{
    [SerializeField] GameObject CapturableTargets;

    public StrategicTask explorationTask = null;

    List<Unit> ExplorationSquadPlaceHolder = new List<Unit>();

    [SerializeField] float timeBetweenUtilitySystemUpdate = 5.0f;
    float previousUtilitySystemTime = 0.0f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        explorationTask = new CreateSquadTask(ExplorationSquadPlaceHolder);

        explorationTask.StartTask(this);

        previousUtilitySystemTime = Time.time;
    }

    protected override void Update()
    {
        base.Update();
        UpdateTask();

        if (previousUtilitySystemTime + timeBetweenUtilitySystemUpdate < Time.time)
        {
            previousUtilitySystemTime = Time.time;
            TasksUtilitySystemUpdate();
        }
    }

    void UpdateTask()
    {
        if (explorationTask != null && !explorationTask.isComplete)
            explorationTask.UpdateTask();
    }

    void TasksUtilitySystemUpdate()
    {
        ActualizeExplorationTask();
    }

    void ActualizeExplorationTask()
    {
        if (explorationTask == null || explorationTask.isComplete)
        {
            int captureIndex = int.MaxValue;
            float distance = float.MaxValue;
            for (int i = 0; i < CapturableTargets.transform.childCount; ++i)
            {
                float tempdist = (CapturableTargets.transform.GetChild(i).position - FactoryList[0].transform.position).magnitude;
                if (CapturableTargets.transform.GetChild(i).GetComponent<TargetBuilding>().GetTeam() == ETeam.Neutral && tempdist < distance)
                {
                    captureIndex = i;
                    distance = tempdist;
                }
            }
            if (captureIndex != int.MaxValue)
            {
                explorationTask = new CapturePointTask(ExplorationSquadPlaceHolder, CapturableTargets.transform.GetChild(captureIndex).GetComponent<TargetBuilding>());
                explorationTask.StartTask(this);
            }
        }
    }
}
