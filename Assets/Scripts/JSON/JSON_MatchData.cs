using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using System.IO;

public class JSON_MatchData : MonoBehaviour
{
    [SerializeField] TextAsset loadInJSON = null;
    [Space]
    [SerializeField] Match match = new Match();
    //Load in the data from GameManager
    public void ImportMatchData(int match_id, string timestamp, string map, string winner, int duration_seconds,
                                int team1Kills, int team2Kills, int team1Deaths, int team2Deaths, int team1TowersDestroyed, int team2TowersDestroyed) {
        match.match_id = match_id;
        match.timestamp = timestamp;
        match.map = map;
        match.winner = winner;
        match.duration_seconds = duration_seconds;
        match.kills["Team1"] = team1Kills;
        match.kills["Team2"] = team2Kills;
        match.deaths["Team1"] = team1Deaths;
        match.deaths["Team2"] = team2Deaths;
        match.towers_destroyed["Team1"] = team1TowersDestroyed;
        match.towers_destroyed["Team2"] = team2TowersDestroyed;
    }
    //Save as JSON file
    public void SaveToJSON() {
        //For saving to pc
        string filePath = Application.persistentDataPath + "/MatchData.json";

        Debug.Log("saving Match Data JSON to: " + filePath);

        //Convert to JSON
        string jsonData = JsonConvert.SerializeObject(match, Formatting.Indented);
        System.IO.File.WriteAllText(filePath, jsonData);
    }
    //Load in match data from a file
    public void LoadFromFile() {
        if (loadInJSON == null) {
            return;
        }

        Debug.Log("Loaded In Match Data");
        match = JsonConvert.DeserializeObject<Match>(loadInJSON.text);
    }
    //Getter
    public Match GetMatch() {
        return match;
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
    public Dictionary<string, int> deaths = new Dictionary<string, int>();
    public Dictionary<string, int> towers_destroyed = new Dictionary<string, int>();
}
