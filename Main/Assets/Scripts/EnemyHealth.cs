using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float health;
    [SerializeField] private float maxHealth;

    private Action<float> onHealthChanged;
    private Action onDeath;

    public float Health
    {
        get => health;
        set
        {
            health = Mathf.Clamp(value, 0, maxHealth);
            OnHealthChanged?.Invoke(health);
            if (health <= 0)
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
        Health = maxHealth;
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
        Health += amount;
    }

    public void TakeDamage(float damage)
    {
        Health -= damage;
    }
}
    

