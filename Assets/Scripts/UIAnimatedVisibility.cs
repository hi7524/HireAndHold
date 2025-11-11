using UnityEngine;

public class UIAnimatedVisibility : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    
    public void Show()
    {
        animator.SetBool(AnimParams.IsActive, true);
    }
    
    public void Hide()
    {
        animator.SetBool(AnimParams.IsActive, false);
    }
    
    public void Toggle()
    {
        bool current = animator.GetBool(AnimParams.IsActive);
        animator.SetBool(AnimParams.IsActive, !current);
    }
}