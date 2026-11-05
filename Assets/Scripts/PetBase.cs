using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 宠物通用行为：解析自身依赖 → 响应点击 → 从自己的语音库里随机挑一句播放，
/// 并让音频、动画、气泡三者以同一时长同步。
///
/// 场景里每只宠物各自持有一份 PetSpeechLibrary / PetAudioPlayer / PetSpeechBubble，
/// 数据互不共享。行为差异交给数据（语音库、动画组件），不靠子类分叉。
/// </summary>
public abstract class PetBase : MonoBehaviour
{
    [Header("依赖（留空则自动在本对象层级下查找）")]
    [SerializeField] private PetSpeechLibrary library;
    [SerializeField] private PetAudioPlayer audioPlayer;
    [SerializeField] private PetSpeechBubble speechBubble;
    [SerializeField] private Button clickButton;

    [Header("调试")]
    [Tooltip("勾选后在 Console 打印每次播放的细节")]
    [SerializeField] private bool verboseLog;

    private IPetAnim _anim;

    /// <summary>音频是否还在播；播放期间的点击会被忽略</summary>
    public bool IsSpeaking { get; private set; }

    protected virtual void Awake()
    {
        ResolveDependencies();
    }

    // ------------------------------------------------------------------ 对外行为

    /// <summary>按洗牌袋说下一句（点击默认走这里，一轮内每条都会轮到）</summary>
    public void SaySomething()
    {
        if (IsSpeaking) return;

        if (library == null)
        {
            Debug.LogError($"[{name}] 没有语音库，无法说话", this);
            return;
        }

        int id = library.NextId();
        if (id < 0)
        {
            Debug.LogWarning($"[{name}] 语音库里没有可播放的音频", this);
            return;
        }

        Speak(id);
    }

    /// <summary>说指定的某一句；播放期间再次调用会被忽略</summary>
    public void Speak(int speechId)
    {
        if (IsSpeaking) return;

        if (library == null || audioPlayer == null || speechBubble == null)
        {
            Debug.LogError($"[{name}] 依赖不完整（library / audioPlayer / speechBubble），无法说话", this);
            return;
        }

        if (!library.TryGet(speechId, out var entry, out var clip) || clip == null)
        {
            Debug.LogWarning($"[{name}] 无法播放 speechId={speechId}", this);
            return;
        }

        IsSpeaking = true;

        float duration = audioPlayer.Play(clip);   // 真实时长，动画和气泡都以它为准
        _anim?.PlayFor(duration);
        speechBubble.Show(entry.label, duration);

        if (verboseLog)
            Debug.Log($"[{name}] 播放 id={speechId} \"{entry.label}\" 音频={clip.name} {duration:F2}s", this);

        StartCoroutine(WaitUntilAudioEnds());
    }

    /// <summary>只播说话动画（空格键那种待机动作也走这里）</summary>
    public void PlayAnim(float duration) => _anim?.PlayFor(duration);

    // ------------------------------------------------------------------ 内部

    private IEnumerator WaitUntilAudioEnds()
    {
        // 用 isPlaying 而不是 WaitForSeconds：音频被打断或暂停时也能正确结束
        yield return new WaitUntil(() => audioPlayer == null || !audioPlayer.IsPlaying);
        IsSpeaking = false;
    }

    /// <summary>
    /// 把依赖统一解析成"自己层级下的那一份"。
    /// 引用一旦被拖成别的宠物的，这里当场报错并纠正，而不是静默串台。
    /// </summary>
    private void ResolveDependencies()
    {
        library = OwnOnly(library, nameof(library));
        speechBubble = OwnOnly(speechBubble, nameof(speechBubble));
        audioPlayer = OwnOrCreate(audioPlayer, nameof(audioPlayer));

        if (clickButton == null) clickButton = FindClickButton();
        if (clickButton != null) clickButton.onClick.AddListener(SaySomething);
        else Debug.LogWarning($"[{name}] 没找到可点击的 Button，点击不会说话", this);

        _anim = FindInterface<IPetAnim>();

        if (library == null) Debug.LogError($"[{name}] 层级下缺少 PetSpeechLibrary", this);
        if (speechBubble == null) Debug.LogError($"[{name}] 层级下缺少 PetSpeechBubble", this);
    }

    /// <summary>只接受自己层级下的组件；指向外部则报错并丢弃</summary>
    private T OwnOnly<T>(T assigned, string fieldName) where T : Component
    {
        var own = GetComponentInChildren<T>(true);
        if (own != null) return own;

        if (assigned != null)
            Debug.LogError($"[{name}] {fieldName} 指向了不属于自己的「{assigned.name}」，已忽略", this);
        return null;
    }

    /// <summary>自己层级下没有就现场创建一份——音频输出不应该三只宠物共用</summary>
    private T OwnOrCreate<T>(T assigned, string fieldName) where T : Component
    {
        var own = GetComponentInChildren<T>(true);
        if (own != null) return own;

        if (assigned != null && verboseLog)
            Debug.Log($"[{name}] {fieldName} 指向的是外部组件「{assigned.name}」，已改为自带一份，避免互相打断", this);
        return gameObject.AddComponent<T>();
    }

    /// <summary>按接口找组件（GetComponentInChildren 不支持接口，只能遍历）</summary>
    private T FindInterface<T>() where T : class
    {
        foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour is T match) return match;
        return null;
    }

    /// <summary>找宠物本体上的按钮：气泡自己的 Button 不算</summary>
    private Button FindClickButton()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (button.GetComponent<PetSpeechBubble>() == null) return button;
        return null;
    }
}
