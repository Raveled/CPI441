using UnityEngine;

namespace PurrLobby
{
    [System.Serializable]
    public struct LobbyUser
    {
        public string Id;
        public string DisplayName;
        public bool IsReady;
        public Texture2D Avatar;
        public int Team;
    }
}