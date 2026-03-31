using UnityEngine;

public class Boss : NonPlayerEntity
{
    [Header("Boss Setup")]
    [SerializeField] float rotateAngularSpeed = 10f;

    Vector3 basePOSThis;
    Vector3 basePOSTarget;

    protected override void Die(Entity damageOrigin) {
        base.Die(damageOrigin);

        if (!isServer) return; // Only execute end game logic on the server

        //REMOVE WHEN ANIMATION IS IN
        Destroy(gameObject);
    }
    protected override void ServerUpdate() {
        Entity currentTarget = GetTarget();
        if (hasTarget && !currentTarget) {
            ResetTarget();
        }

        if (!isDead) {
            FindTarget();
            currentTarget = GetTarget();
            Rotate(currentTarget);
            AttackTimer();
            Attack(currentTarget);
        }

        base.ServerUpdate();
    }
    protected override void ClientUpdate() {
        base.ClientUpdate();
    }
    protected override void Attack(Entity currentTarget) {
        base.Attack();

        if (!isServer) return;

        //If there is a target and it is within attack range
        if (currentTarget && CheckTargetInAttackRange(currentTarget) && attackCooldownTimer.value <= 0) {

            if (currentTarget == this) {
                Debug.Log(gameObject.name + " is targeting self -- Attack()");
            }

            animator.SetTrigger("Attack");
            //deal dmg directly to target
            currentTarget.TakeDamage(attackPower, this);
            attackCooldownTimer.value = defaultAttackCooldown.value;
        }
    }
    bool CheckTargetInAttackRange(Entity currentTarget) {
        if (currentTarget) {
            //Flatten y to only check horizontal distance
            basePOSThis = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
            basePOSTarget = new Vector3(currentTarget.transform.position.x, 0, currentTarget.transform.position.z);
            if (Vector3.Distance(basePOSTarget, basePOSThis) <= attackRange) return true;
        }
        return false;
    }
    void Rotate(Entity currentTarget) {
        if (!isServer) return;

        if (currentTarget) {
            //Get rotate direction
            Vector3 direction = currentTarget.transform.position - transform.position;
            direction.y = 0f; // keep upright

            if (direction.sqrMagnitude < 0.001f) return;

            //Rotate
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateAngularSpeed / 2 * Time.deltaTime);
        }
    }
}
