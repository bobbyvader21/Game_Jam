using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAttributes", menuName = "Scriptable Objects/PlayerAttributes")]
public class PlayerAttributes : ScriptableObject
{
    [SerializeField] private float health;
    [SerializeField] private float maxHealth;

    private Action<float> onHealthChanged;
    private Action onDeath;

    public float Health { 
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

    private void OnEnable()
    {
        health = maxHealth;
    }
    private void OnDisable()
    {
        onHealthChanged = null;
        onDeath = null;
    }
}

