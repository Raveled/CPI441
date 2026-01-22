using UnityEngine;
using PurrNet;
using TMPro;

public class NetworkedObjectTest : NetworkIdentity
{
    private Renderer _renderer;
    
    [SerializeField] private SyncVar<int> _health = new(100);
    [SerializeField] private TMP_Text _healthText;
    
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
        _renderer = GetComponent<Renderer>();
    }
    
    // Affects both server and client through ambiguity of observers
    [ObserversRpc]
    public void ChangeColor(Color newColor)
    {
        if (_renderer != null)
        {
            _renderer.material.color = newColor;
        }
    }
    
    // Player Takes Damage
    [ServerRpc]
    public void takeDamage(int damage)
    {
        _health.value -= damage;
    }

    private void Awake()
    {
        _health.onChanged += onChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _health.onChanged -= onChanged;
    }

    private void onChanged(int newVal)
    {
       _healthText.text = newVal.ToString();
    }
}