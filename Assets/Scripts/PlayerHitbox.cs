using UnityEngine;
using System.Collections.Generic;

public class PlayerHitbox : MonoBehaviour
{
    public float damage = 25f;

    HashSet<Collider> hitThisSwing = new();

    void OnEnable()
    {
        hitThisSwing.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hitThisSwing.Contains(other)) return;

        var enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) return;

        enemyHealth.TakeDamage(damage);
        hitThisSwing.Add(other);
    }
}
