using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 说话气泡：弹出 → 按音频时长保持 → 淡出。
/// 动画参数在这里维护，调用方只需要给"文字 + 时长"。
/// </summary>
public class PetSpeechBubble : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private CanvasGroup group;

    // 气泡本体上挂的 Button，用它取到下面的 TMP 文本。
    // （历史上这个字段就是 Button，保留类型是为了不破坏场景里已有的引用）
    [SerializeField] private Button label;

    [SerializeField] private RectTransform bubbleRoot;

    [Header("动画")]
    [SerializeField] private float appearDuration = 0.25f;
    [SerializeField] private float popStrength = 1.2f;   // 弹出时的最大缩放倍数
    [SerializeField] private float maxFadeOut = 0.2f;    // 淡出上限，短音频会自动缩短

    private TMP_Text _labelText;
    private Coroutine _routine;

    /// <summary>懒解析：气泡初始是隐藏状态时 Awake 不会执行，这里保证第一次用时也能拿到</summary>
    private TMP_Text LabelText
    {
        get
        {
            if (_labelText == null && label != null)
                _labelText = label.GetComponentInChildren<TMP_Text>(true);
            return _labelText;
        }
    }

    private void Awake()
    {
        if (LabelText == null)
            Debug.LogError($"[{name}] 没有找到 TMP_Text，气泡将无法显示文字", this);
    }

    /// <summary>显示一句话；duration 一般传这句音频的真实时长。</summary>
    public void Show(string text, float duration)
    {
        var textComponent = LabelText;
        if (textComponent != null) textComponent.text = text;

        gameObject.SetActive(true);
        if (group != null) group.alpha = 1f;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine(Mathf.Max(0f, duration)));
    }

    /// <summary>立刻收起，不播淡出</summary>
    public void HideImmediate()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (group != null) group.alpha = 0f;
        gameObject.SetActive(false);
    }

    private IEnumerator PlayRoutine(float duration)
    {
        float fadeOut = Mathf.Min(maxFadeOut, duration * 0.25f);   // 短音频保护

        // 弹出：进度 0→1 时叠加一次 sin 过冲
        if (bubbleRoot != null)
        {
            float t = 0f;
            while (t < appearDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / appearDuration);
                float scale = 1f + Mathf.Sin(k * Mathf.PI) * (popStrength - 1f);
                bubbleRoot.localScale = Vector3.one * scale;
                yield return null;
            }
            bubbleRoot.localScale = Vector3.one;
        }

        // 保持：吃掉剩余时长，让整段动画正好等于音频时长
        float hold = duration - appearDuration - fadeOut;
        if (hold > 0f) yield return new WaitForSeconds(hold);

        if (fadeOut > 0f && group != null)
        {
            float ft = 0f;
            while (ft < fadeOut)
            {
                ft += Time.deltaTime;
                group.alpha = 1f - Mathf.Clamp01(ft / fadeOut);
                yield return null;
            }
            group.alpha = 0f;
        }

        gameObject.SetActive(false);
        _routine = null;
    }
}
