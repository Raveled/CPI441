using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMemberListVisualClone : MonoBehaviour
{
    [Tooltip("Transform that cloned entries will be placed under.")]
    [SerializeField] private Transform content;
    [SerializeField] public int Team = 0;

    // Maps source child InstanceID → cloned GameObject
    private readonly Dictionary<int, GameObject> _cloneMap = new();

    // Maps LobbyUser ID → cloned GameObject for external visibility control
    private readonly Dictionary<string, GameObject> _memberIdToClone = new();


    public void Sync(Transform sourceContent)
    {
        if (sourceContent == null || content == null) return;

        SyncEntries(sourceContent);
        RemoveStaleEntries(sourceContent);
    }

    public void Clear()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        _cloneMap.Clear();
    }

    private void SyncEntries(Transform sourceContent)
    {
        for (int i = 0; i < sourceContent.childCount; i++)
        {
            Transform sourceChild = sourceContent.GetChild(i);
            int sourceId = sourceChild.gameObject.GetInstanceID();

            // Instantiate a clone if this source child is new
            if (!_cloneMap.TryGetValue(sourceId, out GameObject clone) || clone == null)
            {
                clone = Instantiate(sourceChild.gameObject, content);
                StripLogicComponents(clone);
                _cloneMap[sourceId] = clone;

                if (sourceChild.TryGetComponent(out PurrLobby.MemberEntry entry))
                    _memberIdToClone[entry.MemberId] = clone;
            }

            // Keep sibling order in sync with source
            clone.transform.SetSiblingIndex(i);

            // Set to visible if team matches list team
            if (sourceChild.TryGetComponent(out PurrLobby.MemberEntry memberEntry))
            {
                if (memberEntry.MemberId != null && memberEntry.teamNum == Team)
                    clone.SetActive(true);
                else
                    clone.SetActive(false);
            }

            // Mirror the source entry's ready color onto the clone's name text
            ApplyReadyColor(sourceChild, clone);

            // Mirror the source entry's character selection onto the clone's background color
            ApplyBackgroundColor(sourceChild, clone);
        }
    }

    private void ApplyBackgroundColor(Transform sourceChild, GameObject clone)
    {
        if (!sourceChild.TryGetComponent(out PurrLobby.MemberEntry sourceEntry))
            return;

        var cloneImage = clone.GetComponent<Image>();
        if (cloneImage == null)
            return;

        switch (sourceEntry.Character)
        {
            case "mosquito":
                cloneImage.color = new Color(255/255f, 121/255f, 0/255f);
                break;
            case "beetle":
                cloneImage.color = new Color(90/255f, 186/255f, 255/255f);
                break;
            case "butterfly":
                cloneImage.color = new Color(0f/255f, 255/255f, 39/255f);
                break;
            default:
                cloneImage.color = new Color(255/255f, 55/255f, 55/255f);
                break;
        }
    }

    private void RemoveStaleEntries(Transform sourceContent)
    {
        // Build a set of all current source IDs
        var activeSourceIds = new HashSet<int>();
        foreach (Transform child in sourceContent)
            activeSourceIds.Add(child.gameObject.GetInstanceID());

        // Destroy clones whose source entry no longer exists
        var staleIds = new List<int>();
        foreach (var (id, clone) in _cloneMap)
        {
            if (!activeSourceIds.Contains(id))
            {
                if (clone != null) Destroy(clone);
                staleIds.Add(id);
            }
        }

        foreach (int id in staleIds)
            _cloneMap.Remove(id);
    }

    private static void ApplyReadyColor(Transform source, GameObject clone)
    {
        if (!source.TryGetComponent(out PurrLobby.MemberEntry sourceEntry))
            return;
 
        var cloneText = clone.GetComponentInChildren<TMP_Text>();
        if (cloneText != null)
            cloneText.color = sourceEntry.NameColor;
    }

    private static void StripLogicComponents(GameObject clone)
    {
        // MemberEntry drives lobby state — clones must not run it
        if (clone.TryGetComponent(out PurrLobby.MemberEntry entry))
            Destroy(entry);
    }
}