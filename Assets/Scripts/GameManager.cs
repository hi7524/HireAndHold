using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameEnd;

    private void Start()
    {
        Time.timeScale = 0;  
    }

    public void StartGame()
    {
        Debug.Log("게임 시작");
        OnGameStart?.Invoke();
    }

    public void PauseGame()
    {
        Debug.Log("일시 정지");
        OnGamePause?.Invoke();
    }

    public void ResumeGame()
    {
        Debug.Log("게임 재개");
        OnGameResume?.Invoke();
    }
    
    public void GameEnd()
    {
        Debug.Log("게임 종료");
        OnGameEnd?.Invoke();
    }
}