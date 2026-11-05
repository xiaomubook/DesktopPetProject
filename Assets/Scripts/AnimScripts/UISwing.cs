using DG.Tweening;
using UnityEngine;

/// <summary>左右摇摆：说话期间来回摆，音频结束时平滑回位。</summary>
public class UISwing : PetAnimBase
{
    [SerializeField] private float maxAngle = 15f;   // 左右各摆多少度

    private Vector3 _originalEuler;
    private bool _captured;

    protected override Tween CreateLoop()
    {
        // PlayFor 进来时已经 StopAnim 归位过，所以这里取到的就是基准角度
        _originalEuler = target.localEulerAngles;
        _captured = true;

        Vector3 left = new Vector3(_originalEuler.x, _originalEuler.y, _originalEuler.z - maxAngle);
        Vector3 right = new Vector3(_originalEuler.x, _originalEuler.y, _originalEuler.z + maxAngle);

        // 循环必须设在 Sequence 上，设在单个 Append 里只会重复那一段
        return DOTween.Sequence()
            .Append(target.DOLocalRotate(left, oneWayDuration).SetEase(Ease.InOutSine))
            .Append(target.DOLocalRotate(right, oneWayDuration * 2f).SetEase(Ease.InOutSine))
            .Append(target.DOLocalRotate(_originalEuler, oneWayDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart);
    }

    protected override Tween CreateRestore()
    {
        if (target == null) return null;
        return target.DOLocalRotate(_originalEuler, restoreDuration).SetEase(Ease.OutQuad);
    }

    protected override void SnapBack()
    {
        if (_captured && target != null) target.localEulerAngles = _originalEuler;
    }
}
