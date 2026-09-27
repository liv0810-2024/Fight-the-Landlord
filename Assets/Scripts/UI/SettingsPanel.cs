using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置面板：调背景音乐和音效的音量。
/// 音量不在这里存 —— AudioManager 自己就负责"改内存 + 应用 + 写存档"一条龙。
/// </summary>
public class SettingsPanel : BasePanel
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Button backButton;

    /// <summary>
    /// 正在把当前音量同步到滑块上。
    /// 【为什么需要】slider.value = x 这行赋值本身会触发 onValueChanged，
    /// 不拦住的话就成了"打开面板 → 触发回调 → 又设一次音量 → 又写一次存档"。
    /// </summary>
    private bool isSyncing;

    private void Awake()
    {
        UIManager.Instance.RegisterPanel(UIName.SettingsPanel, this);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (soundSlider != null) soundSlider.onValueChanged.AddListener(OnSoundChanged);
        if (backButton != null) backButton.onClick.AddListener(OnClickBack);
        Close();
    }

    /// <summary>每次打开都把滑块拨到当前音量。</summary>
    protected override void OnOpen()
    {
        isSyncing = true;
        if (musicSlider != null) musicSlider.value = AudioManager.Instance.MusicVolume;
        if (soundSlider != null) soundSlider.value = AudioManager.Instance.SoundVolume;
        isSyncing = false;
    }

    private void OnMusicChanged(float value)
    {
        if (isSyncing) return; //上面那次同步赋值触发的，不是玩家拖的
        AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSoundChanged(float value)
    {
        if (isSyncing) return;
        AudioManager.Instance.SetSoundVolume(value);
    }

    private void OnClickBack()
    {
        AudioManager.Instance.PlaySfx(GameAudio.ButtonClick);
        Close();
    }

    private void OnDestroy()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
        if (backButton != null) backButton.onClick.RemoveListener(OnClickBack);
    }
}
