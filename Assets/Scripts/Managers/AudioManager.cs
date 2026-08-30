using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频管理器：负责播放游戏音效
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    private AudioSource audioSource;
    protected override void Awake()
    {
        base.Awake();
        // 动态挂一个AudioSource（不需要在编辑器里手动加）
        audioSource =gameObject.AddComponent<AudioSource>();
    }
    public void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager]播放音效失效：clip为空");
            return;
        }
        audioSource.PlayOneShot(clip);
    }
}
