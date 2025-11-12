using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action OnGameStart;
    public event Action OnGameEnd;

    private void Start()
    {
        Time.timeScale = 1;  
    }
}