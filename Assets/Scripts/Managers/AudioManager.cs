using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频资源路径常量。
/// 【规范】路径字符串集中在一处，避免 "Audio/xxx" 散落在各个脚本里拼错。
/// </summary>
public static class GameAudio
{
    public const string BgmMain = "Audio/BGM_Main";
    public const string Deal = "Audio/SFX_Deal";
    public const string Select = "Audio/SFX_Select";
    public const string PlayCard = "Audio/SFX_PlayCard";
    public const string Bomb = "Audio/SFX_Bomb";
    public const string Win = "Audio/SFX_Win";
    public const string Lose = "Audio/SFX_Lose";
    public const string ButtonClick = "Audio/SFX_Click";
}

/// <summary>
/// 音频管理器：背景音乐 + 音效播放、音量控制与持久化。
/// 通过监听游戏事件自动播放，其它模块不需要主动调用。
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    /// <summary>背景音乐播放器（循环）</summary>
    private AudioSource bgmSource;

    /// <summary>音效播放器（PlayOneShot，可叠加播放）</summary>
    private AudioSource sfxSource;

    /// <summary>音频缓存，避免同一个 clip 反复从磁盘读</summary>
    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    /// <summary>已经警告过的缺失路径，防止每帧刷屏</summary>
    private readonly HashSet<string> missingWarned = new HashSet<string>();

    public float MusicVolume { get; private set; } = 1f;
    public float SoundVolume { get; private set; } = 1f;

    protected override void Awake()
    {
        base.Awake();

        // 动态挂两个 AudioSource，不用在编辑器里手动拖
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        // 从存档恢复音量
        MusicVolume = SaveManager.Instance.Data.musicVolume;
        SoundVolume = SaveManager.Instance.Data.soundVolume;
        ApplyVolume();

        // 【补】原来 PlayBgm 写好了却没人调用，BGM 资源放进去也不会响。
        // 这里是唯一的播放入口；资源缺失时 LoadClip 会静默跳过，不影响游戏。
        PlayBgm(GameAudio.BgmMain);

        RegisterEvents();
    }

    private void OnDestroy()
    {
        UnRegisterEvents();
    }

    #region 事件绑定

    private void RegisterEvents()
    {
        EventCenter.Instance.Register(GameEvent.Game_DealCardFinish, OnDealFinish);
        EventCenter.Instance.Register(GameEvent.Card_Select, OnCardSelect);
        EventCenter.Instance.Register(GameEvent.Game_PlayCard, OnPlayCard);
        EventCenter.Instance.Register(GameEvent.Game_ResultReady, OnResultReady);
    }

    private void UnRegisterEvents()
    {
        EventCenter.Instance.UnRegister(GameEvent.Game_DealCardFinish, OnDealFinish);
        EventCenter.Instance.UnRegister(GameEvent.Card_Select, OnCardSelect);
        EventCenter.Instance.UnRegister(GameEvent.Game_PlayCard, OnPlayCard);
        EventCenter.Instance.UnRegister(GameEvent.Game_ResultReady, OnResultReady);
    }

    private void OnDealFinish(object param) => PlaySfx(GameAudio.Deal);

    private void OnCardSelect(object param) => PlaySfx(GameAudio.Select);

    private void OnPlayCard(object param)
    {
        // 炸弹/火箭用专属音效，其它牌用普通出牌音
        List<CardData> cards = param as List<CardData>;
        if (cards != null)
        {
            CardType type = CardRules.CheckCardType(cards);
            if (type == CardType.Bomb || type == CardType.Rocket)
            {
                PlaySfx(GameAudio.Bomb);
                return;
            }
        }
        PlaySfx(GameAudio.PlayCard);
    }

    private void OnResultReady(object param)
    {
        ResultData data = param as ResultData;
        if (data == null) return;
        // 下标 0 是玩家自己的得分，正数说明玩家所在阵营赢了
        PlaySfx(data.roundScores[0] > 0 ? GameAudio.Win : GameAudio.Lose);
    }

    #endregion

    #region 播放接口

    /// <summary>播放音效（可叠加，适合短促的点击音）</summary>
    public void PlaySfx(string path)
    {
        AudioClip clip = LoadClip(path);
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, SoundVolume);
    }

    /// <summary>播放背景音乐（同一首正在播则不重复触发）</summary>
    public void PlayBgm(string path)
    {
        AudioClip clip = LoadClip(path);
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        bgmSource.Stop();
    }

    #endregion

    #region 音量控制

    /// <summary>设置音乐音量（0~1），并写入存档</summary>
    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        SaveManager.Instance.Data.musicVolume = MusicVolume;
        SaveManager.Instance.Save();
    }

    /// <summary>设置音效音量（0~1），并写入存档</summary>
    public void SetSoundVolume(float volume)
    {
        SoundVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        SaveManager.Instance.Data.soundVolume = SoundVolume;
        SaveManager.Instance.Save();
    }

    private void ApplyVolume()
    {
        if (bgmSource != null) bgmSource.volume = MusicVolume;
        // sfxSource 的 volume 保持 1，实际音量由 PlayOneShot 的第二个参数逐次决定
    }

    #endregion

    #region 资源加载

    /// <summary>
    /// 加载音频，找不到就返回 null 并只警告一次。
    /// 【为什么不用 ResManager.Load】它会 LogError，音频资源没做时会刷屏报错，
    /// 而音频属于"没有也能玩"的降级资源，这里做静默处理。
    /// </summary>
    private AudioClip LoadClip(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (clipCache.TryGetValue(path, out AudioClip cached))
        {
            return cached;
        }

        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            if (missingWarned.Add(path))
            {
                Debug.LogWarning($"[AudioManager] 找不到音频 Resources/{path}，已静默跳过（补上资源后自动生效）");
            }
            return null;
        }

        clipCache[path] = clip;
        return clip;
    }

    #endregion
}
