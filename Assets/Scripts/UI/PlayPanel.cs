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
    private void Awake()
    {
        UIManager.Instance.RegisterPanel(UIName.PlayPanel, this);
        EventCenter.Instance.Register(GameEvent.Game_GrabLandlord, OnStartPlay);
        EventCenter.Instance.Register(GameEvent.Game_RoundOver, OnRoundOver);
        playButton.onClick.AddListener(OnClickPlay);
        passButton.onClick.AddListener(OnPlayerPass);
        Close();
    }
    private void OnStartPlay(object param)
    {
        Open();
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

    private void OnDestroy()
    {
        EventCenter.Instance.UnRegister(GameEvent.Game_RoundOver, OnRoundOver);
        EventCenter.Instance.UnRegister(GameEvent.Game_GrabLandlord, OnStartPlay);
        playButton.onClick.RemoveListener(OnClickPlay);
        passButton.onClick.RemoveListener(OnPlayerPass);
    }
}
