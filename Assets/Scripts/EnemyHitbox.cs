using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    public float damage = 15f;
    public float hitCooldown = 0.4f;

    float nextHitTime = 0f;

    void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryHit(other);
    }

    void TryHit(Collider other)
    {
        if (Time.time < nextHitTime) return;

        var hp = other.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;

        hp.TakeDamage(damage);
        nextHitTime = Time.time + hitCooldown;
    }
}
