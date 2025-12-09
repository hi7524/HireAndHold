using System;
using UnityEngine;
using DG.Tweening;

// KHI: 스테이지 내의 플레이어 경험치
public class Experience : MonoBehaviour
{
    [SerializeField] float moveTime = 5f;

    private int expAmount = 10;
    private ExperienceCollector expCollector;
    private ObjectPoolManager poolManager;
    private bool isMoving = false;
    private Vector3 targetPos;

    /// <summary>
    /// 풀에서 가져올 때 호출 (Enemy.ExpItemSpawned에서 사용)
    /// </summary>
    public void Initialize(int amount, ExperienceCollector collector, ObjectPoolManager manager)
    {
        expAmount = amount;
        poolManager = manager;
        isMoving = false;

        // DOTween 정리
        transform.DOKill();

        if (expCollector != null)
            expCollector.OnCollectTriggered -= MoveToTarget;

        expCollector = collector;
        if (expCollector != null)
            expCollector.OnCollectTriggered += MoveToTarget;
    }

    /// <summary>
    /// 기존 호환용 (Test_CreateEXP 등에서 사용)
    /// </summary>
    public void SetExpCollecter(ExperienceCollector expCollector)
    {
        if (this.expCollector != null)
            this.expCollector.OnCollectTriggered -= MoveToTarget;

        this.expCollector = expCollector;
        if (expCollector != null)
            expCollector.OnCollectTriggered += MoveToTarget;
    }

    private void MoveToTarget(Vector3 target)
    {
        if (isMoving)
            return;

        targetPos = target;

        transform.DOLocalMoveY(0.5f, 0.2f).SetRelative(true).SetEase(Ease.OutQuad).SetUpdate(false)
        .OnComplete(() =>
        {
            isMoving = true;
            transform.DOMove(targetPos, moveTime).SetEase(Ease.InOutQuad).SetUpdate(false);
        });
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var collector = collision.GetComponent<ExperienceCollector>();
        if (collector != null)
        {
            collector.CollectExperience(expAmount);
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        // DOTween 정리
        transform.DOKill();

        // 이벤트 해제
        if (expCollector != null)
            expCollector.OnCollectTriggered -= MoveToTarget;

        if (poolManager != null)
            poolManager.Release("Exp", gameObject);
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
