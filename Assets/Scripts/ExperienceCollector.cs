using System;
using UnityEngine;

// KHI: 경험치 오브젝트 자동 수집
public class ExperienceCollector : MonoBehaviour
{
    [SerializeField] private float collectInterval = 5f;

    public event Action<Vector3> OnCollectTriggered;

    private float lastCollectedTime;


    private void Update()
    {
        if (Time.time >= lastCollectedTime + collectInterval)
        {
            TriggerCollection();
            lastCollectedTime = Time.time;
        }
    }

    public void TriggerCollection()
    {
        OnCollectTriggered?.Invoke(transform.position);
    }
}