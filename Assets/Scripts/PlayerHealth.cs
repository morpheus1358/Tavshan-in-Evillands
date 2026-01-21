using UnityEngine;
using System;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    TopDownCharacterController controller;
    Animator animator;
    bool isDead = false;

    void Awake()
    {
        controller = GetComponent<TopDownCharacterController>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
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
        if (isDead) return;
        isDead = true;

        if (controller != null)
            controller.SetDead(); // ✅ lock gameplay first

        if (animator != null)
            animator.CrossFade("Death", 0.05f); // ✅ then play death

        // Optional: disable only AFTER a tiny delay so animator still updates
        StartCoroutine(DisableAfterDeath());
    }

    IEnumerator DisableAfterDeath()
    {
        yield return new WaitForSeconds(0.1f);

        // stop movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // (Optional) disable colliders AFTER Death starts
        // If you disable everything immediately, some setups can glitch.
        // Disable ONLY trigger hitboxes (like sword hitbox), keep body collider ON
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols)
        {
            if (c.isTrigger)
                c.enabled = false;
        }

    }

}
