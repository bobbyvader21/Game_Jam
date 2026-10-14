using UnityEngine;

public class CombatHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public bool isPlayer;

    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        if (!IsDead)
            return;

        if (isPlayer)
        {
            playerController movement = GetComponentInChildren<playerController>();
           
            if (movement != null)
                movement.enabled = false;

            LightningAttack attack = GetComponentInChildren<LightningAttack>();

            if (attack != null)
                attack.enabled = false;

            Rigidbody body = GetComponentInChildren<Rigidbody>();

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        if (!isPlayer)
            return;

        GUI.Label(
            new Rect(15, 15, 250, 30),
            $"Health: {Mathf.CeilToInt(CurrentHealth)} / {maxHealth}"
        );

        if (IsDead)
        {
            GUI.Label(
                new Rect(
                    Screen.width / 2f - 100,
                    Screen.height / 2f + 35,
                    300,
                    30
                ),
                "Defeated — restart Play Mode."
            );
        }
    }
}