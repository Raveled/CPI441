using System;
using System.Collections.Generic;
using System.Linq;
using PurrLobby;
using PurrNet.Logging;
using UnityEngine;

public class LobbyMemberList : MonoBehaviour
{
    [SerializeField] private PurrLobby.MemberEntry memberEntryPrefab;
    [SerializeField] private Transform content;

    // Manage visual cloners that mirror our content list
    [SerializeField] private List<LobbyMemberListVisualClone> visualClones = new();

    public void LobbyDataUpdate(PurrLobby.Lobby room)
    {
        if(!room.IsValid)
            return;

        HandleExistingMembers(room);
        HandleNewMembers(room);
        HandleLeftMembers(room);

        NotifyClones();
    }

    public void OnLobbyLeave()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        ClearClones();
    }

    private void HandleExistingMembers(PurrLobby.Lobby room)
    {
        foreach (Transform child in content)
        {
            if (!child.TryGetComponent(out PurrLobby.MemberEntry member))
                continue;

            var matchingMember = room.Members.Find(x => x.Id == member.MemberId);
            if (!string.IsNullOrEmpty(matchingMember.Id))
            {
                member.UpdateMember(matchingMember);
            }
        }
    }

    private void HandleNewMembers(PurrLobby.Lobby room)
    {
        var existingMembers = content.GetComponentsInChildren<PurrLobby.MemberEntry>();

        foreach (var member in room.Members)
        {
            if (Array.Exists(existingMembers, x => x.MemberId == member.Id))
                continue;

            var entry = Instantiate(memberEntryPrefab, content);
            entry.Init(member);
        }
    }

    private void HandleLeftMembers(PurrLobby.Lobby room)
    {
        var childrenToRemove = new List<Transform>();

        for (int i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            if (!child.TryGetComponent(out PurrLobby.MemberEntry member))
                continue;

            if (!room.Members.Exists(x => x.Id == member.MemberId))
            {
                childrenToRemove.Add(child);
            }
        }

        foreach (var child in childrenToRemove)
        {
            Destroy(child.gameObject);
        }
    }

    // Clone Handlers
    private void NotifyClones()
    {
        foreach (var clone in visualClones)
        {
            if (clone != null)
                clone.Sync(content);
        }
    }
 
    private void ClearClones()
    {
        foreach (var clone in visualClones)
        {
            if (clone != null)
                clone.Clear();
        }
    }
}
