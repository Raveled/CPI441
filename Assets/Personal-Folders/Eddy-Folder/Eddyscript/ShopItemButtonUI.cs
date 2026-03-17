using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ShopItemButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;

    private SO_ItemData item;
    private Action<SO_ItemData> onClick;

    private void Awake()
    {
        button ??= GetComponent<Button>() ?? GetComponentInChildren<Button>(true);

        if (icon == null)
        {
            var t = transform.Find("Icon");
            icon = t ? t.GetComponent<Image>() : GetComponentInChildren<Image>(true);
        }

        if (label == null)
        {
            var t = transform.Find("Label");
            label = t ? t.GetComponent<TextMeshProUGUI>() : GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (button != null)
            button.onClick.AddListener(() => { if (item != null) onClick?.Invoke(item); });
    }

    public void Bind(SO_ItemData itemData, Action<SO_ItemData> click)
    {
        item = itemData;
        onClick = click;

        if (label != null) label.text = item != null ? item.itemName : "Unknown";
        if (icon != null) icon.sprite = item != null ? item.itemIcon : null;
    }
}