using System;
using UnityEngine;
using DG.Tweening;

// KHI: 스테이지 내의 플레이어 경험치
public class Experience : MonoBehaviour
{
    [SerializeField] float moveTime = 5f;
    [SerializeField] private int expAmount = 5;

    private ExperienceCollector expCollector;
    private bool isMoving = false;
    private Vector3 targetPos;

    public void SetExpCollecter(ExperienceCollector expCollector)
    {
        this.expCollector = expCollector;
        expCollector.OnCollectTriggered += MoveToTarget;
    }

    private void MoveToTarget(Vector3 target)
    {
        if (isMoving)
            return;

        targetPos = target;

        transform.DOLocalMoveY(0.5f, 0.2f).SetRelative(true).SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            isMoving = true;
            transform.DOMove(targetPos, moveTime).SetEase(Ease.InOutQuad);
        });
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var collector = collision.GetComponent<ExperienceCollector>();
        if (collector != null)
        {
            collector.CollectExperience(expAmount);
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (expCollector != null)
            expCollector.OnCollectTriggered -= MoveToTarget;
    }
}
