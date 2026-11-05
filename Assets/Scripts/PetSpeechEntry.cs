using System;

/// <summary>
/// 语音库中的一条台词。字段名与 clips.json 一一对应，JsonUtility 按字段名反序列化，
/// 所以这里改名必须同步改 json。
/// </summary>
[Serializable]
public class PetSpeechEntry
{
    public int id;
    public string label;
    public string file;
    public float duration;

    public override string ToString() => $"#{id} \"{label}\" ({file}, {duration:F2}s)";
}
