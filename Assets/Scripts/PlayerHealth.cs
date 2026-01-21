using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDied;

    TopDownCharacterController controller;

    void Awake()
    {
        controller = GetComponent<TopDownCharacterController>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        // I-FRAMES CHECK
        if (controller != null && controller.IsInvincible)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0f, currentHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        OnDied?.Invoke();
        Debug.Log("Player died!");

        // basic: disable controller (you can replace with death anim later)
        if (controller != null)
            controller.enabled = false;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
