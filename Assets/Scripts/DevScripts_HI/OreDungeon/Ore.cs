using UnityEngine;
using UnityEngine.UI;

public class Ore : MonoBehaviour
{
    [SerializeField] private Image oreImage;

    private int touchCount;
    private int oresID;

    public void SetOreType(int id)
    {
        oresID = id;
        Debug.Log($"광석 타입 설정: {oresID}");
    }

    public void SetTouchCount(int count = 1)
    {
        touchCount = count;
    }

    public void OnClick()
    {
        Debug.Log($"광석 클릭 (타입: {oresID})");
        touchCount--;
        TEST();
    }

    private void TEST()
    {
        if (touchCount <= 0)
        {
            gameObject.SetActive(false);
        }
    }
}