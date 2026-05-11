using UnityEngine;

public class StartAnimationRandomised : MonoBehaviour
{
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        var state = animator.GetAnimatorTransitionInfo(0);
        animator.Play(state.fullPathHash, (0), normalizedTime: Random.Range(0f, 1f));
    }
}
