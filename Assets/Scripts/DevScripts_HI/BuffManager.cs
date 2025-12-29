using System;
using System.Collections.Generic;
using UnityEngine;
using Tutorial;

// 버프 조건 체크 및 관리
public class BuffManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private StageUiManager uiManager;
    [SerializeField] private ParticleSystem buffParticle;

    private const float CellBuffRate = 1;   // (%)
    private const float TotalBuffRate = 50; // (%)

    public float GlobalBuffPercentage { get; private set; } // 전역 버프 값 (%)
    public HashSet<string> ActivatedBuffs { get; private set; } = new HashSet<string>();

    public event Action<float> OnBuffPercentageChanged;
    public event Action<HashSet<string>> OnActivatedBuffsChanged;

    private int[,] gridArray => gridManager.GridArray;
    private GridLayoutData layoutData => gridManager.LayoutData;

    private bool isFilledAllGrids;


    // 모든 버프 조건 체크
    public void CheckAllBuffConditions()
    {
        if (gridManager == null || layoutData == null)
            return;

        // 이전 버프 값 저장
        float previousBuffPercentage = GlobalBuffPercentage;

        // 버프 값 초기화
        GlobalBuffPercentage = 0f;

        // 십자가 버프
        if (layoutData.enableCrossBuffs)
        {
            foreach (var crossBuff in layoutData.crossBuffs)
            {
                bool horizontalFilled = IsHorizontalLineFilled(crossBuff.centerPos);
                bool verticalFilled = IsVerticalLineFilled(crossBuff.centerPos);

                // 가로줄 버프 활성화/비활성화
                if (horizontalFilled && !ActivatedBuffs.Contains(crossBuff.horizontalBuffName))
                {
                    ActivateBuff(crossBuff.horizontalBuffName);
                    ActivatedBuffs.Add(crossBuff.horizontalBuffName);

                    // 이벤트 발생 (TutorialManager가 구독해서 처리)
                    GameEvents.RaiseBuffActivated(crossBuff.horizontalBuffName);
                }
                else if (!horizontalFilled && ActivatedBuffs.Contains(crossBuff.horizontalBuffName))
                {
                    DeactivateBuff(crossBuff.horizontalBuffName);
                    ActivatedBuffs.Remove(crossBuff.horizontalBuffName);
                }

                // 세로줄 버프 활성화/비활성화
                if (verticalFilled && !ActivatedBuffs.Contains(crossBuff.verticalBuffName))
                {
                    ActivateBuff(crossBuff.verticalBuffName);
                    ActivatedBuffs.Add(crossBuff.verticalBuffName);

                    // 이벤트 발생 (TutorialManager가 구독해서 처리)
                    GameEvents.RaiseBuffActivated(crossBuff.verticalBuffName);
                }
                else if (!verticalFilled && ActivatedBuffs.Contains(crossBuff.verticalBuffName))
                {
                    DeactivateBuff(crossBuff.verticalBuffName);
                    ActivatedBuffs.Remove(crossBuff.verticalBuffName);
                }

                // 버프 퍼센트 계산
                GlobalBuffPercentage += CalculateCrossBuffValue(crossBuff.centerPos, horizontalFilled, verticalFilled);
            }
        }

        // 영역 버프
        if (layoutData.enableRegionBuffs)
        {
            foreach (var regionBuff in layoutData.regionBuffs)
            {
                int filledCount = CountFilledCellsInRegion(regionBuff.regionCells);

                // 영역이 완전히 채워졌을 때만 버프 활성화
                if (filledCount == regionBuff.regionCells.Count && !ActivatedBuffs.Contains(regionBuff.buffName))
                {
                    ActivateBuff(regionBuff.buffName);
                    ActivatedBuffs.Add(regionBuff.buffName);

                    // 이벤트 발생 (TutorialManager가 구독해서 처리)
                    GameEvents.RaiseBuffCompleted(TutorialConditions.COMPLETE_BUFF);
                }
                else if (filledCount < regionBuff.regionCells.Count && ActivatedBuffs.Contains(regionBuff.buffName))
                {
                    DeactivateBuff(regionBuff.buffName);
                    ActivatedBuffs.Remove(regionBuff.buffName);
                }

                GlobalBuffPercentage += filledCount * CellBuffRate;
            }
        }

        // 전체 채우기 버프 체크
        CheckFilledAllCells();
        if (isFilledAllGrids && !ActivatedBuffs.Contains("컴플리트"))
        {
            ActivateBuff("컴플리트");
            ActivatedBuffs.Add("컴플리트");

            GlobalBuffPercentage += TotalBuffRate;

            // 이벤트 발생 (TutorialManager가 구독해서 처리)
            GameEvents.RaiseBuffCompleted(TutorialConditions.COMPLETE_BUFF);
        }
        else if (!isFilledAllGrids && ActivatedBuffs.Contains("컴플리트"))
        {
            DeactivateBuff("컴플리트");
            ActivatedBuffs.Remove("컴플리트");

            GlobalBuffPercentage -= TotalBuffRate;
        }

        // 버프 퍼센트가 변경되었으면 이벤트 발생
        if (Math.Abs(GlobalBuffPercentage - previousBuffPercentage) > 0.01f)
        {
            OnBuffPercentageChanged?.Invoke(GlobalBuffPercentage);
        }

        // 활성화된 버프 목록 이벤트 발생
        OnActivatedBuffsChanged?.Invoke(ActivatedBuffs);

        // 버프 개수에 따라 파티클 방출 속도 업데이트
        UpdateBuffParticleEmission();
    }

    // 가로줄이 완전히 채워졌는지 체크
    private bool IsHorizontalLineFilled(Vector2Int centerPos)
    {
        for (int x = 0; x < layoutData.width; x++)
        {
            if (!layoutData.IsValidCell(x, centerPos.y))
                continue;

            if (gridArray[x, centerPos.y] != (int)GridState.Occupied)
                return false;
        }
        return true;
    }

    // 세로줄이 완전히 채워졌는지 체크
    private bool IsVerticalLineFilled(Vector2Int centerPos)
    {
        for (int y = 0; y < layoutData.height; y++)
        {
            if (!layoutData.IsValidCell(centerPos.x, y))
                continue;

            if (gridArray[centerPos.x, y] != (int)GridState.Occupied)
                return false;
        }
        return true;
    }

    // 십자가 버프 값 계산
    private float CalculateCrossBuffValue(Vector2Int centerPos, bool horizontalFilled, bool verticalFilled)
    {
        float buffValue = 0f;

        // 가로줄이 채워졌으면 가로 칸 수만큼 버프 추가
        if (horizontalFilled)
        {
            int horizontalCellCount = 0;
            for (int x = 0; x < layoutData.width; x++)
            {
                if (layoutData.IsValidCell(x, centerPos.y))
                    horizontalCellCount++;
            }
            buffValue += horizontalCellCount * CellBuffRate;
        }

        // 세로줄이 채워졌으면 세로 칸 수만큼 버프 추가
        if (verticalFilled)
        {
            int verticalCellCount = 0;
            for (int y = 0; y < layoutData.height; y++)
            {
                if (layoutData.IsValidCell(centerPos.x, y))
                    verticalCellCount++;
            }
            buffValue += verticalCellCount * CellBuffRate;
        }

        // 가로와 세로 모두 채워졌을 때 중복되는 중심 칸 1개 제거
        if (horizontalFilled && verticalFilled)
            buffValue -= CellBuffRate;

        return buffValue;
    }

    // 특정 위치의 공격력 버프 계산
    public float GetAttackBuffPercentage(Vector2Int position)
    {
        return GlobalBuffPercentage;
    }

    // 영역 내에서 채워진 칸 개수 세기
    private int CountFilledCellsInRegion(List<Vector2Int> region)
    {
        int count = 0;
        foreach (var pos in region)
        {
            if (gridArray[pos.x, pos.y] == (int)GridState.Occupied)
                count++;
        }
        return count;
    }

    // 칸이 모두 채워져있는지 체크
    private void CheckFilledAllCells()
    {
        isFilledAllGrids = false;

        for (int x = 0; x < layoutData.width; x++)
        {
            for (int y = 0; y < layoutData.height; y++)
            {
                if (!layoutData.IsValidCell(x, y))
                    continue;

                if (gridArray[x, y] == (int)GridState.Empty)
                    return;
            }
        }

        isFilledAllGrids = true;
    }

    // 버프 활성화
    private void ActivateBuff(string buffName)
    {
        if (uiManager != null)
            uiManager.UpdateInfoText($"{buffName} 활성화!");

        if (gridManager != null)
        {
            gridManager.UpdateBuffColors();
            gridManager.PlayBuffActivationEffect(buffName);
        }
    }

    // 버프 비활성화
    private void DeactivateBuff(string buffName)
    {
        if (uiManager != null)
            uiManager.UpdateInfoText($"{buffName} 해제!");

        if (gridManager != null)
        {
            gridManager.UpdateBuffColors();
        }
    }

    // 버프 개수에 따라 파티클 방출 속도 업데이트
    private void UpdateBuffParticleEmission()
    {
        if (buffParticle == null)
            return;

        int buffCount = ActivatedBuffs.Count;
        float rateOverTime;

        // 버프 개수에 따라 방출 속도 결정
        if (buffCount == 0)
            rateOverTime = 0f;
        else if (buffCount == 1)
            rateOverTime = 2f;
        else if (buffCount == 2)
            rateOverTime = 5f;
        else // buffCount >= 3
            rateOverTime = 10f;

        // Emission 모듈 가져오기 및 설정
        var emission = buffParticle.emission;
        emission.rateOverTime = rateOverTime;

        // 버프가 있으면 파티클 재생
        if (buffCount > 0)
        {
            if (!buffParticle.isPlaying)
            {
                buffParticle.Play();
            }
        }
    }
}