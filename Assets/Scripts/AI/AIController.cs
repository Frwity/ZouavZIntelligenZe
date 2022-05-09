using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public sealed class AIController : UnitController
{
    [SerializeField] public GameObject CapturableTargets;

    [SerializeField] public List<TaskData> taskDatas;

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
            StrategicTask tempTask;
            float score = 0.0f;

            tempTask = new CapturePointTask(ExplorationSquadPlaceHolder);  
            if (tempTask.Evaluate(this, ref score))
                explorationTask = tempTask;


            if (score > 0.0f)
                explorationTask.StartTask(this);
        }
    }
}
