using UnityEngine;

/// <summary>
/// 一只宠物的音频输出。挂在宠物根节点上，自带一个 AudioSource，
/// 所以三只宠物可以同时各说各的，不会互相打断。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PetAudioPlayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource _source;

    private AudioSource Source
    {
        get
        {
            if (_source == null) _source = GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();

            _source.playOnAwake = false;
            return _source;
        }
    }

    public bool IsPlaying => Source.isPlaying;

    /// <summary>播放并返回真实时长（秒），供动画/气泡对齐使用</summary>
    public float Play(AudioClip clip)
    {
        if (clip == null) return 0f;

        AudioSource source = Source;
        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.Play();
        return clip.length;
    }

    public void Stop()
    {
        if (_source != null) _source.Stop();
    }

    private void OnDisable() => Stop();
}
