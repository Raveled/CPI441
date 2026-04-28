using System;
using System.Collections;
using UnityEngine;

public sealed class CharacterAbilityIconLoader : MonoBehaviour
{
    [Serializable]
    public sealed class CharacterAbilitySet
    {
        public string characterName;
        public Sprite ability1;
        public Sprite ability2;
        public Sprite ability3;
        public Sprite ability4;
    }

    [Header("References")]
    [SerializeField] private AbilityBarUI abilityBar;

    [Header("Character Icon Sets")]
    [SerializeField] private CharacterAbilitySet[] characterSets;

    [Header("Behavior")]
    [SerializeField] private bool keepWatchingForCharacterChanges = true;
    [SerializeField] private float retryIntervalSeconds = 0.25f;

    private Player boundPlayer;
    private string appliedCharacterKey = string.Empty;
    private Coroutine watchRoutine;

    private void Awake()
    {
        if (abilityBar == null)
            abilityBar = FindFirstObjectByType<AbilityBarUI>();
    }

    private void Start()
    {
        if (keepWatchingForCharacterChanges && watchRoutine == null)
            watchRoutine = StartCoroutine(WatchBoundPlayer());
    }

    public void BindPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning("[CharacterAbilityIconLoader] BindPlayer called with null.");
            return;
        }

        boundPlayer = player;
        appliedCharacterKey = string.Empty;

        Debug.Log(
            $"[CharacterAbilityIconLoader] Bound player '{boundPlayer.gameObject.name}' " +
            $"with character '{boundPlayer.character.value}'."
        );

        TryApplyFromBoundPlayer();
    }

    public void RefreshIcons()
    {
        TryApplyFromBoundPlayer();
    }

    private IEnumerator WatchBoundPlayer()
    {
        while (true)
        {
            TryApplyFromBoundPlayer();
            yield return new WaitForSeconds(retryIntervalSeconds);
        }
    }

    private void TryApplyFromBoundPlayer()
    {
        if (abilityBar == null)
            abilityBar = FindFirstObjectByType<AbilityBarUI>();

        if (abilityBar == null || boundPlayer == null)
            return;

        string detectedFromComponents = DetectCharacterFromComponents(boundPlayer);
        string detectedFromNetwork = NormalizeKey(boundPlayer.character.value);

        string resolvedCharacter = !string.IsNullOrEmpty(detectedFromComponents)
            ? detectedFromComponents
            : detectedFromNetwork;

        if (string.IsNullOrEmpty(resolvedCharacter))
            return;

        if (!string.IsNullOrEmpty(detectedFromComponents) &&
            !string.IsNullOrEmpty(detectedFromNetwork) &&
            detectedFromComponents != detectedFromNetwork)
        {
            Debug.LogWarning(
                $"[CharacterAbilityIconLoader] Character mismatch on '{boundPlayer.gameObject.name}'. " +
                $"Component='{detectedFromComponents}', NetworkValue='{detectedFromNetwork}'. " +
                $"Using component detection for UI."
            );
        }

        if (resolvedCharacter == appliedCharacterKey)
            return;

        CharacterAbilitySet set = FindSet(resolvedCharacter);
        if (set == null)
        {
            Debug.LogWarning(
                $"[CharacterAbilityIconLoader] No icon set configured for character '{resolvedCharacter}'."
            );
            return;
        }

        abilityBar.SetIcons(set.ability1, set.ability2, set.ability3, set.ability4);
        ApplyKeyLabels(resolvedCharacter);
        appliedCharacterKey = resolvedCharacter;
        bool isMosquito = resolvedCharacter == "mosquito";
        abilityBar.SetSlotPassiveMode(0, isMosquito);
       
        Debug.Log(
            $"[CharacterAbilityIconLoader] Applied icons for '{resolvedCharacter}' " +
            $"from bound player '{boundPlayer.gameObject.name}'."
        );
    }
    private void ApplyKeyLabels(string resolvedCharacter)
    {
        if (abilityBar == null)
            return;

        switch (resolvedCharacter)
        {
            case "mosquito":
                abilityBar.SetKeybinds("Passive", "E", "Q", "R");
                break;

            case "butterfly":
                abilityBar.SetKeybinds("E", "Q", "Shift", "R");
                break;

            case "beetle":
                abilityBar.SetKeybinds("Q", "Shift", "E", "R");
                break;

            default:
                abilityBar.SetKeybinds("E", "Q", "Shift", "R");
                break;
        }
    }
    private CharacterAbilitySet FindSet(string normalizedCharacterName)
    {
        if (characterSets == null)
            return null;

        for (int i = 0; i < characterSets.Length; i++)
        {
            CharacterAbilitySet set = characterSets[i];
            if (set == null)
                continue;

            if (NormalizeKey(set.characterName) == normalizedCharacterName)
                return set;
        }

        return null;
    }

    private static string DetectCharacterFromComponents(Player player)
    {
        if (player == null)
            return string.Empty;

        if (player.GetComponentInChildren<Butterfly>(true) != null)
            return "butterfly";

        if (player.GetComponentInChildren<Mosquito>(true) != null)

            return "mosquito";

        if (player.GetComponentInChildren<Beetle>(true) != null)
            return "beetle";

        return string.Empty;
    }

    private static string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}