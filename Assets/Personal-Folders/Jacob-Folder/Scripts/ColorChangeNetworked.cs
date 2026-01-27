using UnityEngine;
using PurrNet;
using TMPro;

public class ColorChangeNetworked : NetworkIdentity
{
    private Renderer _renderer;
    
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
}