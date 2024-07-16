using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

using Random = System.Random;

public class Game : MonoBehaviour
{
    public static Game Instance;

    public TMP_Text levelLabel;
    public TMP_Text countdown;
    public TMP_Text timerLabel;
    public TMP_Text objectives;
    public TMP_Text commission;
    public GameObject carryInfo;
    public GameObject focusSwitchInfo;
    public GameObject wrongTool;
    public GameObject rightTool;

    // UI
    public TMP_Text loadedSave;
    public GameObject gameSavedConfirmation;
    public GameObject stoppedScreen;
    public GameObject pauseMenu;
    public GameObject winMenu;

    // Audio
    public AudioSource rightItemSound;
    public AudioSource wrongItemSound;
    public AudioSource successSound;

    // Game Objects
    public GameObject spawnAreas;
    public GameObject toolCrate;
    public List<GameObject> toolPlanes;

    private float timerTime = 0.0f;
    private bool isPaused = true;

    private int levelObjTotal;
    private Dictionary<int, int> levelsGoals = new Dictionary<int, int>
    {
        { -1, 5 }, // DEBUG
        { 1, 2 },
        { 2, 4 },
        { 3, 6 },
        { 4, 8 },
        { 5, 10 },
    };

    private List<string> remainingTools;
    private string activeCommission;
    private int deliveredCommissions = 0;

    // DEBUG
    private bool DEBUG = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // START AND UPDATE
    void Start()
    {
        // Setup HUD
        loadedSave.text = "Loaded Save:\n" + CurrentGame.Instance.gameName;
        levelLabel.text = "Level " + CurrentGame.Instance.playLevel;
        levelObjTotal = levelsGoals[CurrentGame.Instance.playLevel];
        objectives.text = "Collected " + deliveredCommissions + "/" + levelObjTotal;

        // Setup tools
        Random rand = new Random();
        Transform[] tables = spawnAreas.GetComponentsInChildren<Transform>();
        List<Transform> tableList = new List<Transform>();
        foreach(Transform table in tables)
        {
            if (table.name.StartsWith("Table-"))
                tableList.Add(table);
        }

        // All the tools to be chosen from
        if (DEBUG)
            Debug.Log("There are " + levelObjTotal + " commissions to do in the level");

        int toolsToPlaceAmount = Math.Min((levelObjTotal + 1) * 2, 20);
        List<GameObject> toolsToPlace = toolPlanes.OrderBy(x => rand.Next()).Take(toolsToPlaceAmount).ToList();
        List<string> toolsNameToPlace = new List<string>();

        // Place tools on random tables
        Dictionary<int, List<List<float>>> tableTakenSpace = new Dictionary<int, List<List<float>>>();
        if (DEBUG)
            Debug.Log("There are " + toolsToPlaceAmount + " tools to be placed");

        for (int i = 0; i < toolsToPlaceAmount; i++)
        {
            GameObject tool = toolsToPlace[i];
            string toolName = tool.name.Replace("Tool-", "");
            toolsNameToPlace.Add(toolName);
            int tableIndex;
            float xPlacement;
            bool invalidPlacement;
            do
            {
                // Choose Random table
                tableIndex = rand.Next(tableList.Count);

                // Choose Random position
                xPlacement = ((rand.Next(160) - 80) / 100.0f);

                // Check if position is valid
                invalidPlacement = false;
                if (tableTakenSpace.ContainsKey(tableIndex))
                {
                    foreach (List<float> takenSpace in tableTakenSpace[tableIndex])
                        invalidPlacement = invalidPlacement || ((xPlacement > takenSpace[0]) && (xPlacement < takenSpace[1]));
                }
            } while(invalidPlacement);

            // Log chosen position
            List<float> takenRange = new List<float>();
            takenRange.Add(xPlacement - 0.4f);
            takenRange.Add(xPlacement + 0.4f);

            if (!tableTakenSpace.ContainsKey(tableIndex))
                tableTakenSpace[tableIndex] = new List<List<float>>();
            
            tableTakenSpace[tableIndex].Add(takenRange);

            // Add Crate on table
            if (DEBUG)
                Debug.Log("Instantiate Crate for tool <" + toolName + "> on Table-" + tableIndex + " at x-postion: " + xPlacement);

            Transform selectedTable = tableList[tableIndex];
            GameObject crate = Instantiate(toolCrate, selectedTable);
            crate.name = "Crate-" + toolName;
            crate.transform.localPosition = new Vector3(xPlacement, 1, 0.28f);

            // Add tool to crate
            GameObject toolPlane = Instantiate(tool, crate.transform);
            toolPlane.name = toolName + "-Top";
            toolPlane.transform.localPosition = new Vector3(0.0f, 1.6f, 0.0f);

            GameObject toolPlaneFront = Instantiate(tool, crate.transform);
            toolPlaneFront.name = toolName + "-Front";
            toolPlaneFront.transform.localPosition = new Vector3(0.0f, 0.75f, 0.8f);
            toolPlaneFront.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        // Commissions
        remainingTools = toolsNameToPlace;
        SetNextCommission();

        // Trigger the start of the level
        TriggerTogglePause();
    }

    void Update()
    {
        // Pause toggle on ESC
        if (Input.GetKeyDown("escape"))
            TriggerTogglePause();

        if (!isPaused)
        {
            timerTime += Time.deltaTime;
            TimeSpan t = TimeSpan.FromSeconds(timerTime);
            timerLabel.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }
    }


    // OTHER FUNCTIONS
    IEnumerator RunCountDown(int sec)
    {
        for (int i=sec; i>=0; i--)
        {
            countdown.text = i.ToString();
            yield return new WaitForSeconds(1.0f);
        }

        // Hide countdown text and unpause
        countdown.text = "";
        TogglePause();
    }

    public void TriggerTogglePause()
    {
        // Make Pause menu appear or not
        stoppedScreen.SetActive(!isPaused);
        pauseMenu.SetActive(!isPaused);

        // If Paused > UnPaused
        if (isPaused)
            StartCoroutine(RunCountDown(2));

        // If UnPaused > Paused
        if (!isPaused)
            TogglePause();
    }

    public void TogglePause()
    {
        // Pause variable updates
        isPaused = !isPaused;
        
        // Player Control updates
        Player.Instance.canMove = !Player.Instance.canMove;
        Player.Instance.ToggleMouse();
    }

    public void SetNextCommission()
    {
        activeCommission = remainingTools[0];

        // Set HUD Commission
        commission.text = "Commission: " + activeCommission.Replace("_", " ");

        // Blink to catch attention
        StartCoroutine(Blink(commission, 2));
    }

    IEnumerator Blink(TMP_Text text, int sec)
    {
        int blinkPerSecond = 3;
        for (int i = sec * blinkPerSecond; i >= 0; i--)
        {
            text.color = Color.blue;
            yield return new WaitForSeconds(0.5f / ((float)blinkPerSecond));

            text.color = Color.white;
            yield return new WaitForSeconds(0.5f / ((float)blinkPerSecond));
        }
    }

    public void Deliver(string deliveredTool)
    {
        // Remove tool from available tools
        remainingTools.Remove(deliveredTool);

        // Deal with delivered tool
        if (deliveredTool == activeCommission)
        {
            // Print Feedback meesage
            rightTool.SetActive(true);
            StartCoroutine(DeactivateAfter(rightTool, 3));

            // Increase delivered counter
            deliveredCommissions += 1;
            objectives.text = "Collected " + deliveredCommissions + "/" + levelObjTotal;

            // Check if Game is won
            if (deliveredCommissions < levelObjTotal)
            {
                // Set the next commission
                SetNextCommission();

                // Play Right Item sound
                rightItemSound.Play();
            }
            else
            {
                FinalizeGame();

                // Play Game won sound
                successSound.Play();
            }
        }
        else
        {
            // Print Feedback meesage
            wrongTool.SetActive(true);
            StartCoroutine(DeactivateAfter(wrongTool, 3));

            // Adding penalty time
            timerTime += 10;

            // Play Wrong Item sound
            wrongItemSound.Play();
        }
    }

    IEnumerator DeactivateAfter(GameObject go, int sec)
    {
        yield return new WaitForSeconds(sec);

        go.SetActive(false);
    }

    private void FinalizeGame()
    {
        // No more commissions
        commission.text = "";

        // Pause Timer and Movement
        TogglePause();

        // Spawn win Screen
        stoppedScreen.SetActive(true);
        winMenu.SetActive(true);

        // Log Perf
        CurrentGame.Instance.LogLevelPerf(((int)timerTime));
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene("SurgeryRoom");
    }

    public void LoadNextLevel()
    {
        CurrentGame.Instance.playLevel += 1;
        SceneManager.LoadScene("SurgeryRoom");
    }

    public void SaveGame()
    {
        CurrentGame.Instance.Save();

        gameSavedConfirmation.SetActive(true);
        StartCoroutine(DeactivateAfter(gameSavedConfirmation, 2));
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
