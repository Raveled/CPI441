using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using System.IO;

public class JSON_EntityData : MonoBehaviour
{
    [SerializeField] TextAsset loadInJSON = null;
    [Space]
    [SerializeField] Statblocks statblocks = new Statblocks();

    //Import data, from gamemanager
    public void ImportEntityData(SO_EntityStatBlock core, SO_EntityStatBlock minion, SO_EntityStatBlock tower,
                                 SO_EntityStatBlock beetle, SO_EntityStatBlock mosquito, SO_EntityStatBlock butterfly) {
        //Init
        if (!statblocks.entityStats.ContainsKey("Core")) statblocks.entityStats["Core"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Minion")) statblocks.entityStats["Minion"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Tower")) statblocks.entityStats["Tower"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Beetle")) statblocks.entityStats["Beetle"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Mosquito")) statblocks.entityStats["Mosquito"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Butterfly")) statblocks.entityStats["Butterfly"] = new EntityStatblock();

        #region NPEs
        ImportEntityDataHelper("Core", core);
        ImportEntityDataHelper("Minion", minion);
        ImportEntityDataHelper("Tower", tower);
        #endregion

        #region PCs
        ImportEntityDataHelper("Beetle", beetle);
        ImportEntityDataHelper("Mosquito", mosquito);
        ImportEntityDataHelper("Butterfly", butterfly);
        #endregion
    }
    //Helper function for loading data
    void ImportEntityDataHelper(string entityType, SO_EntityStatBlock stats_Origin) {
        statblocks.entityStats[entityType].baseHitPoints = stats_Origin.BaseHitPoints;
        statblocks.entityStats[entityType].baseMoveSpeed = stats_Origin.BaseMoveSpeed;
        statblocks.entityStats[entityType].baseAttackPower = stats_Origin.BaseAttackPower;
        statblocks.entityStats[entityType].baseDefaultAttackCooldown = stats_Origin.BaseDefaultAttackCooldown;
        statblocks.entityStats[entityType].rewardGold = stats_Origin.RewardGold;
        statblocks.entityStats[entityType].rewardXP = stats_Origin.RewardXP;
    }
    //Save as JSON file
    public void SaveToJSON() {
        //For saving to pc
        string filePath = Application.persistentDataPath + "/EntityData.json";

        Debug.Log("saving Entity Data JSON to: " + filePath);

        //Convert to JSON
        string jsonData = JsonConvert.SerializeObject(statblocks, Formatting.Indented);
        System.IO.File.WriteAllText(filePath, jsonData);
    }
    //Load JSON from a file
    public void LoadFromFile() {
        if (loadInJSON == null) {
            return;
        }

        Debug.Log("Loaded In Match Data");
        statblocks = JsonConvert.DeserializeObject<Statblocks>(loadInJSON.text);
    }
    //Getter
    public Statblocks GetStatblocks() {
        return statblocks;
    }
}

//Class to hold statblocks
[System.Serializable]
public class Statblocks {
    public Dictionary<string, EntityStatblock> entityStats = new Dictionary<string, EntityStatblock>();
}
//Class to hold stats
[System.Serializable]
public class EntityStatblock {
    public int baseHitPoints = 0;
    public float baseMoveSpeed = 0f;
    public int baseAttackPower = 0;
    public float baseDefaultAttackCooldown = 0f;
    public int rewardGold = 0;
    public int rewardXP = 0;
}
