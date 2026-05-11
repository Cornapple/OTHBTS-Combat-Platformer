using UnityEngine;

public class AnimationRandomRepeat : StateMachineBehaviour
{
    public int minLoops = 1;
    public int maxLoops = 3;

    private int currentLoops;
    private int targetLoops;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        currentLoops = 0;
        targetLoops = Random.Range(minLoops, maxLoops + 1);

        // Reset both parameters at the start of the state
        animator.SetInteger("loopCount", 0);
        animator.SetBool("canTransition", false);
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime >= currentLoops + 1)
        {
            currentLoops++;
            animator.SetInteger("loopCount", currentLoops);

            if (currentLoops >= targetLoops)
            {
                animator.SetBool("canTransition", true);
            }
        }
    }
}

