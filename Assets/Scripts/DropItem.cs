using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 몬스터가 드롭하는 아이템 오브젝트 (Experience 패턴 참고)
/// </summary>
public class DropItem : MonoBehaviour
{
    [SerializeField] private float moveTime = 0.5f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int itemId;
    private int itemCount;
    private ItemData itemData;
    private ObjectPoolManager poolManager;
    private ExperienceCollector expCollector;
    private bool isCollected = false;
    private bool isMoving = false;
    private Vector3 targetPos;

    public int ItemId => itemId;
    public int ItemCount => itemCount;
    public ItemData ItemData => itemData;

    public static event Action<DropItem> OnItemCollected;

    /// <summary>
    /// 풀에서 가져올 때 호출 (Enemy.TryDropItems에서 사용)
    /// </summary>
    public void Initialize(int itemId, int count, ObjectPoolManager manager, ExperienceCollector collector)
    {
        this.itemId = itemId;
        this.itemCount = count;
        this.poolManager = manager;
        this.isCollected = false;
        this.isMoving = false;

        // DOTween 정리
        transform.DOKill();

        // ExperienceCollector 이벤트 구독 (Experience와 동일한 패턴)
        if (expCollector != null)
            expCollector.OnCollectTriggered -= MoveToTarget;

        expCollector = collector;
        if (expCollector != null)
            expCollector.OnCollectTriggered += MoveToTarget;

        // ItemTable에서 아이템 데이터 조회
        itemData = DataTableManager.ItemTable.Get(itemId);

        if (itemData != null)
        {
            // 아이템 아이콘 설정 (Addressable 또는 Resources에서 로드)
            LoadItemIcon(itemData.ITEM_ICON);
        }

        // 드롭 애니메이션 재생
        PlayDropAnimation();
    }

    private void LoadItemIcon(string iconKey)
    {
        // spriteRenderer가 할당되지 않았으면 자동으로 가져오기
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || string.IsNullOrEmpty(iconKey))
            return;

        // AddressablePreloader 캐시에서 스프라이트 로드 시도
        // 없으면 기본 스프라이트 유지
        if (AddressablePreloader.Instance != null)
        {
            var sprite = AddressablePreloader.Instance.GetCachedSprite(iconKey);
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }
    }

    private void PlayDropAnimation()
    {
        // 위로 튀어오르고 떨어지는 애니메이션
        transform.DOLocalMoveY(0.5f, 0.2f)
            .SetRelative(true)
            .SetEase(Ease.OutQuad)
            .SetUpdate(false)
            .OnComplete(() =>
            {
                transform.DOLocalMoveY(-0.5f, 0.3f)
                    .SetRelative(true)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(false);
            });
    }

    private void MoveToTarget(Vector3 target)
    {
        if (isMoving || isCollected)
            return;

        targetPos = target;

        transform.DOLocalMoveY(0.5f, 0.2f)
            .SetRelative(true)
            .SetEase(Ease.OutQuad)
            .SetUpdate(false)
            .OnComplete(() =>
            {
                isMoving = true;
                transform.DOMove(targetPos, moveTime)
                    .SetEase(Ease.InOutQuad)
                    .SetUpdate(false);
            });
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Wall 또는 Player 태그와 충돌 시 아이템 획득
        if (isCollected)
            return;

        if (collision.CompareTag("Wall") || collision.CompareTag("Player"))
        {
            CollectItem();
        }
    }

    private void CollectItem()
    {
        if (isCollected)
            return;

        isCollected = true;

        // 아이템 획득 이벤트 발생 (인벤토리 시스템에서 구독)
        OnItemCollected?.Invoke(this);

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // DOTween 정리
        transform.DOKill();

        // 이벤트 해제
        if (expCollector != null)
            expCollector.OnCollectTriggered -= MoveToTarget;

        if (poolManager != null)
            poolManager.Release("DropItem", gameObject);
        else
            Destroy(gameObject);
    }

    private void OnDisable()
    {
        transform.DOKill();
        if (expCollector != null)
            expCollector.OnCollectTriggered -= MoveToTarget;
    }
}
