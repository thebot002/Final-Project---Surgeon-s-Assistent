using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class CurrentGame : MonoBehaviour
{
    public static CurrentGame Instance;

    public string gameName = "DEBUG";
    public bool isActive = false;
    public Dictionary<int, int> levelsDone = new Dictionary<int, int>();
    public int highestPlayedLevel = -1;

    public bool isSaved = true;

    public int playLevel = -1;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [System.Serializable]
    class SaveData
    {
        public string nameOfGame;
        public List<string> levelsInfo;
        public int highestPlayedLevel;
    }

    public void Save()
    {
        SaveData data = new SaveData();
        data.nameOfGame = gameName;

        List<string> levelsDoneList = new List<string>();
        foreach (KeyValuePair<int, int> entry in levelsDone)
            levelsDoneList.Add(entry.Key + "," + entry.Value);

        data.levelsInfo = levelsDoneList;
        data.highestPlayedLevel = highestPlayedLevel;

        string json = JsonUtility.ToJson(data);
        string path = Application.persistentDataPath + "/Saves/" + gameName + ".json";
        File.WriteAllText(path, json);
        Debug.Log("Game saved at: " + path);
    }

    public void Load()
    {
        string path = Application.persistentDataPath + "/Saves/" + gameName + ".json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            Dictionary<int, int> levels = new Dictionary<int, int>();
            foreach (string levelInfo in data.levelsInfo)
            {
                int levelNumber = Int32.Parse(levelInfo.Split(",")[0]);
                int time = Int32.Parse(levelInfo.Split(",")[1]);
                levels.Add(levelNumber, time);
            }
            
            levelsDone = levels;
            highestPlayedLevel = data.highestPlayedLevel;
        }
        else
        {
            Debug.Log("File Not Found!");
        }
    }

    public void LogLevelPerf(int time)
    {
        if(!levelsDone.ContainsKey(playLevel) || (levelsDone[playLevel] < time))
        {
            levelsDone[playLevel] = time;
        }

        if(playLevel > highestPlayedLevel)
        {
            highestPlayedLevel = playLevel;
        }
    }
}
