using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.Splines.Interpolators;

public class JSON_EntityData : MonoBehaviour
{
    [SerializeField] TextAsset loadInJSON = null;
    [Space]
    [SerializeField] Statblocks statblocks = new Statblocks();

    //Import data, from gamemanager
    public void ImportEntityData(SO_EntityStatBlock core, SO_EntityStatBlock minion, SO_EntityStatBlock tower,
                                 SO_EntityStatBlock beetle, SO_EntityStatBlock mosquito, SO_EntityStatBlock butterfly) {
        //Init
        #region Create Keys
        if (!statblocks.entityStats.ContainsKey("Core")) statblocks.entityStats["Core"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Minion")) statblocks.entityStats["Minion"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Tower")) statblocks.entityStats["Tower"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Beetle")) statblocks.entityStats["Beetle"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Mosquito")) statblocks.entityStats["Mosquito"] = new EntityStatblock();
        if (!statblocks.entityStats.ContainsKey("Butterfly")) statblocks.entityStats["Butterfly"] = new EntityStatblock();
        #endregion

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
        statblocks.entityStats[entityType].baseAcceleration = stats_Origin.BaseAcceleration;
        statblocks.entityStats[entityType].basePlanarDamping = stats_Origin.BasePlanarDamping;
        statblocks.entityStats[entityType].baseJumpForce = stats_Origin.BaseJumpForce;
        statblocks.entityStats[entityType].baseJumpCooldown = stats_Origin.BaseJumpCooldown;
        statblocks.entityStats[entityType].baseAttackPower = stats_Origin.BaseAttackPower;
        statblocks.entityStats[entityType].baseDefaultAttackCooldown = stats_Origin.BaseDefaultAttackCooldown;
        statblocks.entityStats[entityType].rewardGold = stats_Origin.RewardGold;
        statblocks.entityStats[entityType].rewardXP = stats_Origin.RewardXP;
    }
    //Save as JSON file
    public void SaveToJSON() {
        //For saving to pc
        string filePath = Application.persistentDataPath + "/EntityData.json";

        //Convert to JSON
        string jsonData = JsonConvert.SerializeObject(statblocks, Formatting.Indented);
        System.IO.File.WriteAllText(filePath, jsonData);

        Debug.Log("saving Entity Data JSON to: " + filePath);
    }
    //Load JSON from a file
    public void LoadFromFile() {
        //Attempt to load from local file
        string filePath = Application.persistentDataPath + "/EntityData.json";

        if (System.IO.File.Exists(filePath)) {
            string jsonData = System.IO.File.ReadAllText(filePath);

            statblocks = JsonConvert.DeserializeObject<Statblocks>(jsonData);

            Debug.Log("Loaded Entity Data from: " + filePath);
        } else {
            //If no local file, load from the pre-loaded file instead
            if (loadInJSON == null) return;
            statblocks = JsonConvert.DeserializeObject<Statblocks>(loadInJSON.text);
            Debug.Log("Loaded In Match Data");
        }
    }
    //FOR TESTING
    public void EditMinion(int hp, int attack) {
        statblocks.entityStats["Minion"].baseHitPoints = hp;
        statblocks.entityStats["Minion"].baseAttackPower = attack;
        SaveToJSON();
    }
    //Getter
    public Statblocks GetStatblocks() {
        return statblocks;
    }
    public string GetJSONString() {
        string filePath = Application.persistentDataPath + "/EntityData.json";
        string jsonData = "";
        if (System.IO.File.Exists(filePath)) {
            jsonData = System.IO.File.ReadAllText(filePath);
        }
        return jsonData;
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
    public float baseAcceleration = 0f;
    public float basePlanarDamping = 0f;
    public float baseJumpForce = 0f;
    public float baseJumpCooldown = 0f;
    public int baseAttackPower = 0;
    public float baseDefaultAttackCooldown = 0f;
    public int rewardGold = 0;
    public int rewardXP = 0;
}
