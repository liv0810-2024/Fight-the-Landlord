using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结算面板：只负责显示结算数据，胜负规则全部由 ResultManager 处理。
/// </summary>
public class ResultPanel : BasePanel
{
    [SerializeField] Text resultText;
    [SerializeField] Button restartButton;

    private void Awake()
    {
        UIManager.Instance.RegisterPanel(UIName.ResultPanel, this);
        restartButton.onClick.AddListener(OnClickRestart);
        // 不再监听 Game_RoundOver（那时结算数据还没算好），改听 ResultManager 算完后广播的事件
        EventCenter.Instance.Register(GameEvent.Game_ResultReady, OnResultReady);
        Close();
    }

    /// <summary>
    /// 回调：结算数据已就绪 → 刷新文本并打开面板。
    /// </summary>
    private void OnResultReady(object param)
    {
        ResultData data = param as ResultData;
        if (data == null)
        {
            Debug.LogError("[ResultPanel] OnResultReady 参数不是 ResultData");
            return;
        }

        resultText.text = BuildResultText(data);
        Open();
    }

    /// <summary>
    /// 把结算数据拼成展示文本。
    /// </summary>
    private string BuildResultText(ResultData data)
    {
        string campName = data.isLandlordWin ? "地主" : "农民";
        string myRole = data.landlordIndex == 0 ? "地主" : "农民";

        // 下标 0 就是自己，正数补个 "+" 号
        int myRoundScore = data.roundScores[0];
        string sign = myRoundScore >= 0 ? "+" : "";

        // 春天/反春天额外提示一行
        string springLine = data.isSpring ? "春天！倍数翻倍\n" : "";

        return $"{GetWinnerName(data.winnerIndex)}先出完牌\n" +
               $"{campName}获胜（你是{myRole}）\n" +
               springLine +
               $"本局 {data.multiple} 倍，你 {sign}{myRoundScore} 分\n" +
               $"你的累计得分：{data.totalScores[0]} 分";
    }

    private void OnClickRestart()
    {
        // 重开前清掉单局结算状态（累计分保留）
        ResultManager.Instance.ResetRoundState();
        GameMainManager.Instance.RestarGame();
    }

    private void OnDestroy()
    {
        EventCenter.Instance.UnRegister(GameEvent.Game_ResultReady, OnResultReady);
        restartButton.onClick.RemoveListener(OnClickRestart);
    }

    private string GetWinnerName(int winner)
    {
        if (winner == 0) return "你";
        if (winner == 1) return "左AI";
        return "右AI";
    }
}
