using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public sealed class AIController : UnitController
{
    [SerializeField] public GameObject capturableTargets;

    [SerializeField] public List<TaskData> taskDatas;

    public PlayerController playerController { get; private set; }

    public StrategicTask task1 = null;
    public StrategicTask task2 = null;

    Squad explorationSquad;
    Squad militarySquad1;
    Squad militarySquad2;

    [SerializeField] float timeBetweenUtilitySystemUpdate = 5.0f;
    float previousUtilitySystemTime1 = 0.0f;
    float previousUtilitySystemTime2 = 0.0f;

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

        previousUtilitySystemTime1 = Time.time;
        previousUtilitySystemTime2 = Time.time + timeBetweenUtilitySystemUpdate / 2.0f;

        playerController = FindObjectOfType<PlayerController>();
    }

    protected override void Update()
    {
        base.Update();
        UpdateTasks();

        if (previousUtilitySystemTime1 + timeBetweenUtilitySystemUpdate < Time.time)
        {
            previousUtilitySystemTime1 = Time.time;
            //    Debug.Log("1-------------");
            UtilitySystemUpdate(ref task1, 0.1f);
            //    Debug.Log("1-------------");

        }
        if (previousUtilitySystemTime2 + timeBetweenUtilitySystemUpdate < Time.time)
        {
            previousUtilitySystemTime2 = Time.time + timeBetweenUtilitySystemUpdate / 2.0f;
            //    Debug.Log("2-------------");
            UtilitySystemUpdate(ref task2, 0.2f);
            //    Debug.Log("2-------------");
        }
    }

    void UpdateTasks()
    {
        if (task1 != null && !task1.isComplete)
            task1.UpdateTask();
        if (task2 != null && !task2.isComplete)
            task2.UpdateTask();
    }

    void UtilitySystemUpdate(ref StrategicTask task, float scoreThreshold)
    {
        if (task == null || task.isComplete)
        {
            StrategicTask tempTask;
            float score = scoreThreshold;

            if (explorationSquad.State == E_TASK_STATE.Free)
            {
                tempTask = new CapturePointTask(explorationSquad);
                if (tempTask.Evaluate(this, ref score))
                    task = tempTask;
            }

            tempTask = new CreateFactoryTask();
            if (tempTask.Evaluate(this, ref score))
                task = tempTask;

            tempTask = new CreateMinerTask();
            if (tempTask.Evaluate(this, ref score))
                task = tempTask;

            if (IsSquadAvailible())
            {
                tempTask = new AttackTargetTask(GetRandomSquad());
                if (tempTask.Evaluate(this, ref score))
                    task = tempTask;
            }

            if (score > scoreThreshold)
                task.StartTask(this);
        }
    }

    Squad GetRandomSquad()
    {
        if (!IsSquadAvailible())
            return null;

        int random = Random.Range(0, 3);

        // no while because of very few cases, could be better with

        if (random++ == 0 && militarySquad1.State == E_TASK_STATE.Free)
            return militarySquad1;
        if (random++ == 1 && militarySquad2.State == E_TASK_STATE.Free)
            return militarySquad2;
        if (random == 2 && explorationSquad.State == E_TASK_STATE.Free)
            return explorationSquad;
        else
            random = 0;
        if (random++ == 0 && militarySquad1.State == E_TASK_STATE.Free)
            return militarySquad1;
        if (random == 1 && militarySquad2.State == E_TASK_STATE.Free)
            return militarySquad2;

        return null;
    }

    bool IsSquadAvailible()
    {
        if (militarySquad1.State == E_TASK_STATE.Free || militarySquad2.State == E_TASK_STATE.Free || explorationSquad.State == E_TASK_STATE.Free)
            return true;
        return false;
    }
}
