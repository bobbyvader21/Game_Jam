using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private PlayerAttributes playerAttributes;


    public bool IsDead => playerAttributes.Health <= 0f;
    private PlayerAnimationController playerAnimationController;
    private playerController playerController;
    private LightningAttack lightningAttack;
    private Rigidbody playerRigidbody;
    private Camera_Persp_Change cameraPerspChange;

    private void Start()
    {
        playerAnimationController = GetComponentInChildren<PlayerAnimationController>();
        playerController = GetComponentInChildren<playerController>();
        lightningAttack = GetComponentInChildren<LightningAttack>();
        playerRigidbody = GetComponentInChildren<Rigidbody>();
        cameraPerspChange = GetComponentInChildren<Camera_Persp_Change>();
    }

    private void OnEnable()
    {
        playerAttributes.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        playerAttributes.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        // Handle player death (e.g., play animation, respawn)
        playerAnimationController.enabled = false;

        playerController.enabled = false;

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.isKinematic = true;

        lightningAttack.enabled = false;

        // might move this somewhere else, because it makes no sense for the player health to handle the cursor, but for now it works
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void TakeDamage(float damage)
    {
        playerAttributes.Health -= damage;
    }
    public void Heal(float amount)
    {
        playerAttributes.Health += amount;
    }
}
