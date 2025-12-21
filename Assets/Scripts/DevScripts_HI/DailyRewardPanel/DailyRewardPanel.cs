using System;
using UnityEngine;

public class DailyRewardPanel : MonoBehaviour
{
    [SerializeField] DailyRewardSlot[] slots;

    private void Start()
    {
        // 현재 날짜와 시간
        DateTime now = DateTime.Now;
        
        Debug.Log($"월: {now.Month}"); // 12
        Debug.Log($"일: {now.Day}"); // 21
    }
}