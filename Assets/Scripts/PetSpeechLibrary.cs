using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 一个角色的语音库：台词表（json）+ Resources 下的音频。
///
/// 设计要点：每只宠物各挂一个实例，数据互不共享。这里刻意 **不提供全局单例**——
/// 语音库是"有归属"的数据，一旦做成单例，就会诱导调用方去偷用别人的库。
/// 需要哪份数据，就由 PetBase 通过自身层级下的引用拿到。
/// </summary>
public class PetSpeechLibrary : MonoBehaviour
{
    [Header("数据源")]
    [Tooltip("台词表 json：裸数组 [{id,label,file,duration}]，或已包好的 {\"items\":[...]}")]
    [SerializeField] private TextAsset jsonFile;

    [Tooltip("音频目录，相对 Assets/Resources，例如 FenggeClip")]
    [SerializeField] private string audioFolder;

    [Tooltip("文案（label）完全相同的条目只保留第一条。同一句话录了多遍时，勾上可以避免玩家听到“重复的台词”")]
    [SerializeField] private bool dedupeSameLabel = true;

    [Header("调试")]
    [Tooltip("勾选后打印加载汇总（错误和警告不受影响，始终输出）")]
    [SerializeField] private bool verboseLog;

    private readonly Dictionary<int, PetSpeechEntry> _entries = new();
    private readonly Dictionary<int, AudioClip> _clips = new();
    private readonly List<int> _playableIds = new();
    private ShuffleBag<int> _bag;
    private bool _built;

    /// <summary>真正加载到音频、可以播放的条数</summary>
    public int PlayableCount => _playableIds.Count;

    /// <summary>至少有一条能播</summary>
    public bool IsReady => _playableIds.Count > 0;

    private void Awake() => EnsureBuilt();

    /// <summary>取某条台词及其音频；台词存在但音频缺失时 clip 为 null</summary>
    public bool TryGet(int id, out PetSpeechEntry entry, out AudioClip clip)
    {
        EnsureBuilt();
        clip = _clips.TryGetValue(id, out var c) ? c : null;
        return _entries.TryGetValue(id, out entry);
    }

    /// <summary>
    /// 按洗牌袋取下一句：一轮内每条都会轮到一次，播完自动重洗。
    /// 没有可用音频时返回 -1。
    /// </summary>
    public int NextId()
    {
        EnsureBuilt();
        if (_bag == null) return -1;

        int id = _bag.Draw();

        if (verboseLog && _entries.TryGetValue(id, out var entry))
            Debug.Log($"[{name}] 洗牌袋剩余 {_bag.Remaining}/{_playableIds.Count} → id={id} \"{entry.label}\"", this);

        return id;
    }

    // ---------------------------------------------------------------- 加载

    /// <summary>懒加载：即使所在物体一开始是隐藏的，第一次查询时也能拿到数据</summary>
    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        Build();
    }

    private void Build()
    {
        _entries.Clear();
        _clips.Clear();
        _playableIds.Clear();

        if (jsonFile == null)
        {
            Debug.LogError($"[{name}] 未指定 jsonFile", this);
            return;
        }

        if (!TryParseEntries(out var items)) return;

        var seenLabels = new HashSet<string>();
        int dropped = 0;

        foreach (var item in items)
        {
            if (item == null) continue;

            // 同一句文案录了多遍时，只留第一条，避免洗牌袋一轮里出现"同一句话两次"
            if (dedupeSameLabel && !string.IsNullOrWhiteSpace(item.label))
            {
                if (!seenLabels.Add(item.label.Trim()))
                {
                    dropped++;
                    continue;
                }
            }

            _entries[item.id] = item;
        }

        if (dropped > 0)
            Debug.LogWarning($"[{name}] 有 {dropped} 条台词文案重复，已忽略；想保留它们就把 dedupeSameLabel 关掉", this);

        if (_entries.Count == 0)
        {
            Debug.LogError($"[{name}] {jsonFile.name} 里没有有效条目", this);
            return;
        }

        LoadClips();

        // 洗牌袋只依赖"可播放 id 列表"，加载完再建，保证不会抽到缺音频的条目
        _bag = _playableIds.Count > 0 ? new ShuffleBag<int>(_playableIds) : null;

        if (verboseLog)
            Debug.Log($"[{name}] 台词 {_entries.Count} 条 / 可播放 {_clips.Count} 条（Resources/{audioFolder}）", this);

        if (_clips.Count == 0)
            Debug.LogError($"[{name}] 没能加载到任何音频，检查 Assets/Resources/{audioFolder} 是否存在、文件名是否与 json 对得上", this);
        else if (_clips.Count < _entries.Count)
            Debug.LogWarning($"[{name}] 有 {_entries.Count - _clips.Count} 条台词缺音频，对应 id 无法播放", this);
    }

    private bool TryParseEntries(out PetSpeechEntry[] items)
    {
        items = null;
        try
        {
            // JsonUtility 的根类型必须是对象，不能是数组，所以先包一层 {"items":[...]}
            items = JsonUtility.FromJson<JsonWrapper>(WrapArray(jsonFile.text))?.items;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{name}] 解析 {jsonFile.name} 失败：{ex.Message}\n" +
                           "json 应是 [{id,label,file,duration}, ...] 或 {\"items\":[...]}", this);
            return false;
        }

        if (items == null || items.Length == 0)
        {
            Debug.LogError($"[{name}] {jsonFile.name} 里没有解析到任何条目", this);
            return false;
        }
        return true;
    }

    private void LoadClips()
    {
        if (string.IsNullOrEmpty(audioFolder))
        {
            Debug.LogError($"[{name}] 未指定 audioFolder", this);
            return;
        }

        int missing = 0;
        int durationMismatch = 0;

        foreach (var entry in _entries.Values)
        {
            string path = $"{audioFolder}/{Path.GetFileNameWithoutExtension(entry.file)}";
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                missing++;
                if (verboseLog) Debug.LogWarning($"[{name}] 缺少音频 Resources/{path}", this);
                continue;
            }

            _clips[entry.id] = clip;
            _playableIds.Add(entry.id);

            // json 里记了每段的真实时长，可以当数据完整性校验用
            if (entry.duration > 0.05f && Mathf.Abs(clip.length - entry.duration) > 0.1f)
            {
                durationMismatch++;
                if (verboseLog)
                    Debug.LogWarning($"[{name}] id={entry.id} 音频 {clip.length:F2}s ≠ json {entry.duration:F2}s，Resources/{path} 可能不是这一段", this);
            }
        }

        if (missing > 0)
            Debug.LogWarning($"[{name}] Resources/{audioFolder} 下缺 {missing} 条音频", this);
        if (durationMismatch > 0)
            Debug.LogWarning($"[{name}] 有 {durationMismatch} 条音频时长与 json 记录不符，检查是否放错文件", this);
    }

    // ---------------------------------------------------------------- json 结构

    [System.Serializable]
    private class JsonWrapper
    {
        public PetSpeechEntry[] items;
    }

    /// <summary>统一成 {"items":[...]}；文件本身已是对象时原样使用</summary>
    private static string WrapArray(string raw)
    {
        // 去掉 BOM 和首尾空白，编辑器另存为 UTF-8 with BOM 也能读
        string s = raw.Trim().TrimStart('\uFEFF').Trim();
        return s.StartsWith("{") ? s : "{\"items\":" + s + "}";
    }
}
