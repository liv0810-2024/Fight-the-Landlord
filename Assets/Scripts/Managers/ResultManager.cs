using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 结算管理器：负责胜负判定、倍数结算、分数累计。
/// 只算数据，不碰 UI —— 算完通过 Game_ResultReady 事件广播给表现层。
/// </summary>
public class ResultManager : Singleton<ResultManager>
{
    /// <summary>底分：最终得分 = 底分 × 倍数</summary>
    private const int BASE_SCORE = 100;

    /// <summary>三人累计得分，下标=玩家索引。跨局保留，重开不清零。</summary>
    public int[] totalScores = new int[3];

    /// <summary>最近一局的结算结果</summary>
    public ResultData lastResult;

    protected override void Awake()
    {
        base.Awake();
        // 直接接上存档里的数组（同一个引用），改它就是改存档，
        // 结算完调 SaveManager.Save() 即可落盘。
        totalScores = SaveManager.Instance.Data.totalScores;
        EventCenter.Instance.Register(GameEvent.Game_RoundOver, OnRoundOver);
    }

    private void OnDestroy()
    {
        // 注册了就要反注册，避免对象销毁后事件仍持有引用
        EventCenter.Instance.UnRegister(GameEvent.Game_RoundOver, OnRoundOver);
    }

    #region 结算核心

    /// <summary>
    /// 回调：有人出完牌了 → 计算结算结果并广播。
    /// </summary>
    /// <param name="param">int 类型，先出完牌的玩家索引</param>
    public void OnRoundOver(object param)
    {
        if (!(param is int))
        {
            Debug.LogError($"[ResultManager] Game_RoundOver 参数类型非法: {param}");
            return;
        }

        int winnerIndex = (int)param;

        // 计算结算数据
        ResultData data = CalculateResult(winnerIndex);
        if (data == null) return; // 计算失败，内部已打日志

        lastResult = data;

        // 广播"结算数据已就绪"，让 UI 层刷新
        EventCenter.Instance.Trigger(GameEvent.Game_ResultReady, data);
    }

    /// <summary>
    /// 计算一局结算结果。
    ///
    /// 【计分规则】设 N = 底分 × 倍数
    ///   地主赢：地主 +2N，两个农民各 -N
    ///   农民赢：地主 -2N，两个农民各 +N
    ///   总和恒为 0（1 打 2 的零和博弈）
    ///
    /// 【阵营判定】先出完牌的人 == 地主 → 地主赢；否则农民赢（两个农民一队，都赢）
    /// </summary>
    /// <param name="winnerIndex">先出完牌的玩家索引</param>
    /// <returns>结算数据；参数非法时返回 null</returns>
    private ResultData CalculateResult(int winnerIndex)
    {
        // 索引越界（哨兵值 -1 也会被挡在这里）
        if (winnerIndex < 0 || winnerIndex > 2)
        {
            Debug.LogError($"[ResultManager] winnerIndex 非法: {winnerIndex}");
            return null;
        }

        // landLordIndex 初始值是 -1，若结算在抢地主之前被误触发会静默算错分，必须挡住
        int landlordIndex = LandlordManager.Instance.GetLandlordIndex();
        if (landlordIndex < 0 || landlordIndex > 2)
        {
            Debug.LogError($"[ResultManager] 地主尚未确定(landlordIndex={landlordIndex})，无法结算");
            return null;
        }

        int multiple = LandlordManager.Instance.GetMultiple();
        if (multiple < 1)
        {
            Debug.LogWarning($"[ResultManager] 倍数异常({multiple})，已兜底为 1");
            multiple = 1;
        }

        // 判定阵营
        bool isLandlordWin = (winnerIndex == landlordIndex);

        // === 春天 / 反春天：满足条件倍数翻倍 ===
        // 春天：地主赢，且两个农民一手牌都没出过
        // 反春天（夏天）：农民赢，且地主只出过 1 手牌（就是开局那一手）
        bool isSpring = false;
        if (isLandlordWin)
        {
            bool farmersNeverPlayed = true;
            for (int i = 0; i < 3; i++)
            {
                if (i == landlordIndex) continue;
                if (PlayCardManager.Instance.GetPlayCount(i) > 0)
                {
                    farmersNeverPlayed = false;
                    break;
                }
            }
            isSpring = farmersNeverPlayed;
        }
        else
        {
            isSpring = PlayCardManager.Instance.GetPlayCount(landlordIndex) <= 1;
        }

        if (isSpring)
        {
            multiple *= 2;
            Debug.Log("[ResultManager] 触发春天/反春天，倍数翻倍");
        }

        // 计算本局得分
        int unit = BASE_SCORE * multiple;
        int[] roundScores = new int[3];
        for (int i = 0; i < 3; i++)
        {
            if (isLandlordWin)
                roundScores[i] = (i == landlordIndex) ? 2 * unit : -unit;
            else
                roundScores[i] = (i == landlordIndex) ? -2 * unit : unit;
        }

        // 累加到总分
        for (int i = 0; i < 3; i++)
        {
            totalScores[i] += roundScores[i];
        }

        // 打包成数据对象
        ResultData data = new ResultData();
        data.winnerIndex = winnerIndex;
        data.isLandlordWin = isLandlordWin;
        data.multiple = multiple;
        data.baseScore = BASE_SCORE;
        data.roundScores = roundScores;
        data.landlordIndex = landlordIndex;
        data.isSpring = isSpring;
        // 对外暴露内部数组必须给副本：数组是引用类型，直接赋值会让 UI 改到内部状态
        data.totalScores = (int[])totalScores.Clone();

        // 玩家所在阵营是否获胜：roundScores[0] 是我自己的得分，正数就是赢
        bool isPlayerWin = roundScores[0] > 0;
        SaveManager.Instance.RecordGame(isPlayerWin, landlordIndex == 0);

        Debug.Log($"[ResultManager] 结算完成：{(isLandlordWin ? "地主" : "农民")}胜，" +
                  $"倍数={multiple}，本局得分=[{string.Join(", ", roundScores)}]，" +
                  $"累计得分=[{string.Join(", ", totalScores)}]");

        return data;
    }

    #endregion

    #region 对外查询

    /// <summary>获取某个玩家的累计得分</summary>
    public int GetTotalScore(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex > 2)
        {
            Debug.LogWarning($"[ResultManager] GetTotalScore 索引非法: {playerIndex}");
            return 0;
        }
        return totalScores[playerIndex];
    }

    /// <summary>
    /// 重置单局状态（重开新一局时调用）。
    /// 只清单局数据，不动 totalScores —— 累计分是跨局的。
    /// </summary>
    public void ResetRoundState()
    {
        lastResult = null;
    }

    #endregion
}
