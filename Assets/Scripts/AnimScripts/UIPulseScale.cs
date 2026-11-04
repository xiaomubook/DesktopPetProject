using UnityEngine;
using DG.Tweening;

public class UIPulseScale : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float minScaleY = 0.5f;      // Y轴最小缩放比例
    [SerializeField] private float oneWayDuration = 0.3f;  // 单程时间

    private Tween pulseTween;
    private Tween delayTween;
    private Vector3 originalScale;
    private bool hasPlayed;

    /// <summary>播放脉冲缩放，totalDuration 秒后停止并恢复原大小</summary>
    public void PlayPulse(float totalDuration)
    {
        StopPulse();

        originalScale = target.localScale;
        hasPlayed = true;
        float minY = originalScale.y * minScaleY;

        pulseTween = target
            .DOScaleY(minY, oneWayDuration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);

        delayTween = DOVirtual.DelayedCall(totalDuration, () =>
        {
            pulseTween?.Kill();
            pulseTween = null;
            target.DOScaleY(originalScale.y, 0.1f)
                  .SetEase(Ease.OutQuad)
                  .OnComplete(() => target.localScale = originalScale);
        });
    }

    public void StopPulse()
    {
        pulseTween?.Kill();
        delayTween?.Kill();
        pulseTween = null;
        delayTween = null;
        if (hasPlayed) target.localScale = originalScale;
    }

    private void OnDestroy() => StopPulse();
    
}
