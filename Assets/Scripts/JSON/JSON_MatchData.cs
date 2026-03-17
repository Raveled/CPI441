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

        //Convert to JSON
        string jsonData = JsonConvert.SerializeObject(match, Formatting.Indented);
        System.IO.File.WriteAllText(filePath, jsonData);

        Debug.Log("saving Entity Data JSON to: " + filePath);
    }
    //Load JSON from a file
    public void LoadFromFile() {
        //Attempt to load from local file
        string filePath = Application.persistentDataPath + "/MatchData.json";

        if (System.IO.File.Exists(filePath)) {
            string jsonData = System.IO.File.ReadAllText(filePath);

            match = JsonConvert.DeserializeObject<Match>(jsonData);

            Debug.Log("Loaded Entity Data from: " + filePath);
        } else {
            //If no local file, load from the pre-loaded file instead
            if (loadInJSON == null) return;
            match = JsonConvert.DeserializeObject<Match>(loadInJSON.text);
            Debug.Log("Loaded In Match Data");
        }
    }
    //Getter
    public Match GetMatch() {
        return match;
    }
    public string GetJSONString() {
        string filePath = Application.persistentDataPath + "/MatchData.json";
        string jsonData = "";
        if (System.IO.File.Exists(filePath)) {
            jsonData = System.IO.File.ReadAllText(filePath);
        }
        return jsonData;
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
