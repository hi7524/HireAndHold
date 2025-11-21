using UnityEngine;
using UnityEngine.UI;

public class StageCard : MonoBehaviour
{
    public int stageIndex;
    public bool isLocked = true;
    public ScaleAnimator scaleAnimator;

    [Header("UI")]
    public GameObject lockOverlay;
    public Image stageImage;
    public Text stageName;

    private bool isSelected = false;

    private void Awake()
    {
        scaleAnimator = GetComponent<ScaleAnimator>();
    }

    public void ApplyData(StageUIData data)
    {
        stageName.text = data.stageName;
        stageImage.sprite = data.stageImage;
        SetLocked(data.isLocked);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(locked);
        }
    }

    public void Select()
    {
        if (isLocked)
        {
            return;
        }
        if (isSelected)
        {
            return;
        }

        isSelected = true;
        scaleAnimator.PlaySelect();
    }

    public void Deselect()
    {
        if (!isSelected)
        {
            return;
        }

        isSelected = false;
        scaleAnimator.PlayDeselect();
    }
}
