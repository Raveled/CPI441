using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    public class MemberEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text userName;
        [SerializeField] private RawImage avatar;
        [SerializeField] private Color readyColor;

        private Color _defaultColor;
        private string _memberId;
        private int _teamNum;
        public string MemberId => _memberId;
        public int teamNum => _teamNum;

        public Color NameColor => userName.color;

        public void Init(LobbyUser user)
        {
            _defaultColor = userName.color;
            _memberId = user.Id;
            _teamNum = user.Team;
            avatar.texture = user.Avatar;
            userName.text = user.DisplayName;
            SetReady(user.IsReady);
        }
        
        public void SetReady(bool isReady)
        {
            userName.color = isReady ? readyColor : _defaultColor;
        }

        public void UpdateMember(LobbyUser user)
        {
            _teamNum = user.Team;
            SetReady(user.IsReady);
        }
    }
}
