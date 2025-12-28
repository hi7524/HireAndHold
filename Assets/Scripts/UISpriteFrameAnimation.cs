using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class UISpriteFrameAnimation : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameInterval = 0.05f;
    [SerializeField] private bool playOnEnable = true;

    private CancellationTokenSource playCts;

    public async UniTask PlayOnceAsync(CancellationToken ct)
    {
        if (targetImage == null || frames == null || frames.Length == 0)
            return;

        // 기존 재생 중인 애니메이션 취소
        playCts?.Cancel();
        playCts?.Dispose();
        playCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        try
        {
            for (int i = 0; i < frames.Length; i++)
            {
                playCts.Token.ThrowIfCancellationRequested();
                targetImage.sprite = frames[i];
                await UniTask.Delay(
                    (int)(frameInterval * 1000),
                    DelayType.UnscaledDeltaTime,
                    PlayerLoopTiming.Update,
                    playCts.Token
                );
            }
        }
        catch (OperationCanceledException)
        {
            // 정상적인 취소
        }
        finally
        {
            playCts?.Dispose();
            playCts = null;
        }
    }

    /// <summary>
    /// 애니메이션을 멈추고 첫 번째 프레임을 표시
    /// </summary>
    public void Stop()
    {
        // 재생 중인 애니메이션 취소
        playCts?.Cancel();
        playCts?.Dispose();
        playCts = null;

        // 첫 프레임 표시
        ShowFirstFrame();
    }

    /// <summary>
    /// 첫 번째 프레임을 표시
    /// </summary>
    public void ShowFirstFrame()
    {
        if (targetImage != null && frames != null && frames.Length > 0)
        {
            targetImage.sprite = frames[0];
        }
    }

    /// <summary>
    /// 마지막 프레임을 표시
    /// </summary>
    public void ShowLastFrame()
    {
        if (targetImage != null && frames != null && frames.Length > 0)
        {
            targetImage.sprite = frames[frames.Length - 1];
        }
    }

    /// <summary>
    /// 특정 프레임을 표시
    /// </summary>
    public void ShowFrame(int frameIndex)
    {
        if (targetImage != null && frames != null && frameIndex >= 0 && frameIndex < frames.Length)
        {
            targetImage.sprite = frames[frameIndex];
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            PlayOnceAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    private void OnDisable()
    {
        // 컴포넌트 비활성화 시 애니메이션 정지
        playCts?.Cancel();
        playCts?.Dispose();
        playCts = null;
    }

    private void OnDestroy()
    {
        playCts?.Cancel();
        playCts?.Dispose();
        playCts = null;
    }
}
