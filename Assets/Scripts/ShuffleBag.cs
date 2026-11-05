using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 洗牌袋：把集合打乱后依次取出，取完自动重洗。
///
/// 和"每次独立随机"的区别：一轮之内每条都出现且只出现一次，
/// 所以不会出现"某句一直抽不到、另一句反复出现"的情况。
/// 跨轮时会避免与上一轮的最后一条连续重复。
///
/// 用法：<c>var bag = new ShuffleBag&lt;int&gt;(ids); bag.Draw();</c>
/// </summary>
public sealed class ShuffleBag<T>
{
    private readonly IReadOnlyList<T> _source;
    private readonly List<T> _bag;

    private T _lastDrawn;
    private bool _hasLastDrawn;

    public ShuffleBag(IReadOnlyList<T> source)
    {
        _source = source ?? throw new System.ArgumentNullException(nameof(source));
        _bag = new List<T>(source.Count);
    }

    /// <summary>这一轮还没抽的张数</summary>
    public int Remaining => _bag.Count;

    /// <summary>抽一张；集合为空时返回 default(T)</summary>
    public T Draw()
    {
        if (_source.Count == 0) return default;

        if (_bag.Count == 0) Refill();

        int index = _bag.Count - 1;
        T value = _bag[index];
        _bag.RemoveAt(index);

        _lastDrawn = value;
        _hasLastDrawn = true;
        return value;
    }

    /// <summary>丢弃当前这一轮，下次抽取重新洗牌</summary>
    public void Reset()
    {
        _bag.Clear();
        _hasLastDrawn = false;
    }

    private void Refill()
    {
        _bag.Clear();
        for (int i = 0; i < _source.Count; i++) _bag.Add(_source[i]);

        // Fisher–Yates 洗牌，保证每种排列等概率
        for (int i = _bag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = _bag[i];
            _bag[i] = _bag[j];
            _bag[j] = temp;
        }

        // 从尾部取牌，所以尾部就是"下一张"。
        // 它若正好等于上一轮的末张，就随机换到别处，避免跨轮连续重复。
        if (_bag.Count > 1 && _hasLastDrawn)
        {
            int tail = _bag.Count - 1;
            if (EqualityComparer<T>.Default.Equals(_bag[tail], _lastDrawn))
            {
                int swap = Random.Range(0, tail);   // [0, tail-1]，不会选回自己
                T temp = _bag[tail];
                _bag[tail] = _bag[swap];
                _bag[swap] = temp;
            }
        }
    }
}
