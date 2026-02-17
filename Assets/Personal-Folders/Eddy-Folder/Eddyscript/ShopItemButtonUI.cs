using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class ShopItemButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    private SO_ItemData item;
    private System.Action<SO_ItemData> onClicked;

    public void Bind(SO_ItemData itemData, System.Action<SO_ItemData> clicked)
    {
        item = itemData;
        onClicked = clicked;

        if (icon != null) icon.sprite = itemData != null ? itemData.itemIcon : null;
        if (nameText != null) nameText.text = itemData != null ? itemData.itemName : "Item";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClicked?.Invoke(item));
        }
    }
}