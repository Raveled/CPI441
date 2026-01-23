using UnityEngine;

public class Core : NonPlayerEntity 
{
    [Header("Core Setup")]
    [SerializeField] Entity protector;

    GameManager gameManager;
    protected override void Start() {
        gameManager = FindFirstObjectByType<GameManager>();
        base.Start();
    }
    protected override void TakeDamage(int damage, Entity damageOrigin) {
        if (!protector || protector.GetIsDead()) {
            base.TakeDamage(damage, damageOrigin);
        }
    }
    protected override void DestroyThis(Entity damageOrigin) {
        gameManager.CoreDestroyed((int)team);
        //play destroy animation
    }
}
