using System;
using UnityEngine;

public class DailyRewardPanel : MonoBehaviour
{
    [SerializeField] DailyRewardSlot[] slots;

    private void Start()
    {
        // 현재 날짜와 시간
        DateTime now = DateTime.Now;

        // 날짜 기반 ID 생성
        // 일일 출석체크 보상은 28일까지만 고정이므로 오늘 날짜가 28일을 넘었다면 따로 보상을 지급하지 않음
        if (now.Day <= 28)
        {
           string dateId = $"23{now.Month:D2}{now.Day:D2}";
        }
    }
}