using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 60f;
    float currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
