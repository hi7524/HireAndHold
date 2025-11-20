using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public float ElapsedTime { get; private set; }
    public bool IsGameStarted { get; private set; }
    public bool IsPausedGame { get; private set; }

    public int CurSpeedLevel => speedLevels[curSpeedLevelIdx];

    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action OnGameEnd;

    private int[] speedLevels = { 1, 2, 3 };
    private float originalSpeed;
    private int curSpeedLevelIdx;

    private StageTimeEventScheduler timeScheduler = new StageTimeEventScheduler();


    private void Start()
    {
        Reset();
    }

    private void Update()
    {
        if (!IsGameStarted)
            return;

        ElapsedTime += Time.deltaTime;
        timeScheduler.UpdateTime(Time.deltaTime);
    }

    // 게임 재설정
    private void Reset()
    {
        curSpeedLevelIdx = 0;
        IsGameStarted = false;
        Time.timeScale = 0;

        ElapsedTime = 0f;
        timeScheduler.Reset();
    }

    // 게임 시작
    public void StartGame()
    {
        GameManagerLog("게임 시작");

        IsGameStarted = true;
        Time.timeScale = speedLevels[0];
        OnGameStart?.Invoke();
    }

    // 게임 일시 정지
    public void PauseGame()
    {
        if (!IsGameStarted)
            return;

        GameManagerLog("일시 정지");

        originalSpeed = Time.timeScale;
        Time.timeScale = 0f;
        IsPausedGame = true;
        OnGamePause?.Invoke();
    }

    // 게임 재개
    public void ResumeGame()
    {
        if (!IsGameStarted)
            return;

        GameManagerLog("게임 재개");

        Time.timeScale = originalSpeed;
        IsPausedGame = false;
        OnGameResume?.Invoke();
    }

    // 게임 종료
    public void GameEnd()
    {
        GameManagerLog("게임 종료");

        Time.timeScale = 0f;
        OnGameEnd?.Invoke();
    }

    // 게임 플레이 속도 변경
    public void ChangeSpeed()
    {
        curSpeedLevelIdx = (curSpeedLevelIdx + 1) % speedLevels.Length;
        Time.timeScale = speedLevels[curSpeedLevelIdx];
    }

    // 시간에 따른 이벤트 추가
    public TimeEvent AddTimeEvent(int minutes, int seconds, Action callback)
    {
        float totalSeconds = minutes * 60f + seconds;
        return timeScheduler.AddTimeEvent(totalSeconds, callback);
    }

    // 이벤트 삭제
    public bool RemoveTimeEvent(TimeEvent timeEvent)
    {
        return timeScheduler.RemoveTimeEvent(timeEvent);
    }

    private void GameManagerLog(string msg)
    {
        Debug.Log($"<color=#E155E1>{msg}</color>");
    }
}