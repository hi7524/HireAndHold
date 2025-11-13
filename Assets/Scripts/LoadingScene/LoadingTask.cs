using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class LoadingTask
{
    public string taskName;
    public Func<CancellationToken, UniTask> taskAction;
    public float weight; // 작업 비중 (0~1)

    public LoadingTask(string name, Func<CancellationToken, UniTask> action, float weight = 1f)
    {
        this.taskName = name;
        this.taskAction = action;
        this.weight = weight;
    }
}