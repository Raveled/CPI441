using UnityEngine;
using TMPro;

public class HPBarHelper : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI hpText;
    public void SetText(string newText) {
        hpText.text = newText;
    }
}
