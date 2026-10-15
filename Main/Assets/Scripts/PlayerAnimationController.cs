using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody playerRigidbody;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private playerController playerControllerScript;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerRigidbody = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        animator.SetFloat("x", playerRigidbody.linearVelocity.z);
        animator.SetBool("isGrounded", playerControllerScript.IsGrounded);
        animator.SetFloat("y", playerRigidbody.linearVelocity.y);
        if (playerControllerScript.IsJumping)
        {
            animator.SetTrigger("Jump");
        }
        FlipCharacter();
    }
    private void FlipCharacter()
    {
        // Flip using local scale so attached colliders/checkers flip too
        if (playerRigidbody.linearVelocity.z > 0.1f)
            spriteRenderer.flipX = false;
        else if (playerRigidbody.linearVelocity.z < -0.1f)
            spriteRenderer.flipX = true;
    }
}
