using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GachaResult
{
    public List<GachaItem> items;
    public GachaType type;
    public int count;
    public DateTime timestamp;

    /// <summary>
    /// Firebase 저장 작업 (문 열림 애니메이션 중 대기용)
    /// </summary>
    public UniTask SaveTask { get; set; } = UniTask.CompletedTask;

    public GachaResult(List<GachaItem> items, GachaType type)
    {
        this.items = items;
        this.type = type;
        this.count = items.Count;
        this.timestamp = DateTime.Now;
    }

    /// <summary>
    /// Firebase 저장 완료 대기
    /// </summary>
    public async UniTask WaitForSaveAsync()
    {
        try
        {
            await SaveTask;
            Debug.Log("[GachaResult] Firebase 저장 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GachaResult] Firebase 저장 실패: {ex.Message}");
        }
    }
}
