using System.Collections.Generic;
using UnityEngine;
public sealed class ShopCatalog : MonoBehaviour
{
    [SerializeField] private List<SO_ItemData> itemsForSale = new List<SO_ItemData>();

    public IReadOnlyList<SO_ItemData> ItemsForSale => itemsForSale;
}