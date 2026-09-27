using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 封面面板：游戏标题 + "点击任意处进入"。
/// 点一下自己关掉，把主菜单叫出来。
/// </summary>
public class CoverPanel : BasePanel
{
    //【搭场景时连】铺满整个屏幕的透明按钮，专职接收"点哪儿都行"这个操作
    [SerializeField] private Button anywhereButton;

    private void Awake()
    {
        UIManager.Instance.RegisterPanel(UIName.CoverPanel, this);
        anywhereButton.onClick.AddListener(OnClickAnywhere);
        // 【为什么在这里碰一下 AudioManager】大厅是玩家进的第一个场景，
        // 这里不主动创建的话，要等到 GameScene 才会第一次听到 BGM。
        // PlayBgm 内部有"正在播就不重播"的保护，所以这句多调几次也无害。
        AudioManager.Instance.PlayBgm(GameAudio.BgmMain);
        // 封面是进游戏后的第一屏，直接开着
        Open();
    }

    private void OnClickAnywhere()
    {
        AudioManager.Instance.PlaySfx(GameAudio.ButtonClick);
        Close();
        UIManager.Instance.OpenPanel(UIName.MainMenuPanel);
    }

    private void OnDestroy()
    {
        anywhereButton.onClick.RemoveListener(OnClickAnywhere);
    }
}
