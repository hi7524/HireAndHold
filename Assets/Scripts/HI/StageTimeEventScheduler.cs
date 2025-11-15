using System;
using System.Collections.Generic;

public class TimeEvent
{
    public float TriggerTime { get; }
    public Action Callback { get; }

    public TimeEvent(float triggerTime, Action callback)
    {
        TriggerTime = triggerTime;
        Callback = callback;
    }
}

public class StageTimeEventScheduler
{
    private float elapsedTime;
    private List<TimeEvent> scheduledEvents = new List<TimeEvent>();

    // 이벤트 추가
    public TimeEvent AddTimeEvent(float triggerTime, Action callback)
    {
        var timeEvent = new TimeEvent(triggerTime, callback);

        scheduledEvents.Add(timeEvent);
        scheduledEvents.Sort((a, b) => a.TriggerTime.CompareTo(b.TriggerTime));
        return timeEvent;
    }

    // 이벤트 제거
    public bool RemoveTimeEvent(TimeEvent timeEvent)
    {
        return scheduledEvents.Remove(timeEvent);
    }

    // 시간 업데이트 및 실행
    public void UpdateTime(float deltaTime)
    {
        elapsedTime += deltaTime;

        int removeCount = 0;
        for (int i = 0; i < scheduledEvents.Count; i++)
        {
            if (scheduledEvents[i].TriggerTime <= elapsedTime)
            {
                scheduledEvents[i].Callback?.Invoke();
                removeCount++;
            }
            else
            {
                break;
            }
        }

        if (removeCount > 0)
            scheduledEvents.RemoveRange(0, removeCount);
    }

    // 초기화
    public void Reset()
    {
        elapsedTime = 0f;
        scheduledEvents.Clear();
    }
}