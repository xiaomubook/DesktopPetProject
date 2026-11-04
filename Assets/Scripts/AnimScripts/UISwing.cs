using UnityEngine;
using DG.Tweening;

public class UISwing : MonoBehaviour, IPetAnim
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float maxAngle = 15f;         // 左右各摆角度
    [SerializeField] private float oneWayDuration = 0.3f;  // 单程时间

    private Tween swingTween;
    private Tween delayTween;
    private Vector3 originalEuler;
    private bool hasPlayed;

    /// <summary>说话时按音频时长播放（PetBase 统一调用）</summary>
    public void PlayFor(float duration) => PlaySwing(duration);

    public void PlaySwing(float totalDuration)
    {
        StopSwing();

        originalEuler = target.localEulerAngles;
        hasPlayed = true;

        Vector3 left = new Vector3(originalEuler.x, originalEuler.y, originalEuler.z - maxAngle);
        Vector3 right = new Vector3(originalEuler.x, originalEuler.y, originalEuler.z + maxAngle);

        // 关键：循环必须设置在 Sequence 上
        swingTween = DOTween.Sequence()
            .Append(target.DOLocalRotate(left, oneWayDuration).SetEase(Ease.InOutSine))
            .Append(target.DOLocalRotate(right, oneWayDuration * 2f).SetEase(Ease.InOutSine))
            .Append(target.DOLocalRotate(originalEuler, oneWayDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart);

        delayTween = DOVirtual.DelayedCall(totalDuration, () =>
        {
            swingTween?.Kill();
            swingTween = null;
            target.DOLocalRotate(originalEuler, 0.1f)
                  .SetEase(Ease.OutQuad)
                  .OnComplete(() => target.localEulerAngles = originalEuler);
        });
    }

    public void StopSwing()
    {
        swingTween?.Kill();
        delayTween?.Kill();
        swingTween = null;
        delayTween = null;
        if (hasPlayed) target.localEulerAngles = originalEuler;
    }

    private void OnDestroy() => StopSwing();
    
}
