using DG.Tweening;
using UnityEngine;

/// <summary>
/// 宠物说话动画的统一接口。PetBase 只认识这个接口，不关心底下是脉冲、摇摆还是别的；
/// duration 就是这句语音的真实时长，动画自动跟着它走。
/// </summary>
public interface IPetAnim
{
    void PlayFor(float duration);
    void StopAnim();
}

/// <summary>
/// 说话动画的公共生命周期：进入循环 → 到时收尾 → 还原初始姿态。
/// 子类只需描述"怎么循环""怎么平滑归位""怎么瞬间归位"，
/// tween 的创建、打断、销毁和状态机都由基类统一处理，
/// 保证同一时刻只有一条 tween 在驱动同一个属性。
/// </summary>
public abstract class PetAnimBase : MonoBehaviour, IPetAnim
{
    [Header("动画目标")]
    [SerializeField] protected RectTransform target;

    [Header("节奏")]
    [SerializeField] protected float oneWayDuration = 0.2f;  // 单程时长
    [SerializeField] protected float restoreDuration = 0.1f; // 收尾回位时长

    private Tween _loopTween;     // 循环播放中
    private Tween _restoreTween;  // 收尾回位中
    private Tween _finishTween;   // 定时器：到点结束循环
    private bool _playing;        // 循环是否处于"待收尾"状态

    public void PlayFor(float duration)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{name}] target 未绑定，动画跳过", this);
            return;
        }

        // 先彻底打断上一次：立即归位，避免两条 tween 抢同一个属性
        StopAnim();

        _playing = true;
        _loopTween = CreateLoop();

        // 提前 restoreDuration 收尾，让"归位"正好落在音频结束的时刻
        _finishTween = DOVirtual.DelayedCall(Mathf.Max(0f, duration - restoreDuration), FinishLoop);
    }

    public void StopAnim()
    {
        bool needSnap = _playing || _restoreTween != null;

        _finishTween?.Kill();
        _finishTween = null;

        _loopTween?.Kill();
        _loopTween = null;

        _restoreTween?.Kill();
        _restoreTween = null;

        _playing = false;

        if (needSnap) SnapBack();
    }

    /// <summary>循环动画</summary>
    protected abstract Tween CreateLoop();

    /// <summary>收尾时的平滑归位动画</summary>
    protected abstract Tween CreateRestore();

    /// <summary>瞬间归位到初始姿态</summary>
    protected abstract void SnapBack();

    private void FinishLoop()
    {
        _finishTween = null;

        _loopTween?.Kill();
        _loopTween = null;

        if (!_playing) return;
        _playing = false;

        _restoreTween = CreateRestore();
        if (_restoreTween == null)
        {
            SnapBack();
            return;
        }

        _restoreTween.OnComplete(() =>
        {
            _restoreTween = null;
            SnapBack();   // 兜底：tween 有误差也保证回到精确初值
        });
    }

    protected virtual void OnDestroy() => StopAnim();
}
