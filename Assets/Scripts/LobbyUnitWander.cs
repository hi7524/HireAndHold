using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class LobbyUnitWander : MonoBehaviour
{
    [Header("Move Area (Local)")]
    public Vector2 min = new Vector2(-1.5f, -1.5f);
    public Vector2 max = new Vector2(1.5f, 1.5f);

    [Header("Move Settings")]
    public float moveSpeed = 0.5f;
    public float idleTimeMin = 1.5f;
    public float idleTimeMax = 3.5f;

    private Vector3 startLocalPos;
    private Animator animator;
    private bool running;

    private CancellationTokenSource cts;

    private void Awake()
    {
        startLocalPos = transform.localPosition;
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        cts = new CancellationTokenSource();
        WanderLoop(cts.Token).Forget();
    }

    private void OnDisable()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }


    private async UniTaskVoid WanderLoop(CancellationToken token)
    {
        await UniTask.Delay(Random.Range(300, 800), cancellationToken: token);

        while (!token.IsCancellationRequested)
        {
            SetWalk(false);

            await UniTask.Delay(
                Mathf.RoundToInt(Random.Range(idleTimeMin, idleTimeMax) * 1000),
                cancellationToken: token
            );

            Vector3 target = startLocalPos + new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                0f
            );

            SetWalk(true);
            await MoveTo(target, token);
        }
    }


    private async UniTask MoveTo(Vector3 target, CancellationToken token)
    {
        while (!token.IsCancellationRequested &&
               Vector3.Distance(transform.localPosition, target) > 0.02f)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                target,
                moveSpeed * Time.deltaTime
            );

            FlipByDirection(target);
            await UniTask.Yield(token);
        }

        SetWalk(false);
    }


    private void FlipByDirection(Vector3 target)
    {
        float dir = target.x - transform.localPosition.x;
        if (Mathf.Abs(dir) < 0.01f) return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir > 0 ? 1 : -1);
        transform.localScale = scale;
    }

    private void SetWalk(bool walking)
    {
        if (animator == null) return;

        //animator.SetFloat("Speed", walking ? 1f : 0f);
    }
}
