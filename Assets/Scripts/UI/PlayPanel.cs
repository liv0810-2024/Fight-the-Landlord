using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 出牌面板
/// </summary>
public class PlayPanel : BasePanel
{
    [SerializeField] public Button playButton;
    [SerializeField] public Button passButton;
    //【需要在 Inspector 里拖】场景中新建一个按钮，拖到这个槽位，否则提示功能点不到
    [SerializeField] public Button hintButton;

    private void Awake()
    {
        UIManager.Instance.RegisterPanel(UIName.PlayPanel, this);
        // 【改】原来监听的是 Game_GrabLandlord（地主确定），地主一确定就打开然后整局常驻。
        // 现在改听出牌权变更：只有轮到玩家自己时才出现。
        EventCenter.Instance.Register(GameEvent.Game_TurnChanged, OnTurnChanged);
        EventCenter.Instance.Register(GameEvent.Game_RoundOver, OnRoundOver);
        playButton.onClick.AddListener(OnClickPlay);
        passButton.onClick.AddListener(OnPlayerPass);
        if (hintButton != null) hintButton.onClick.AddListener(OnClickHint);
        Close();
    }

    private void OnDestroy()
    {
        EventCenter.Instance.UnRegister(GameEvent.Game_RoundOver, OnRoundOver);
        EventCenter.Instance.UnRegister(GameEvent.Game_TurnChanged, OnTurnChanged);
        playButton.onClick.RemoveListener(OnClickPlay);
        passButton.onClick.RemoveListener(OnPlayerPass);
        if (hintButton != null) hintButton.onClick.RemoveListener(OnClickHint);
    }

    /// <summary>
    /// 出牌权换人了。参数是当前该出牌的玩家索引，0=我。
    /// 【为什么要跟着显隐】AI 出牌那几秒按钮点下去也不会有反应，
    /// 亮着只会让人以为"是不是卡住了"；藏起来反而清楚：现在不归我操作。
    /// </summary>
    private void OnTurnChanged(object param)
    {
        int turn = (param is int) ? (int)param : -1;
        if (turn == 0) Open();
        else Close();
    }

    private void OnRoundOver(object param)
    {
        Close();
    }

    private void OnClickPlay()
    {
        PlayCardManager.Instance.PlayerPlayCards();
    }

    private void OnPlayerPass()
    {
        PlayCardManager.Instance.PlayerPass();
    }

    /// <summary>提示：每点一次换下一组出法，点到底绕回第一组。</summary>
    private void OnClickHint()
    {
        PlayCardManager.Instance.ShowHint();
    }
}
