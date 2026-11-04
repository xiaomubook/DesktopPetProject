using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetSpeechBubble : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Button label;
    [SerializeField] private RectTransform bubbleRoot;

    private Coroutine _routine;

    /// <summary>显示文字并做弹性动画，duration 由音频时长决定</summary>
    public void Show(string text, float duration, float strength, float fadeOut)
    {
        label.GetComponentInChildren<TMP_Text>().text = text;
        gameObject.SetActive(true);
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine(duration, strength, fadeOut));
    }

    public void HideImmediate()
    {
        if (_routine != null) StopCoroutine(_routine);
        group.alpha = 0f;
        gameObject.SetActive(false);
    }

    private IEnumerator PlayRoutine(float duration, float strength, float fadeOut)
    {
        group.alpha = 1f;

        // 弹性出现（0.25s 内从 0.6 弹到 1.0，带 overshoot）
        float t = 0f, appear = 0.25f;
        while (t < appear)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / appear);
            float scale = 1f + Mathf.Sin(k * Mathf.PI) * (strength - 1f);
            bubbleRoot.localScale = Vector3.one * scale;
            yield return null;
        }
        bubbleRoot.localScale = Vector3.one;

        // 保持（吃掉剩余时长，动画总长 = 音频时长）
        float hold = Mathf.Max(0f, duration - appear - fadeOut);
        if (hold > 0f) yield return new WaitForSeconds(hold);

        // 淡出
        float ft = 0f;
        while (ft < fadeOut)
        {
            ft += Time.deltaTime;
            group.alpha = 1f - Mathf.Clamp01(ft / fadeOut);
            yield return null;
        }

        group.alpha = 0f;
        gameObject.SetActive(false);
    }
}
