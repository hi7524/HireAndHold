using UnityEngine;
using UnityEngine.UI;

public class Ore : MonoBehaviour
{
    [SerializeField] private Image oreImage;
    
    private int touchCount;

    
    public void SetTouchCount(int count = 1)
    {
        touchCount = count;
    }

    public void OnClick()
    {
        Debug.Log("광석 클릭");
    }

    
}