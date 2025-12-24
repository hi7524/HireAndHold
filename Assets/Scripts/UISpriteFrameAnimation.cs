using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class UISpriteFrameAnimation : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameInterval = 0.05f;
    [SerializeField] private bool playOnEnable = true;

    public async UniTask PlayOnceAsync(CancellationToken ct)
    {
        if (targetImage == null || frames == null || frames.Length == 0)
            return;

        for (int i = 0; i < frames.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            targetImage.sprite = frames[i];
            await UniTask.Delay(
                (int)(frameInterval * 1000),
                DelayType.UnscaledDeltaTime,
                PlayerLoopTiming.Update,
                ct
            );
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            PlayOnceAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    public void ShowLastFrame()
    {
        if (targetImage != null && frames != null && frames.Length > 0)
        {
            targetImage.sprite = frames[frames.Length - 1];
        }
    }

}
