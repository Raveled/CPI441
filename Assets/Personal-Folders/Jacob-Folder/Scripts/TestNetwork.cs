using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestNetwork : NetworkIdentity
{
    [SerializeField] private NetworkIdentity networkIdentity;
    [SerializeField] private Color _color;
    [SerializeField] private InputAction interactAction1;
    [SerializeField] private InputAction interactAction2;
    
    private NetworkIdentity _spawnedInstance;
    
    //Note using Awake to spawn objects has conflicts on clients since the network needs preparation time
    
    // SPAWN OBJECT FROM SERVER
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (!asServer)
        {
            Debug.Log("TestNetwork: OnSpawned -- Not Server");
            return;
        }

        Debug.Log("TestNetwork: OnSpawned -- Server");
        var instance = Instantiate(networkIdentity, Vector3.zero, Quaternion.identity);
        _spawnedInstance = instance;
        
        SyncSpawnedInstanceObserversRpc(instance);
    }
    
    [ObserversRpc(bufferLast:true)]
    private void SyncSpawnedInstanceObserversRpc(NetworkIdentity instance)
    {
        // Clients receive the reference directly
        _spawnedInstance = instance;
        Debug.Log($"Client: Synced spawned instance");
    }
    
    // INTERACTION
    private void OnEnable()
    {
        interactAction1.Enable();
        interactAction1.performed += OnInteract1;
        
        interactAction2.Enable();
        interactAction2.performed += OnInteract2;
    }

    private void OnDisable()
    {
        interactAction1.performed -= OnInteract1;
        interactAction1.Disable();
        
        interactAction2.performed -= OnInteract2;
        interactAction2.Disable();
    }

    private void OnInteract1(InputAction.CallbackContext context)
    {
        // Request color change
        RequestColorChange(_color);
    }
    
    private void OnInteract2(InputAction.CallbackContext context)
    {
        RequestDamage(10);
    }
    
    [ObserversRpc(bufferLast:true)]
    private void RequestColorChange(Color newColor)
    {
        Debug.Log("Color change request");
        
        // Tell the spawned object to change color
        if (_spawnedInstance != null)
        {
            var objectNetworkTest = _spawnedInstance.GetComponent<NetworkedObjectTest>();
            if (objectNetworkTest != null)
            {
                objectNetworkTest.ChangeColor(newColor);
            }
        }
    }
    
    [ServerRpc]
    private void RequestDamage(int dmg)
    {
        Debug.Log("Damage request");
        
        // Tell the spawned object to take damage
        if (_spawnedInstance != null)
        {
            var objectNetworkTest = _spawnedInstance.GetComponent<NetworkedObjectTest>();
            if (objectNetworkTest != null)
            {
                objectNetworkTest.takeDamage(dmg);
            }
        }
    }
    
    // CHANGE COLOR
    /*
     * ServerRPC = Client (Server) -> Server
     * ObserversRPC = Server (Client) -> All Clients
     * TargetRPC = Server (Client) -> Single Client
     *
     * Unsafe rules in purrnet allow Server and Client to act as similar agents
     */
}

