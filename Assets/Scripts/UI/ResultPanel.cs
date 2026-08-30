using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanel : BasePanel
{
    [SerializeField] Text resultText;
    [SerializeField] Button restartButton;
    private void Awake()
    {
        UIManager.Instance.RegisterPanel(UIName.ResultPanel, this);
        restartButton.onClick.AddListener(OnClickRestart);
        EventCenter.Instance.Register(GameEvent.Game_RoundOver, OnRounOver);
        Close();
    }

    /// <summary>
    /// 本局结束，显示赢家并打开面板。
    /// </summary>
    /// <param name="param"></param>
    private void OnRounOver(object param)
    {
        int winner=(int)param;
        resultText.text = GetWinnerName(winner) + "赢了";
        Open();
    }
    private void OnClickRestart()
    {
        GameMainManager.Instance.RestarGame();
    }
    private void OnDestroy()
    {
        EventCenter.Instance.UnRegister(GameEvent.Game_RoundOver, OnRounOver);
        restartButton.onClick.RemoveListener(OnClickRestart);
    }
    private string GetWinnerName(int winner)
    {
        if (winner == 0) return "你";
        if (winner == 1) return "左AI";
        return "右AI";
    }
}
