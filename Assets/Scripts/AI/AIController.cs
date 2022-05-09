using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public sealed class AIController : UnitController
{
    [SerializeField] public GameObject capturableTargets;

    [SerializeField] public List<TaskData> taskDatas;

    public StrategicTask explorationTask = null;

    Squad explorationSquad = new Squad();

    [SerializeField] float timeBetweenUtilitySystemUpdate = 5.0f;
    float previousUtilitySystemTime = 0.0f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();


        explorationTask = new CreateExploSquadTask(explorationSquad);

        float t = 0;
        explorationTask.Evaluate(this, ref t);

        explorationTask.StartTask(this);

        previousUtilitySystemTime = Time.time;

    }
    
    void Update()
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

            tempTask = new CapturePointTask(explorationSquad);  
            if (tempTask.Evaluate(this, ref score))
                explorationTask = tempTask;

            Debug.Log(score);
            if (score > 0.0f)
                explorationTask.StartTask(this);
        }
    }
}
