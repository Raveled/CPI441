using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[System.Serializable]
public class ItemCreator : EditorWindow
{
    private string itemName = "New Item";
    private Sprite itemIcon;
    private string description = "";
    private int healthBonus = 0;
    private int attackDamageBonus = 0;
    private float attackSpeedMultiplier = 1f;
    private float movementSpeedMultiplier = 1f;
    private int defenseBonus = 0;
    private bool stackable = true;
    private int maxStacks = 1;
    
    [MenuItem("Tools/Item Creator")]
    public static void ShowWindow()
    {
        GetWindow<ItemCreator>("Item Creator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Create Custom Item", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Basic Info
        GUILayout.Label("Basic Information", EditorStyles.boldLabel);
        itemName = EditorGUILayout.TextField("Item Name", itemName);
        itemIcon = (Sprite)EditorGUILayout.ObjectField("Item Icon", itemIcon, typeof(Sprite), false);
        description = EditorGUILayout.TextField("Description", description, GUILayout.Height(60));
        
        EditorGUILayout.Space();
        
        // Stats
        GUILayout.Label("Stat Modifiers", EditorStyles.boldLabel);
        healthBonus = EditorGUILayout.IntField("Health Bonus", healthBonus);
        attackDamageBonus = EditorGUILayout.IntField("Attack Damage Bonus", attackDamageBonus);
        attackSpeedMultiplier = EditorGUILayout.FloatField("Attack Speed Multiplier", attackSpeedMultiplier);
        movementSpeedMultiplier = EditorGUILayout.FloatField("Movement Speed Multiplier", movementSpeedMultiplier);
        defenseBonus = EditorGUILayout.IntField("Defense Bonus", defenseBonus);
        
        EditorGUILayout.Space();
        
        // Stacking
        GUILayout.Label("Stacking Options", EditorStyles.boldLabel);
        stackable = EditorGUILayout.Toggle("Stackable", stackable);
        if (stackable)
            maxStacks = EditorGUILayout.IntField("Max Stacks", maxStacks);
        else
            maxStacks = 1;
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create Item", GUILayout.Height(30)))
        {
            CreateItem();
        }
        
        if (GUILayout.Button("Reset Fields", GUILayout.Height(25)))
        {
            ResetFields();
        }
    }
    
    private void CreateItem()
    {
        SO_ItemData newItem = CreateInstance<SO_ItemData>();
        
        newItem.itemName = itemName;
        newItem.itemIcon = itemIcon;
        newItem.description = description;
        newItem.healthBonus = healthBonus;
        newItem.attackDamageBonus = attackDamageBonus;
        newItem.attackSpeedMultiplier = attackSpeedMultiplier;
        newItem.movementSpeedMultiplier = movementSpeedMultiplier;
        newItem.defenseBonus = defenseBonus;
        newItem.stackable = stackable;
        newItem.maxStacks = maxStacks;
        
        string path = "Assets/Personal-Folders/Eddy-Folder/Items/" + itemName + ".asset";
        
        // Create directory if it doesn't exist
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);
        
        AssetDatabase.CreateAsset(newItem, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newItem;
        
        Debug.Log($"Created item: {itemName} at {path}");
    }
    
    private void ResetFields()
    {
        itemName = "New Item";
        itemIcon = null;
        description = "";
        healthBonus = 0;
        attackDamageBonus = 0;
        attackSpeedMultiplier = 1f;
        movementSpeedMultiplier = 1f;
        defenseBonus = 0;
        stackable = true;
        maxStacks = 1;
    }
}
#endif