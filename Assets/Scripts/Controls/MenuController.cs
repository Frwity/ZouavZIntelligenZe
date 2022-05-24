﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField]
    Transform FactoryMenuCanvas = null;
    public GraphicRaycaster BuildMenuRaycaster { get; private set; }

    UnitController Controller = null;
    GameObject FactoryMenuPanel = null;
    Text BuildPointsText = null;
    Text CapturedTargetsText = null;
    Button[] BuildUnitButtons = null;
    Button[] BuildFactoryButtons = null;
    Button BuildTurretButton = null;
    Button CancelBuildButton = null;
    Text[] BuildQueueTexts = null;
    
    [SerializeField]
    GameObject SquadMenuPanel = null;
    Text SquadCurrentMode = null;
    
    [SerializeField]
    GameObject ProduceResourcesMenuPanel = null;
    GameObject produceResourcesButton = null;
    GameObject produceResourcesText = null;

    public void HideFactoryMenu()
    {
        if (FactoryMenuPanel)
            FactoryMenuPanel.SetActive(false);
    }
    public void ShowFactoryMenu()
    {
        if (FactoryMenuPanel)
            FactoryMenuPanel.SetActive(true);
    }
    public void HideSquadMenu()
    {
        if (SquadMenuPanel)
            SquadMenuPanel.SetActive(false);
    }
    public void ShowSquadMenu()
    {
        if (SquadMenuPanel)
            SquadMenuPanel.SetActive(true);
    }
    public void HideProduceResourcesMenu()
    {
        if (ProduceResourcesMenuPanel)
            ProduceResourcesMenuPanel.SetActive(false);

        produceResourcesButton.GetComponent<Button>().onClick.RemoveAllListeners();
    }
    public void ShowProduceResourcesMenu()
    {
        if (ProduceResourcesMenuPanel)
            ProduceResourcesMenuPanel.SetActive(true);
    }
    public void SetSquadText(E_MODE mode)
    {
        SquadCurrentMode.text = "Current Mode : " + Enum.GetName(typeof(E_MODE), mode);
    }
    public void UpdateBuildPointsUI()
    {
        if (BuildPointsText != null)
            BuildPointsText.text = "Build Points : " + Controller.TotalBuildPoints;
    }
    public void UpdateCapturedTargetsUI()
    {
        if (CapturedTargetsText != null)
            CapturedTargetsText.text = "Captured Targets : " + Controller.CapturedTargets;
    }
    public void UpdateFactoryBuildQueueUI(int i, Factory selectedFactory)
    {
        if (selectedFactory == null)
            return;
        int queueCount = selectedFactory.GetQueuedCount(i);
        if (queueCount > 0)
        {
            BuildQueueTexts[i].text = "+" + queueCount;
            BuildQueueTexts[i].enabled = true;
        }
        else
        {
            BuildQueueTexts[i].enabled = false;
        }
    }
    public void HideAllFactoryBuildQueue()
    {
        foreach (Text text in BuildQueueTexts)
        {
            if (text)
                text.enabled = false;
        }
    }
    public void UnregisterBuildButtons(int availableUnitsCount, int availableFactoriesCount)
    {
        // unregister build buttons
        for (int i = 0; i < availableUnitsCount; i++)
        {
            BuildUnitButtons[i].onClick.RemoveAllListeners();
        }
        for (int i = 0; i < availableFactoriesCount; i++)
        {
            BuildFactoryButtons[i].onClick.RemoveAllListeners();
        }
        BuildTurretButton.onClick.RemoveAllListeners();
    }

    public void UpdateFactoryMenu(Factory selectedFactory, int selectedFactoryCount, Func<int, Factory, bool> requestUnitBuildMethod, Action<int> enterFactoryBuildModeMethod, Action enterTurretBuildModeMethod)
    {
        ShowFactoryMenu();

        // Unit build buttons
        // register available buttons
        int i = 0;
        for (; i < selectedFactory.AvailableUnitsCount; i++)
        {
            BuildUnitButtons[i].gameObject.SetActive(true);

            int index = i; // capture index value for event closure
            BuildUnitButtons[i].onClick.AddListener(() =>
            {
                if (requestUnitBuildMethod(index, selectedFactory))
                    UpdateFactoryBuildQueueUI(index, selectedFactory);
            });

            Text[] buttonTextArray = BuildUnitButtons[i].GetComponentsInChildren<Text>();
            Text buttonText = buttonTextArray[0];//BuildUnitButtons[i].GetComponentInChildren<Text>();
            UnitDataScriptable data = selectedFactory.GetBuildableUnitData(i);
            buttonText.text = data.Caption + "(" + data.Cost + ")";

            // Update queue count UI
            BuildQueueTexts[i] = buttonTextArray[1];
            UpdateFactoryBuildQueueUI(i, selectedFactory);
        }
        // hide remaining buttons
        for (; i < BuildUnitButtons.Length; i++)
        {
            BuildUnitButtons[i].gameObject.SetActive(false);
        }

        // activate Cancel button
        CancelBuildButton.onClick.AddListener(  () =>
                                                {
                                                    selectedFactory?.CancelCurrentBuild();
                                                    HideAllFactoryBuildQueue();
                                                });

        // Factory build buttons
        // register available buttons
        i = 0;
        if (selectedFactoryCount < 2)
        {
            for (; i < selectedFactory.AvailableFactoriesCount; i++)
            {
                BuildFactoryButtons[i].gameObject.SetActive(true);

                int index = i; // capture index value for event closure
                BuildFactoryButtons[i].onClick.AddListener(() =>
                {
                    enterFactoryBuildModeMethod(index);
                });

                Text buttonText = BuildFactoryButtons[i].GetComponentInChildren<Text>();
                FactoryDataScriptable data = selectedFactory.GetBuildableFactoryData(i);
                buttonText.text = data.Caption + "(" + data.Cost + ")";
            }
        }
        // hide remaining buttons
        for (; i < BuildFactoryButtons.Length; i++)
        {
            BuildFactoryButtons[i].gameObject.SetActive(false);
        }

        BuildTurretButton.gameObject.SetActive(true);
        BuildTurretButton.onClick.AddListener(() =>
        {
            enterTurretBuildModeMethod();
        });
    }

    public void UpdateProduceResourcesMenu(TargetBuilding target)
    {
        ShowProduceResourcesMenu();
        if (target)
        {
            produceResourcesText.SetActive(target.isProducingResources);
            produceResourcesButton.SetActive(!target.isProducingResources);
            produceResourcesButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (Controller.TotalBuildPoints >= 8)
                {
                    Controller.TotalBuildPoints -= 8;
                    target.StartUpgrade();
                    produceResourcesButton.SetActive(false);
                    produceResourcesText.SetActive(true);
                }
            });
        }
    }
    void Awake()
    {
        if (FactoryMenuCanvas == null)
        {
            Debug.LogWarning("FactoryMenuCanvas not assigned in inspector");
        }
        else
        {
            Transform FactoryMenuPanelTransform = FactoryMenuCanvas.Find("FactoryMenu_Panel");
            if (FactoryMenuPanelTransform)
            {
                FactoryMenuPanel = FactoryMenuPanelTransform.gameObject;
                FactoryMenuPanel.SetActive(false);
            }
            BuildMenuRaycaster = FactoryMenuCanvas.GetComponent<GraphicRaycaster>();
            Transform BuildPointsTextTransform = FactoryMenuCanvas.Find("BuildPointsText");
            if (BuildPointsTextTransform)
            {
                BuildPointsText = BuildPointsTextTransform.GetComponent<Text>();
            }
            Transform CapturedTargetsTextTransform = FactoryMenuCanvas.Find("CapturedTargetsText");
            if (CapturedTargetsTextTransform)
            {
                CapturedTargetsText = CapturedTargetsTextTransform.GetComponent<Text>();
            }
        }
        if (SquadMenuPanel == null)
            Debug.LogWarning("SquadMenuPanel not assigned in inspector");
        else
        {
            SquadCurrentMode = SquadMenuPanel.transform.Find("CurrentMode").GetComponent<Text>();
            SquadMenuPanel.SetActive(false);
        }

        if (ProduceResourcesMenuPanel != null)
            ProduceResourcesMenuPanel.SetActive(false);

    Controller = GetComponent<UnitController>();
    }
    void Start()
    {
        BuildUnitButtons = FactoryMenuPanel.transform.Find("BuildUnitMenu_Panel").GetComponentsInChildren<Button>();
        BuildFactoryButtons = FactoryMenuPanel.transform.Find("BuildFactoryMenu_Panel").GetComponentsInChildren<Button>();
        BuildTurretButton = FactoryMenuPanel.transform.Find("BuildTurretMenu_Panel").GetComponentInChildren<Button>();
        CancelBuildButton = FactoryMenuPanel.transform.Find("Cancel_Button").GetComponent<Button>();
        produceResourcesButton = ProduceResourcesMenuPanel.transform.Find("Button").gameObject;
        produceResourcesText = ProduceResourcesMenuPanel.transform.Find("Already Upgraded").gameObject;
        BuildQueueTexts = new Text[BuildUnitButtons.Length];
    }
}