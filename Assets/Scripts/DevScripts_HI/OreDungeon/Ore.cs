using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AssetKits.ParticleImage;
using DG.Tweening;

public class Ore : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private Image oreImage;
    [SerializeField] private float particleArrivalTime = 0.5f; // 파티클이 목표에 도착하는 시간 (초)

    private OreData oreData;
    private int touchCount;
    private int oresID;
    private OreDungeonManager manager;
    private Canvas canvas;
    private Camera worldCamera;
    private ObjectPoolManager poolManager;
    private Transform tailEffectTarget;
    private bool isDestroyed = false;

    // 광석 비활성화 이벤트
    public event Action<Ore> OnOreDeactivated;

    public void SetOreType(int id, DataTable_Ore oreTable, OreDungeonManager dungeonManager, Canvas canvasRef, Camera camera, ObjectPoolManager poolMgr, Transform tailTarget)
    {
        oresID = id;
        oreData = oreTable.Get(id);
        manager = dungeonManager;
        canvas = canvasRef;
        worldCamera = camera;
        poolManager = poolMgr;
        tailEffectTarget = tailTarget;

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
        // 사운드 재생
        if (SoundManager.Instance != null && AddressablePreloader.Instance != null)
        {
            AudioClip mineSound = AddressablePreloader.Instance.GetCachedAudioClip("MineSound");
            if (mineSound != null)
            {
                SoundManager.Instance.PlaySFX(mineSound);
            }
        }

        // 터치 위치에서 이펙트 재생
        PlayEffectAtTouchPosition(eventData.position);

        // 기존 로직
        OnClick();
    }

    public void OnClick()
    {
        touchCount--;
        manager?.OnOreTouched();

        // 광석이 파괴될 예정이라면 애니메이션 후 비활성화
        if (touchCount <= 0)
        {
            isDestroyed = true;

            // Punch 애니메이션 후 비활성화
            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    // 비활성화 이벤트 발생
                    OnOreDeactivated?.Invoke(this);
                });
        }
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

        // Ore의 현재 World Position을 그대로 사용
        Vector3 oreWorldPos = transform.position;

        // Screen 좌표에서 Ray를 쏴서 Canvas Plane과의 교차점 찾기
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        Plane canvasPlane = new Plane(-worldCamera.transform.forward, oreWorldPos);

        Vector3 worldPosition = oreWorldPos; // 기본값

        if (canvasPlane.Raycast(ray, out float distance))
        {
            worldPosition = ray.GetPoint(distance);
        }

        // 첫 번째 이펙트 재생 (MineEffect)
        GameObject effect = poolManager.Get("MineEffect");

        if (effect != null)
        {
            effect.transform.position = worldPosition;
        }
        else
        {
            Debug.LogError("MineEffect를 풀에서 가져오지 못했습니다. 풀이 제대로 설정되었는지 확인하세요.");
        }

        // 두 번째 이펙트 재생 (ParticleImage) - Canvas 위치에 그대로 생성
        if (canvas != null)
        {
            GameObject particleEffect = poolManager.Get("TailEffect");

            if (particleEffect != null)
            {
                particleEffect.transform.SetParent(canvas.transform, false);
                // Ore의 위치에서 시작
                particleEffect.transform.position = transform.position;

                // ParticleImage의 Attractor 설정
                if (tailEffectTarget != null)
                {
                    ParticleImage particleImage = particleEffect.GetComponent<ParticleImage>();
                    if (particleImage != null)
                    {
                        particleImage.attractorTarget = tailEffectTarget;

                        // 파티클 시작 알림
                        manager?.OnParticleStarted();

                        // 설정된 시간 후 콜백 실행 (파티클이 목표에 도착하는 시간)
                        DOVirtual.DelayedCall(particleArrivalTime, () =>
                        {
                            OnParticleReachedTarget();
                            // 파티클 완료 알림
                            manager?.OnParticleCompleted();
                        });
                    }
                }
            }
            else
            {
                Debug.LogError("ParticleImage를 풀에서 가져오지 못했습니다. 풀이 제대로 설정되었는지 확인하세요.");
            }
        }
    }

    private void OnParticleReachedTarget()
    {
        // 아이콘 펀치 애니메이션
        if (tailEffectTarget != null)
        {
            tailEffectTarget.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
        }

        // 숫자 증가
        manager.AddOreCount(1);

        // 광석이 파괴되었다면 남은 광석 수 감소 처리
        if (isDestroyed)
        {
            CalculateDropResult();
            manager?.OnOreDestroyed();
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
        int randomValue = UnityEngine.Random.Range(0, 100);
        int jackpotPercent = oreData.Jackpot_Percent;
        int fiascoPercent = oreData.Fiasco;

        if (randomValue < jackpotPercent)
        {
            // 대성공
            int dropCount = UnityEngine.Random.Range(oreData.Jackpot_Percent_Minimum_Number_Of_Drops,
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
