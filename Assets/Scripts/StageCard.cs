using UnityEngine;

public class StageCard : MonoBehaviour
{
    public int stageIndex;
    public bool isLocked = true;
    public ScaleAnimator scaleAnimator;

    private void Awake()
    {
        scaleAnimator = GetComponent<ScaleAnimator>();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

    }

    public void Select()
    {
        if (isLocked)
        {
            return;
        }

        scaleAnimator.PlaySelect();
    }

    public void Deselect()
    {
        scaleAnimator.PlayDeselect();
    }
}
