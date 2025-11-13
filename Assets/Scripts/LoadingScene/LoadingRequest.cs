using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingRequest
{
    public string targetSceneName;
    public LoadSceneMode loadMode;
    public List<LoadingTask> tasks;
    public Action onLoadingComplete;

    public LoadingRequest(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        targetSceneName = sceneName;
        loadMode = mode;
        tasks = new List<LoadingTask>();
    }

    public void AddTask(string taskName, Func<CancellationToken, UniTask> taskAction, float weight = 1f)
    {
        tasks.Add(new LoadingTask(taskName, taskAction, weight));
    }
}