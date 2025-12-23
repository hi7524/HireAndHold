using UnityEngine;
using UnityEngine.UI;

public class Ore : MonoBehaviour
{
    [SerializeField] private Image oreImage;

    private OreData oreData;
    private int touchCount;
    private int oresID;
    private OreDungeonManager manager;

    public void SetOreType(int id, DataTable_Ore oreTable, OreDungeonManager dungeonManager)
    {
        oresID = id;
        oreData = oreTable.Get(id);
        manager = dungeonManager;

        if (oreData == null)
        {
            Debug.LogError($"광석 데이터를 찾을 수 없습니다. ID: {id}");
            return;
        }

        touchCount = oreData.Ores_HP;
    }

    public void SetTouchCount(int count = 1)
    {
        touchCount = count;
    }

    public void OnClick()
    {
        touchCount--;
        manager.AddOreCount(1);
        manager?.OnOreTouched();
        CheckDestroy();
    }

    private void CheckDestroy()
    {
        if (touchCount <= 0)
        {
            CalculateDropResult();
            manager?.OnOreDestroyed();
            gameObject.SetActive(false);
        }
    }

    private void CalculateDropResult()
    {
        int randomValue = Random.Range(0, 100);
        int jackpotPercent = oreData.Jackpot_Percent;
        int fiascoPercent = oreData.Fiasco;

        if (randomValue < jackpotPercent)
        {
            // 대성공
            int dropCount = Random.Range(oreData.Jackpot_Percent_Minimum_Number_Of_Drops,
                                          oreData.Jackpot_Percent_Maximum_Number_Of_Drops + 1);
        }
        else if (randomValue < jackpotPercent + fiascoPercent)
        {
            // 대실패
            int dropCount = oreData.Fiasco_Drops;
        }
        else
        {
            // 일반 성공
            int dropCount = oreData.Base_Number_Of_Successful_Drops;
        }
    }
}