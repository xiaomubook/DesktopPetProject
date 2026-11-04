using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PetSpeechLibrary : MonoBehaviour
{
    public static PetSpeechLibrary Instance { get; private set; }

    [SerializeField] private TextAsset jsonFile;        // 拖入 json
    [SerializeField] private string audioFolder; // Resources 下子目录

    private readonly Dictionary<int, PetSpeechEntry> _entries = new();
    private readonly Dictionary<int, AudioClip> _clips = new();
    private readonly List<int> _playableIds = new();   // 真正有音频、可以随机到的 id

    private void Awake()
    {
        // 只记录一个兜底单例，绝不再销毁其它实例：
        // 三个宠物各自挂一份语音库，一旦销毁，它们的 library 引用会变成“假 null”，
        // 从而全部回退到同一个 Instance，点谁都会播出同一个库的音频。
        if (Instance == null) Instance = this;
        Load();
    }

    private void Load()
    {
        if (jsonFile == null)
        {
            Debug.LogError($"[SpeechLibrary] {name} 的 jsonFile 未赋值，请在 Inspector 里拖入 clips.json", this);
            return;
        }

        // 三个库的名字都叫 Library，日志里带上父物体（宠物名）才分得清
        string owner = transform.parent != null ? transform.parent.name : name;

        // JsonUtility 的根必须是一个对象，不能是数组：套一层 {"items":[...]} 再用包装类解析
        JsonWrapper wrapper;
        try
        {
            wrapper = JsonUtility.FromJson<JsonWrapper>(WrapArray(jsonFile.text));
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpeechLibrary] 解析 {jsonFile.name} 失败：{ex.Message}\n请检查 JSON 是否为数组 [] 或 {{\"items\":[...]}}");
            return;
        }

        var arr = wrapper?.items;
        if (arr == null || arr.Length == 0)
        {
            Debug.LogError($"[SpeechLibrary] {jsonFile.name} 里没有解析到任何条目");
            return;
        }

        foreach (var e in arr)
        {
            if (e == null) continue;
            _entries[e.id] = e;
        }

        int loaded = LoadClips(audioFolder, owner);

        // 兜底：folder 名写错（多写/少写一个 s）时，自动试另一种拼法，并在日志里点名
        if (loaded == 0 && !string.IsNullOrEmpty(audioFolder))
        {
            string alt = audioFolder.EndsWith("s")
                ? audioFolder.Substring(0, audioFolder.Length - 1)
                : audioFolder + "s";
            int altLoaded = LoadClips(alt, owner);
            if (altLoaded > 0)
            {
                Debug.LogWarning($"[Library] {owner} 的 folder=\"{audioFolder}\" 加载不到音频，已自动改用 \"{alt}\"。请把 Inspector 里的值改成 {alt}", this);
                audioFolder = alt;
                loaded = altLoaded;
            }
        }

        // 一眼看出这个库到底加载了什么，跨宠物串音的排查全靠它
        Debug.Log($"[Library] {owner}/{name} | json={jsonFile.name} | folder={audioFolder} | 条目={_entries.Count} | 音频={_clips.Count}", this);
        if (_clips.Count == 0)
            Debug.LogError($"[Library] {owner}/{name} 一个音频都没加载到，检查 Resources/{audioFolder} 是否存在、文件名是否对得上", this);
    }

    private int LoadClips(string folder, string owner)
    {
        _clips.Clear();
        _playableIds.Clear();
        if (string.IsNullOrEmpty(folder)) return 0;

        int missing = 0;
        foreach (var e in _entries.Values)
        {
            // 按文件名去 Resources 加载
            string path = $"{folder}/{Path.GetFileNameWithoutExtension(e.file)}";
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                missing++;
                continue;
            }

            _clips[e.id] = clip;
            _playableIds.Add(e.id);

            // json 里记了每段的真实时长：文件被换成别人家的音频时，时长会对不上
            if (e.duration > 0.05f && Mathf.Abs(clip.length - e.duration) > 0.1f)
                Debug.LogWarning($"[Library] {owner} id={e.id} 音频实际 {clip.length:F2}s，json 记录 {e.duration:F2}s，{path} 很可能不是这一段", this);
        }
        if (missing > 0)
            Debug.LogWarning($"[Library] {owner} 在 Resources/{folder} 下有 {missing} 条音频没找到", this);
        return _clips.Count;
    }

    /// <summary>当前角色可播放的语音条数</summary>
    public int PlayableCount => _playableIds.Count;

    /// <summary>
    /// 随机抽一条可播放的 id；avoid 用于避免连续重复（传上一次播过的 id）。
    /// 没有可用音频时返回 -1。
    /// </summary>
    public int GetRandomId(int avoid = -1)
    {
        int n = _playableIds.Count;
        if (n == 0) return -1;
        if (n == 1) return _playableIds[0];

        int start = Random.Range(0, n);
        if (_playableIds[start] != avoid) return _playableIds[start];

        // 正好抽到上一句：顺移一格，既不会连续重复，分布也仍然均匀
        return _playableIds[(start + 1) % n];
    }

    public bool TryGet(int id, out PetSpeechEntry entry, out AudioClip clip)
    {
        bool ok = _entries.TryGetValue(id, out entry);
        _clips.TryGetValue(id, out clip);
        return ok;
    }

    public PetSpeechEntry GetEntry(int id) =>
        _entries.TryGetValue(id, out var e) ? e : null;

    // JsonUtility 根对象必须是对象类型，用它来承载顶层数组
    [System.Serializable]
    private class JsonWrapper
    {
        public PetSpeechEntry[] items;
    }

    // JsonUtility 顶层数组包裹；若文件本身已是 {"items":[...]} 则原样使用
    private static string WrapArray(string raw)
    {
        // 去掉 BOM 和首尾空白，避免 JsonUtility 解析失败
        string s = raw.Trim().TrimStart('\uFEFF').Trim();
        if (s.StartsWith("{")) return s;
        return "{\"items\":" + s + "}";
    }
}
