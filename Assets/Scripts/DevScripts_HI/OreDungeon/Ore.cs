using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Ore : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image oreImage;

    private OreData oreData;
    private int touchCount;
    private int oresID;
    private OreDungeonManager manager;
    private Canvas canvas;
    private Camera worldCamera;
    private ObjectPoolManager poolManager;

    public void SetOreType(int id, DataTable_Ore oreTable, OreDungeonManager dungeonManager, Canvas canvasRef, Camera camera, ObjectPoolManager poolMgr)
    {
        oresID = id;
        oreData = oreTable.Get(id);
        manager = dungeonManager;
        canvas = canvasRef;
        worldCamera = camera;
        poolManager = poolMgr;

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

    public void OnPointerDown(PointerEventData eventData)
    {
        // 터치 위치에서 이펙트 재생
        PlayEffectAtTouchPosition(eventData.position);

        // 기존 로직
        OnClick();
    }

    public void OnClick()
    {
        touchCount--;
        manager.AddOreCount(1);
        manager?.OnOreTouched();
        CheckDestroy();
    }

    private void PlayEffectAtTouchPosition(Vector2 screenPosition)
    {
        if (poolManager == null)
        {
            Debug.LogError("poolManager가 null입니다!");
            return;
        }

        if (worldCamera == null)
        {
            Debug.LogError("worldCamera가 null입니다!");
            return;
        }

        // 방법 1: Ore의 현재 World Position을 그대로 사용
        Vector3 oreWorldPos = transform.position;

        // 방법 2: Screen 좌표에서 Ray를 쏴서 Canvas Plane과의 교차점 찾기
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane canvasPlane = new Plane(-worldCamera.transform.forward, oreWorldPos);

        Vector3 worldPosition = oreWorldPos; // 기본값

        if (canvasPlane.Raycast(ray, out float distance))
        {
            worldPosition = ray.GetPoint(distance);
        }

        // 이펙트 재생
        GameObject effect = poolManager.Get("MineEffect");

        if (effect != null)
        {
            effect.transform.position = worldPosition;
        }
        else
        {
            Debug.LogError("MineEffect를 풀에서 가져오지 못했습니다. 풀이 제대로 설정되었는지 확인하세요.");
        }
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