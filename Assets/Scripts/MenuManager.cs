using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class MenuManager : MonoBehaviour
{
    // MainMenu Components
    public Button loadGameButton;

    // NewGame Components
    public GameObject inputField;
    public GameObject newGameNameError;
    public GameObject newGameNameExistsError;
    public TMP_Dropdown saveSelector;

    // LoadGame Components
    public TMP_Text loadedSaveName;

    // LevelSelector Components
    public GameObject levelButtonsContainer;
    public GameObject levelButtonModel;
    public TMP_Text levelDetailsInfo;
    public GameObject exitToMenuConfirm;
    public GameObject gameSavedConfirmation;

    // menus
    public GameObject mainMenu;
    public GameObject newGameMenu;
    public GameObject loadGameMenu;
    public GameObject levelSelectionMenu;

    // variables
    private List<string> existingSaves = new List<string> ();
    private bool isConfirmed = false;
    private Dictionary<int, Button> levelButtons = new Dictionary<int, Button>();
    private Button selectedLevelButton;
    private int selectedLevel = -1;

    // When menu is loaded
    void Start()
    {
        // Load_Game options
        string[] files = Directory.GetFiles(Application.persistentDataPath + "/Saves");

        if (files.Length == 0)
        {
            Debug.Log("No Load files available...");
            loadGameButton.interactable = false;
        }
        else
        {
            // Setting Load_Game options
            for (int i=0; i<files.Length; i++)
            {
                string fileName = files[i].Split("\\")[^1].Split(".json")[0];
                existingSaves.Add(fileName);
            }
            saveSelector.AddOptions(existingSaves);
        }

        // Generate level buttons
        for (int i=0; i<5; i++)
        {
            GameObject buttonContainer = Instantiate(levelButtonModel, levelButtonsContainer.transform);
            buttonContainer.name = "Level" + (i+1) + "Button";
            buttonContainer.transform.Translate(((60 * i) - 120), 0, 0);

            Button button = buttonContainer.transform.GetComponentInChildren<Button>();
            string text = button.GetComponentInChildren<TMP_Text>().text;
            button.GetComponentInChildren<TMP_Text>().text = text.Replace("<n>", (i + 1).ToString());

            int x = new int();
            x = i;
            button.onClick.AddListener(delegate { SelectLevel(x+1); });

            levelButtons[i + 1] = button;
        }
        SelectLevel(1);

        // Activate Level Selection Menu if currentGame is active
        if (CurrentGame.Instance.isActive)
        {
            // Loaded Save Name Tag
            loadedSaveName.text = CurrentGame.Instance.gameName;
            SetupLevelChoices();
            if (CurrentGame.Instance.highestPlayedLevel > 1)
            {
                int levelToSelect = Math.Min(CurrentGame.Instance.highestPlayedLevel + 1, 5);
                SelectLevel(levelToSelect);
            }

            // Menu switch
            mainMenu.SetActive(false);
            levelSelectionMenu.SetActive(true);
        }
    }

    public void QuitGame()
    {
        Debug.Log("QUIT!");
        Application.Quit();
    }

    public void NewGameConfirm()
    {
        string gameName = inputField.GetComponent<TMP_InputField>().text;

        if (string.IsNullOrEmpty(gameName))
        {
            newGameNameError.SetActive(true);
            return;
        }

        if (existingSaves.Contains(gameName))
        {
            newGameNameExistsError.SetActive(true);
            return;
        }

        // CurrentGame information
        CurrentGame.Instance.gameName = gameName;
        CurrentGame.Instance.isActive = true;
        CurrentGame.Instance.Save();

        // Updating interactible elements
        loadGameButton.interactable = true;
        inputField.GetComponent<TMP_InputField>().text = "";

        // Loaded Save Name Tag
        loadedSaveName.text = gameName;

        // Adding new option to Load_Save DropDown
        List<string> new_option = new List<string>();
        existingSaves.Add(gameName);
        new_option.Add(gameName);
        saveSelector.AddOptions(new_option);

        // Setup Level choices
        SetupLevelChoices();

        //menu switch
        newGameMenu.SetActive(false);
        newGameNameError.SetActive(false);
        newGameNameExistsError.SetActive(false);
        levelSelectionMenu.SetActive(true);
    }

    public void LoadGameConfirm()
    {
        string gameName = saveSelector.options[saveSelector.value].text;
        CurrentGame.Instance.gameName = gameName;
        CurrentGame.Instance.isActive = true;
        CurrentGame.Instance.Load();

        // Loaded Save Name Tag
        loadedSaveName.text = gameName;

        // Setup Level choices
        SetupLevelChoices();

        //menu switch
        loadGameMenu.SetActive(false);
        levelSelectionMenu.SetActive(true);
    }

    private void SetupLevelChoices()
    {
        int disableAbove = 1;
        if(CurrentGame.Instance.highestPlayedLevel > 0)
        {
            disableAbove = CurrentGame.Instance.highestPlayedLevel + 1;
        }

        for (int i=1; i<=5; i++)
        {
            levelButtons[i].interactable = (i <= disableAbove);

            if (i < disableAbove)
            {
                // Retrieve and Format level time
                int levelTime = CurrentGame.Instance.levelsDone[i];
                TimeSpan t = TimeSpan.FromSeconds(levelTime);
                string formattedTime = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);

                // Write it on the button
                levelButtons[i].GetComponentInChildren<TMP_Text>().text = i + "\n" + formattedTime;
            }
        }
        SelectLevel(disableAbove);
    }

    public void SelectLevel(int levelNumber)
    {
        // Check if selected is the one clicked
        if (selectedLevel == levelNumber)
            return;

        // Check if there is an already select level and reset outline
        if (selectedLevel != -1 ) 
            selectedLevelButton.GetComponent<Outline>().effectDistance = new Vector2(1, 1);

        // Set outline of new SelectedLevel
        selectedLevel = levelNumber;
        selectedLevelButton = levelButtons[levelNumber];
        selectedLevelButton.GetComponent<Outline>().effectDistance = new Vector2(4, 4);

        //Set level detail info
        // TODO: Find place for this info, as it is copied from Game.cs var
        Dictionary<int, int> levelsGoals = new Dictionary<int, int>
        {
            { -1, 5 }, // DEBUG
            { 1, 2 },
            { 2, 4 },
            { 3, 6 },
            { 4, 8 },
            { 5, 10 },
        };
        int toolsToPlaceAmount = Math.Min((levelsGoals[levelNumber] + 1) * 2, 20);
        string levelInfoText = "Level <b>" + levelNumber + "</b> goals:";
        levelInfoText += String.Format("\n - {0,-7}<b>{1,2}</b> tools", "Gather", levelsGoals[levelNumber]);
        levelInfoText += String.Format("\n - {0,-7}<b>{1,2}</b> tools", "From", toolsToPlaceAmount) ;
        levelDetailsInfo.text = levelInfoText;
    }

    public void PlayGame()
    {
        CurrentGame.Instance.isSaved = false;
        isConfirmed = false;
        Debug.Log("Let's play level: " + selectedLevel);

        CurrentGame.Instance.playLevel = selectedLevel;
        SceneManager.LoadScene("SurgeryRoom");
    }

    public void BackToMainMenu()
    {
        if (CurrentGame.Instance.isSaved == true || isConfirmed == true)
        {
            // CurrentGame
            CurrentGame.Instance.isActive = false;

            // Variables and warning
            exitToMenuConfirm.SetActive(false);
            isConfirmed = false;

            // Menu Change
            levelSelectionMenu.SetActive(false);
            mainMenu.SetActive(true);
        }
        else
        {
            exitToMenuConfirm.SetActive(true);
            isConfirmed = true;
        }
    }

    public void SaveGame()
    {
        CurrentGame.Instance.Save();
        isConfirmed = false;
        CurrentGame.Instance.isSaved = true;

        gameSavedConfirmation.SetActive(true);
        StartCoroutine(DeactivateAfter(gameSavedConfirmation, 2));
    }

    IEnumerator DeactivateAfter(GameObject go, int sec)
    {
        yield return new WaitForSeconds(sec);

        go.SetActive(false);
    }
}
