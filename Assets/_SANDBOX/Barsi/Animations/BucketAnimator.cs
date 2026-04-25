using UnityEngine;

public class BucketAnimator : MonoBehaviour
{
    [Range(0.5f, 3f)]
    [SerializeField] private float walkAnimSpeed = 1f;

    [SerializeField] private Animator animator;


    void Start()
    {
        if (animator != null)
        {
            animator.SetFloat("walkSpeedMultiplier", walkAnimSpeed);
        }
    }

    public void ToggleAnimation(bool state)
    {
        if (animator != null)
        {
            animator?.SetBool("isMoving", state);
        }

    }
}