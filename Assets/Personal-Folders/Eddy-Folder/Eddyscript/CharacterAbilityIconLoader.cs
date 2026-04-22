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
    }

    [Header("References")]
    [SerializeField] private AbilityBarUI abilityBar;

    [Header("Character Icon Sets")]
    [SerializeField] private CharacterAbilitySet[] characterSets;

    [Header("Behavior")]
    [SerializeField] private bool keepWatchingForCharacterChanges = false;
    [SerializeField] private float retryIntervalSeconds = 0.25f;

    private Player cachedLocalPlayer;
    private string appliedCharacterKey = string.Empty;

    private void Awake()
    {
        if (abilityBar == null)
        {
            abilityBar = FindFirstObjectByType<AbilityBarUI>();
        }
    }

    private void Start()
    {
        StartCoroutine(BindLocalPlayerAndApplyIcons());
    }

    private IEnumerator BindLocalPlayerAndApplyIcons()
    {
        while (abilityBar == null)
        {
            abilityBar = FindFirstObjectByType<AbilityBarUI>();
            yield return new WaitForSeconds(retryIntervalSeconds);
        }

        while (true)
        {
            if (cachedLocalPlayer == null)
            {
                cachedLocalPlayer = FindLocalPlayer();
            }

            if (cachedLocalPlayer != null)
            {
                string characterName = ReadCharacterName(cachedLocalPlayer);
                if (!string.IsNullOrWhiteSpace(characterName))
                {
                    ApplyIconsForCharacter(characterName);

                    if (!keepWatchingForCharacterChanges)
                    {
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(retryIntervalSeconds);
        }
    }

    private Player FindLocalPlayer()
    {
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].isLocalPlayer())
            {
                return players[i];
            }
        }

        return null;
    }

    private string ReadCharacterName(Player player)
    {
        if (player == null)
        {
            return string.Empty;
        }

        string raw = player.character.value;
        return NormalizeKey(raw);
    }

    private void ApplyIconsForCharacter(string characterName)
    {
        string normalized = NormalizeKey(characterName);
        if (string.IsNullOrEmpty(normalized) || normalized == appliedCharacterKey)
        {
            return;
        }

        CharacterAbilitySet set = FindSet(normalized);
        if (set == null)
        {
            Debug.LogWarning($"[CharacterAbilityIconLoader] No icon set configured for character '{characterName}'.");
            return;
        }

        abilityBar.SetIcons(set.ability1, set.ability2, set.ability3);
        appliedCharacterKey = normalized;
        Debug.Log($"[CharacterAbilityIconLoader] Loaded ability icons for '{characterName}'.");
    }

    private CharacterAbilitySet FindSet(string normalizedCharacterName)
    {
        if (characterSets == null)
        {
            return null;
        }

        for (int i = 0; i < characterSets.Length; i++)
        {
            CharacterAbilitySet set = characterSets[i];
            if (set == null)
            {
                continue;
            }

            if (NormalizeKey(set.characterName) == normalizedCharacterName)
            {
                return set;
            }
        }

        return null;
    }

    private static string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}