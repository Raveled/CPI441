using UnityEngine;
[DisallowMultipleComponent]
public sealed class InventorySlotCountEnforcer : MonoBehaviour
{
    [SerializeField] private int maxSlots = 6;

    private void Awake()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) Apply();
    }
#endif

    private void Apply()
    {
        maxSlots = Mathf.Clamp(maxSlots, 1, 6);

        var inv = GetComponent<Inventory>();
        if (inv == null) return;

        var so = new SerializedObjectProxy(inv);
        so.TrySetIntField("maxSlots", maxSlots);
    }
private sealed class SerializedObjectProxy
    {
        private readonly Object target;

        public SerializedObjectProxy(Object target) => this.target = target;

        public bool TrySetIntField(string fieldName, int value)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(target);
            var prop = serialized.FindProperty(fieldName);
            if (prop == null || prop.propertyType != UnityEditor.SerializedPropertyType.Integer) return false;
            prop.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
#else
            return false;
#endif
        }
    }
}