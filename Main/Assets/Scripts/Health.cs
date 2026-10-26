using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float currentHealth;
    [SerializeField] private float maxHealth;

    private Action<float> onHealthChanged;
    private Action onDeath;

    public float CurrentHealth
    {
        get => currentHealth;
        set
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth);
            if (currentHealth <= 0)
            {
                OnDeath?.Invoke();
            }

        }
    }
    public float MaxHealth { get => maxHealth; set => maxHealth = value; }
    public Action OnDeath { get => onDeath; set => onDeath = value; }
    public Action<float> OnHealthChanged { get => onHealthChanged; set => onHealthChanged = value; }

    private void Start()
    {
        CurrentHealth = maxHealth;
    }

    private void OnEnable()
    {
        OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        // Handle enemy death (e.g., play animation, destroy object)
        gameObject.SetActive(false);
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
    }
}
    

