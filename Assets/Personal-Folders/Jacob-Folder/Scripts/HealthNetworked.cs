using UnityEngine;
using PurrNet;
using TMPro;
using System;

public class HealthNetworked : NetworkIdentity
{
    
    [SerializeField] private SyncVar<int> _health = new(100);
    [SerializeField] private TMP_Text _healthText;
    
    // Player Takes Damage
    [ServerRpc]
    public void takeDamage(int damage)
    {
        _health.value = Math.Max(0, _health.value - damage);
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

    public int getHealth()
    {
        return _health.value;
    }
}