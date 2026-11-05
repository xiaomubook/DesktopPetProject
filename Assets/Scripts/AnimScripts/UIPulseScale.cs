using DG.Tweening;
using UnityEngine;

/// <summary>脉冲缩放：说话期间 Y 轴反复压扁，音频结束时平滑回位。</summary>
public class UIPulseScale : PetAnimBase
{
    [SerializeField] private float minScaleY = 0.5f;   // Y 轴最小缩放比例

    private Vector3 _originalScale;
    private bool _captured;

    protected override Tween CreateLoop()
    {
        // PlayFor 进来时已经 StopAnim 归位过，所以这里取到的就是基准缩放
        _originalScale = target.localScale;
        _captured = true;

        return target
            .DOScaleY(_originalScale.y * minScaleY, oneWayDuration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);
    }

    protected override Tween CreateRestore()
    {
        if (target == null) return null;
        return target.DOScaleY(_originalScale.y, restoreDuration).SetEase(Ease.OutQuad);
    }

    protected override void SnapBack()
    {
        if (_captured && target != null) target.localScale = _originalScale;
    }
}
