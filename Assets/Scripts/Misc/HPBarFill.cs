using UnityEngine;
using UnityEngine.UI;

public class HPBarFill : MonoBehaviour
{
    [SerializeField] Image fillImage;
    public void SetColor(Color32 color) {
        fillImage.color = color;
    }
}
