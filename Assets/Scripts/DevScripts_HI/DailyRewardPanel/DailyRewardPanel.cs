using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class DailyRewardPanel : MonoBehaviour
{
    [SerializeField] private DailyRewardSlot[] slots;
    [SerializeField] private GameObject interactiveAreaObj;
    [SerializeField] private GameObject rewardClaimedPanel;

    private void OnEnable()
    {
        UpdateInteractiveArea();
    }

    private async void Start()
    {
        await InitializePanelAsync();
    }

    private void UpdateInteractiveArea()
    {
        if (DatabaseManager.Instance == null)
        {
            interactiveAreaObj.SetActive(false);
            return;
        }

        // 오늘 보상을 받았는지 체크
        bool hasClaimed = DatabaseManager.Instance.HasClaimedToday();
        interactiveAreaObj.SetActive(!hasClaimed);
    }

    private async UniTask InitializePanelAsync()
    {
        // DatabaseManager 초기화 대기
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("[DailyReward] DatabaseManager 인스턴스가 없습니다");
            return;
        }

        await DatabaseManager.Instance.WaitForInitializationAsync();

        DateTime now = DateTime.Now;

        // 28일 체크 시스템이므로 28일 초과면 리턴
        if (now.Day > 28)
            return;

        // 현재 월의 각 날짜 상태 표시
        for (int day = 1; day <= 28; day++)
        {
            DateTime dateToCheck = new DateTime(now.Year, now.Month, day);
            int slotIndex = day - 1;

            // 해당 날짜의 보상 데이터 가져오기
            int rewardId = int.Parse($"23{now.Month:D2}{day:D2}");
            var rewardData = DataTableManager.DailyRewardTable.Get(rewardId);

            if (rewardData != null)
            {
                // 보상 타입이 아이템(2)인 경우 ItemTable에서 정보 불러오기
                if (rewardData.REWARD_TYPE == 2)
                {
                    var itemData = DataTableManager.ItemTable.Get(rewardData.REWARD_ID);
                    if (itemData != null)
                    {
                        slots[slotIndex].SetItemIcon(itemData.ITEM_ICON);
                    }
                }
            }

            if (day < now.Day)
            {
                // 과거 날짜
                if (DatabaseManager.Instance.HasClaimedOnDate(dateToCheck))
                {
                    slots[slotIndex].MarkAsClaimed();
                }
                else
                {
                    slots[slotIndex].MarkAsMissed();
                }
            }
            else if (day == now.Day)
            {
                // 오늘 날짜
                slots[slotIndex].MarkAsToday();

                if (DatabaseManager.Instance.HasClaimedToday())
                {
                    slots[slotIndex].MarkAsClaimed();
                }
            }
            // 미래 날짜는 기본 상태(잠김)
        }
    }

    public async void OnClickRewardPanel()
    {
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("[DailyReward] DatabaseManager 인스턴스가 없습니다");
            return;
        }

        if (DatabaseManager.Instance.HasClaimedToday())
        {
            Debug.Log("[DailyReward] 오늘 이미 출석 체크함");
            return;
        }

        DateTime now = DateTime.Now;
        if (now.Day > 28)
        {
            Debug.Log("[DailyReward] 28일 초과");
            return;
        }

        // 오늘 날짜에 해당하는 보상 데이터 가져오기
        int todayId = int.Parse($"23{now.Month:D2}{now.Day:D2}");
        var rewardData = DataTableManager.DailyRewardTable.Get(todayId);

        if (rewardData == null)
        {
            Debug.LogError($"[DailyReward] 보상 데이터 없음: {todayId}");
            return;
        }

        // Firebase에 출석 체크 기록
        bool success = await DatabaseManager.Instance.ClaimDailyRewardAsync();

        if (success)
        {
            // 보상 지급
            await GiveRewardAsync(rewardData);

            // UI 업데이트
            slots[now.Day - 1].MarkAsClaimed();

            // Interactive Area 비활성화
            UpdateInteractiveArea();
            rewardClaimedPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[DailyReward] 출석 체크 실패");
        }
    }

    private async UniTask GiveRewardAsync(DailyRewardData rewardData)
    {
        // REWARD_TYPE에 따라 보상 지급
        switch (rewardData.REWARD_TYPE)
        {
            case 1: // 골드
                await DatabaseManager.Instance.AddGoldAsync(rewardData.REWARD_NUM);
                break;

            case 2: // 아이템
                await DatabaseManager.Instance.AddItemAsync(rewardData.REWARD_ID, rewardData.REWARD_NUM);
                break;

            case 3: // 다이아
                await DatabaseManager.Instance.AddDiamondAsync(rewardData.REWARD_NUM);
                break;

            default:
                Debug.LogError($"[DailyReward] 알 수 없는 보상 타입: {rewardData.REWARD_TYPE}");
                break;
        }
    }
}