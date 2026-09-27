using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单：开始游戏 / 设置 / 退出。
/// 自己不做任何业务，三个按钮各自转交出去。
/// </summary>
public class MainMenuPanel : BasePanel
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        UIManager.Instance.RegisterPanel(UIName.MainMenuPanel, this);
        startButton.onClick.AddListener(OnClickStart);
        // 后两个做成可选：万一你不想要退出按钮，删掉物体也不至于报空引用
        if (settingsButton != null) settingsButton.onClick.AddListener(OnClickSettings);
        if (quitButton != null) quitButton.onClick.AddListener(OnClickQuit);
        // 初始收起来，等封面点进来再 Open
        Close();
    }

    private void OnClickStart()
    {
        AudioManager.Instance.PlaySfx(GameAudio.ButtonClick);
        GameLauncher.Instance.EnterGame();
    }

    private void OnClickSettings()
    {
        AudioManager.Instance.PlaySfx(GameAudio.ButtonClick);
        UIManager.Instance.OpenPanel(UIName.SettingsPanel);
    }

    private void OnClickQuit()
    {
        AudioManager.Instance.PlaySfx(GameAudio.ButtonClick);
        GameLauncher.Instance.QuitGame();
    }

    private void OnDestroy()
    {
        startButton.onClick.RemoveListener(OnClickStart);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnClickSettings);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnClickQuit);
    }
}
