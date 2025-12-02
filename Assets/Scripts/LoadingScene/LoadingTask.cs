using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class LoadingTask
{
    public string taskName;
    public Func<CancellationToken, IProgress<float>, UniTask> taskAction;
    public float weight;
    public int maxRetryCount;
    public bool isCritical;  // 실패 시 로딩 중단 여부

    // 기본 생성자 (내부 진행률 없음)
    public LoadingTask(string name, Func<CancellationToken, UniTask> action, float weight = 1f, bool isCritical = true)
    {
        this.taskName = name;
        this.taskAction = (ct, progress) => action(ct);
        this.weight = weight;
        this.maxRetryCount = 3;
        this.isCritical = isCritical;
    }

    // 진행률 지원 생성자
    public LoadingTask(string name, Func<CancellationToken, IProgress<float>, UniTask> action, float weight = 1f, bool isCritical = true)
    {
        this.taskName = name;
        this.taskAction = action;
        this.weight = weight;
        this.maxRetryCount = 3;
        this.isCritical = isCritical;
    }
}