using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{

    public string saveFile;
    public string idFile;
    public List<GameData> gameDataList = new List<GameData>();
    private int currentParticipantId;

    void Awake()
    {
        saveFile = Path.Combine(Application.persistentDataPath + "gamedata.json");
        idFile = Path.Combine(Application.persistentDataPath + "lastParticipantId.txt");

        readFile();
        assignNewParticipantId();
        Debug.Log("Save file at: " + Application.persistentDataPath);
    }

    private void assignNewParticipantId()
    {
        if (File.Exists(idFile))
        {
            string idString = File.ReadAllText(idFile);
            if (int.TryParse(idString, out int lastId))
            {
                currentParticipantId = lastId + 1;
            }
            else
            {
                Debug.LogWarning("Invalid lastParticipantId format, starting from new id: 1" + idString);
                currentParticipantId = 1;
            }
        }
        else{
            currentParticipantId = 1;
        }
        saveLastParticipantId();
        Debug.Log("ParticipantId written to " + idFile);

    }

    private void saveLastParticipantId()
    {
        File.WriteAllText(idFile, currentParticipantId.ToString("D5"));
    }

    public void readFile()
    {
        if (File.Exists(saveFile))
        {
            string fileContents = File.ReadAllText(saveFile);

            gameDataList = JsonUtility.FromJson<GameDataList>(fileContents)?.gameDataList ?? new List<GameData>();
        }
        else
        {
            Debug.LogWarning("Save file not found at " + saveFile); 
        }
    }

    public void writeFile()
    {
        GameDataList gameDataListWrapper = new GameDataList { gameDataList = this.gameDataList };
        string jsonString = JsonUtility.ToJson(gameDataListWrapper, true);
        
        File.WriteAllText(saveFile, jsonString);
        Debug.Log("Data written to " + saveFile);
    }

    public void SaveGameData(GameData newGameData)
    {
        newGameData.participantId = currentParticipantId.ToString("D5");
        gameDataList.Add(newGameData);
        writeFile();
    }

    [System.Serializable]
    public class GameDataList
    {
        public List<GameData> gameDataList;
    }


}
