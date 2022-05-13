using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public sealed class AIController : UnitController
{
    [SerializeField] public GameObject capturableTargets;

    [SerializeField] public List<TaskData> taskDatas;

    public PlayerController playerController { get; private set; }

    public StrategicTask explorationTask = null;
    public StrategicTask ecoTask = null;
    public StrategicTask militaryTask = null;

    Squad explorationSquad;
    Squad militarySquad1;
    Squad militarySquad2;

    [SerializeField] float timeBetweenUtilitySystemUpdate = 5.0f;
    float previousUtilitySystemTime = 0.0f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        militarySquad1 = new Squad(this);
        militarySquad2 = new Squad(this);
        explorationSquad = new Squad(this);

        previousUtilitySystemTime = Time.time;

        playerController = FindObjectOfType<PlayerController>();
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
        if (ecoTask != null && !ecoTask.isComplete)
            ecoTask.UpdateTask();
        if (militaryTask != null && !militaryTask.isComplete)
            militaryTask.UpdateTask();
    }

    void TasksUtilitySystemUpdate()
    {
        ActualizeExplorationTask();
        ActualizeEcoTask();
        ActualizeMilitaryTask();
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

            if (score > 0.0f)
                explorationTask.StartTask(this);
        }
    }

    void ActualizeEcoTask()
    {
        if (ecoTask == null || ecoTask.isComplete)
        {
            StrategicTask tempTask;
            float score = 0.0f;

            tempTask = new CreateFactoryTask();
            if (tempTask.Evaluate(this, ref score))
                ecoTask = tempTask;


            if (score > 0.0f)
                ecoTask.StartTask(this);
        }
    }

    void ActualizeMilitaryTask()
    {
        if (militaryTask == null || militaryTask.isComplete)
        {
            StrategicTask tempTask;
            float score = 0.0f;

            if (score > 0.0f)
                militaryTask.StartTask(this);
        }
    }
}
