using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Newtonsoft.Json;

public class SaveMatchData : MonoBehaviour
{
    public Match match = new Match();
    //Load in the data from GameManager
    public void ImportMatchData(int match_id, string timestamp, string map, string winner, int duration_seconds,
                                int team1Kills, int team2Kills, int team1TowersDestroyed, int team2TowersDestroyed) {
        match.match_id = match_id;
        match.timestamp = timestamp;
        match.map = map;
        match.winner = winner;
        match.duration_seconds = duration_seconds;
        match.kills["Team1"] = team1Kills;
        match.kills["Team2"] = team2Kills;
        match.towers_destroyed["Team1"] = team1TowersDestroyed;
        match.towers_destroyed["Team2"] = team2TowersDestroyed;
    }

    //Save as JSON file
    public void SaveToJSON() {
        string matchData = JsonUtility.ToJson(match);
        
        //For saving to pc
        string filePath = Application.persistentDataPath + "/MatchData.json";

        //For saving to project (to test)
        //string filePath = Application.persistentDataPath + "/MatchData.json";

        Debug.Log(filePath);
        //System.IO.File.WriteAllText(filePath, matchData);


        string jsonData = JsonConvert.SerializeObject(match, Formatting.Indented);
        System.IO.File.WriteAllText(filePath, jsonData);
    }
    
}
//Class to hold match data
[System.Serializable]
public class Match {
    public int match_id = 0;
    public string timestamp = "";
    public string map = "";
    public string winner = "";
    public int duration_seconds = 0;

    public Dictionary<string, int> kills = new Dictionary<string, int>();
    public Dictionary<string, int> towers_destroyed = new Dictionary<string, int>();
}
