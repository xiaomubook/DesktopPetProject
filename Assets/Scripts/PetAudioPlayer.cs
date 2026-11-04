using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource source;

    /// <summary>播放并返回真实时长；播完可轮询 isPlaying</summary>
    public float Play(AudioClip clip)
    {
        if (clip == null) return 0f;
        source.clip = clip;
        source.Play();
        return clip.length;
    }

    public bool IsPlaying => source != null && source.isPlaying;
    public void Stop() => source?.Stop();
}
