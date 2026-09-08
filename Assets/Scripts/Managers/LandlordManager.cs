using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 抢地主管理器:负责抢地主流程、底牌归属、倍数计算。
/// </summary>
public class LandlordManager : Singleton<LandlordManager>
{
    #region 字段
    /// <summary>记录谁是地主 地主索引：0=我，1=左边AI，2=右边AI，-1=还没确定</summary>
    private int landLordIndex=-1; 

    /// <summary>
    /// 当前是谁选择
    /// </summary>
    private int currentBidderIndex=-1; 

    /// <summary>
    /// 是否处于抢地主阶段
    /// </summary>
    private bool isBidding=false; 

    /// <summary>
    /// 第一个抢的人
    /// </summary>
    private int firstBidderIndex=-1;

    /// <summary>
    /// 最后一个抢的人
    /// </summary>
    private int currentTopBidder=-1;

    /// <summary>
    /// 几个人做过选择（用于判断地主选择阶段是否结束）
    /// </summary>
    private int actedCount=0;

    /// <summary>
    /// 几个人抢过地主
    /// </summary>
    private int bidCount=0;

    /// <summary>
    /// 是否第二轮
    /// </summary>
    private bool isSecondChance=false;

    /// <summary>
    /// 地主是否已确认
    /// </summary>
    private bool isLandlordConfirmed=false;

    /// <summary>
    /// 倍数
    /// </summary>
    private int multiple=1;
    #endregion
    #region 生命周期
    protected override void Awake()
    {
        base.Awake();
        ResetState();
    }
    #endregion

    /// <summary>
    /// 把全部字段恢复到初始值，用于每局游戏重新开始时调用。
    /// </summary>
    public void ResetState()
    {
        landLordIndex=-1;
        currentBidderIndex=-1;
        firstBidderIndex=-1;
        currentTopBidder=-1;
        actedCount=0;
        bidCount=0;
        isBidding=false;
        isLandlordConfirmed=false;
        isSecondChance=false;
        multiple=1;
    }


    /// <summary>
    /// 开始一轮抢地主。先初始化全部字段，再推进到第一个人。
    /// </summary>
    /// <param name="firstPlayerIndex">随机选出的第一个叫地主的人 (0~2)</param>
    public void StartBidding(int first)
    {
        if (first < 0 || first > 2)
        {
            Debug.LogError($"[LandlordManager] StartBidding 参数非法: {first}，已修正为 0");
            first=0;
        }
       ResetState();
       currentBidderIndex=first;
       isBidding=true;
       Debug.Log($"[LandlordManager] 抢地主开始，{GetPlayerName(first)} 先叫");
       ContinueBidding();
    }

    /// <summary>
    /// 负责判断下一步做什么
    /// </summary>
    public void ContinueBidding()
    {
        if(!isBidding)return;
        if (currentBidderIndex == 0)
        {
            //打开抢地主UI
            EventCenter.Instance.Trigger(GameEvent.UI_OpenGrabPanel);
             Debug.Log("[LandlordManager] 等待玩家选择...");
        }
        else
        {
            StartCoroutine(AIBidAfterDelay(currentBidderIndex));
        }
    }

    /// <summary>
    /// 统一处理玩家和 AI 的叫地主选择。
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <param name="wantsLandlord"></param>
    public void ResolveBid(int playerIndex,bool wantsLandlord)
    {
        if (!isBidding)
        {
            Debug.LogWarning("[LandlordManager] ResolveBid 被调用但 isBidding=false,已忽略");
            return;
        }
        // 【防御2】不是当前轮到的人，直接返回
        // 防止 UI 重复点击、协程超时后状态已变、或者并发调用
        if (playerIndex != currentBidderIndex)
        {
            Debug.LogWarning($"[LandlordManager] 不是 {GetPlayerName(playerIndex)} 的回合，" + $"当前是 {GetPlayerName(currentBidderIndex)} 的回合");
            return;
        }
        //记录本次选择
         actedCount++;
        //玩家选择抢
        if (wantsLandlord)
        {
            bidCount++;
            if (bidCount == 1)
            {
                firstBidderIndex=playerIndex;
            }
            currentTopBidder=playerIndex;
            multiple++;
            Debug.Log($"[LandlordManager] {GetPlayerName(playerIndex)} 选择「抢地主」，倍数={multiple}");
           // 【关键规则】第二轮机会：第一个叫地主的人再次叫 → 直接确定，不再问其他人
            if (isSecondChance && playerIndex == firstBidderIndex)
            {
                Debug.Log($"[LandlordManager] 第一个叫的人 {GetPlayerName(playerIndex)} 第二轮再抢，直接确定！");
                ConfirmLandlord(playerIndex);
                return;
            }
        }
        else
        {
            Debug.Log($"[LandlordManager] {GetPlayerName(playerIndex)} 选择「不抢」");
        }
        if (actedCount >= 3)
            {
                FinishBidding();
            }
            else
            {
                currentBidderIndex=(currentBidderIndex+1)%3;
                ContinueBidding();
            }
    }

    /// <summary>
    /// 一轮叫地主结束后的判断逻辑
    /// </summary>
    private void FinishBidding()
    {
        Debug.Log($"[LandlordManager] 一轮结束：" +
                  $"bidCount={bidCount}, " +
                  $"firstBidder={GetPlayerName(firstBidderIndex)}, " +
                  $"topBidder={GetPlayerName(currentTopBidder)}");
        //处理第一轮抢完地主之后的事情
        if (bidCount == 0)
        {
            //没人抢就重新发牌
            Debug.Log("[LandlordManager] 无人叫地主，重新发牌");
            StartCoroutine(DelayedRedeal());
            return;
        }
        if (bidCount == 1)
        {
            ConfirmLandlord(firstBidderIndex);
            return;
        }
        //多人叫地主
        if (isSecondChance)
        {
            Debug.Log($"[LandlordManager] 第二轮结束，{GetPlayerName(currentTopBidder)} 是地主");
            ConfirmLandlord(currentTopBidder);
            return;
        }
        if (firstBidderIndex != currentTopBidder)
        {
            Debug.Log($"[LandlordManager] {GetPlayerName(firstBidderIndex)} 被超越，给予第二次机会");
            isSecondChance=true;
            currentBidderIndex=firstBidderIndex;
            actedCount=0;
            ContinueBidding();
        }
        else
        {
            ConfirmLandlord(currentTopBidder);
        }
    }

    /// <summary>
    /// AI 延迟后自动叫地主。
    /// </summary>
    /// <param name="playerIndex">AI 的玩家索引 (1 或 2)</param>
    private IEnumerator AIBidAfterDelay(int playerIndex)
    {
        // 随机延迟，模拟 AI 思考时间（每个 AI 速度略有不同，更像真人）
        float delay = Random.Range(1.0f, 1.5f);
        yield return new WaitForSeconds(delay);

        // 【防御】等待期间状态可能已经改变
        // 比如：游戏被重置、地主已经确定、或者轮到别人了
        if (!isBidding) yield break;
        if (currentBidderIndex != playerIndex) yield break;

        // AI 根据手牌强度决策
        bool wantsLandlord = DecideAIBid(playerIndex);

        Debug.Log($"[LandlordManager] {GetPlayerName(playerIndex)} AI 决策: " +
                  $"{(wantsLandlord ? "抢地主" : "不抢")}");

        // 提交决策，进入 ResolveBid 核心流程
        ResolveBid(playerIndex, wantsLandlord);
    }

    /// <summary>
    /// AI 叫地主决策算法 —— 评分制。
    /// </summary>
    /// <param name="playerIndex">AI 的玩家索引</param>
    /// <returns>true=叫地主，false=不叫</returns>
    private bool DecideAIBid(int playerIndex)
    {
        List<CardData> hand = GetHandByIndex(playerIndex);
        if (hand == null || hand.Count == 0) return false;

        int score = 0;

        // 统计每种权重的数量（用于判断炸弹、顺子潜力等）
        // Dictionary<int, int>：key=weight, value=该weight出现了几次
        Dictionary<int, int> weightCount = new Dictionary<int, int>();
        foreach (CardData card in hand)
        {
            if (weightCount.ContainsKey(card.weight))
                weightCount[card.weight]++;
            else
                weightCount[card.weight] = 1;
        }

        // 逐张评分
        bool hasBigJoker = false;
        bool hasSmallJoker = false;

        foreach (CardData card in hand)
        {
            switch (card.weight)
            {
                case 17: // 大王
                    score += 5;
                    hasBigJoker = true;
                    break;
                case 16: // 小王
                    score += 4;
                    hasSmallJoker = true;
                    break;
                case 15: // 2
                    score += 3;
                    break;
                case 14: // A
                    score += 2;
                    break;
                // 其余牌不加分
            }
        }

        // 火箭（同时拥有大小王）：额外加分
        if (hasBigJoker && hasSmallJoker)
        {
            score += 8;
        }

        // 炸弹（4张及以上同点数）：每组加6分
        // 遍历 weightCount，检查每种 weight 的出现次数
        foreach (var kvp in weightCount)
        {
            if (kvp.Value >= 4)
            {
                score += 6;
            }
        }

        // 顺子潜力（连续5个不同权重都存在 ≥1 张）
        // 遍历 3~A 的范围，检查连续性
        int consecutiveCount = 0;
        for (int w = 3; w <= 14; w++) // 3=Three, 14=A
        {
            if (weightCount.ContainsKey(w) && weightCount[w] >= 1)
            {
                consecutiveCount++;
                if (consecutiveCount >= 5)
                {
                    score += 3;
                    break; // 只加一次，不重复加分
                }
            }
            else
            {
                consecutiveCount = 0; // 连续性中断，重置
            }
        }

        Debug.Log($"[LandlordManager] {GetPlayerName(playerIndex)} 手牌评分: {score} 分");

        // 根据评分做决策（加入随机因素，让 AI 更"像人"）
        if (score >= 10) return true;               // 好牌，一定抢
        if (score >= 6) return Random.value < 0.7f;  // 还行，70%概率抢
        if (score >= 3) return Random.value < 0.4f;  // 一般，40%概率抢
        return false;                                 // 烂牌，不抢
    }

    /// <summary>
    /// 延迟重新发牌
    /// </summary>
    /// <returns></returns>
    private IEnumerator DelayedRedeal()
    {
        yield return null;
        ResetState();
        GameMainManager.Instance.StartGame();
    }

    /// <summary>
    /// 确定地主：记录身份、发底牌、翻倍数。
    /// </summary>
    /// <param name="index">玩家索引</param>
    public void ConfirmLandlord(int index)
    {
        if (index < 0 || index > 2)
        {
            Debug.LogError($"[LandlordManager] ConfirmLandlord 参数非法: {index}");
            return;
        }
        isBidding=false;
        landLordIndex = index;
        isLandlordConfirmed = true;
        AssignBottomCards(index);
        Debug.Log("[LandlordManager]地主确定：" + GetPlayerName(index) +"，倍数 " + multiple);
        EventCenter.Instance.Trigger(GameEvent.Game_GrabLandlord,index);
    }

    /// <summary>
    /// 发底牌
    /// </summary>
    /// <param name="index"></param>
     public void AssignBottomCards(int index)
    {
        //数据层：底牌加进地主手牌
        List<CardData> hand=GetHandByIndex(index);
        if (hand == null)
        {
            Debug.LogError($"[LandlordManager] AssignBottomCards: 无法获取玩家 {index} 的手牌");
            return;
        }
        //检查底牌
        List<CardData> bottomCards=DeckManager.Instance.bottomCards;
        if (bottomCards == null || bottomCards.Count == 0)
        {
            Debug.LogError("[LandlordManager] AssignBottomCards: 底牌为空");
            return;
        }
        // 打印底牌（方便调试）
        Debug.Log($"[LandlordManager] 底牌: {string.Join(", ", bottomCards.ConvertAll(c => $"{c.rank}{c.suit}"))}");
        hand.AddRange(DeckManager.Instance.bottomCards);
        hand.Sort((a,b)=>a.weight.CompareTo(b.weight));
        bottomCards.Clear();
    }

    /// <summary>
    /// 玩家选择"抢"或"不抢"的入口。
    /// </summary>
    /// <param name="grab">true=抢，false=不抢</param>
    public void PlayerChooseGrab(bool wantsLandlord)
    {
        if (!isBidding)
        {
            Debug.LogWarning("[LandlordManager] 当前不在抢地主阶段，忽略玩家操作");
            return;
        }
        ResolveBid(0,wantsLandlord);
    }
    
    /// <summary>
    /// 获取地主索引（供其他模块查询）
    /// </summary>
    /// <returns></returns>
    public int GetLandlordIndex()=>landLordIndex;

    /// <summary>
    /// 获取当前倍数（供结算模块使用）
    /// </summary>
    /// <returns></returns>
    public int GetMultiple()=>multiple;

    /// <summary>
    /// 是否处于抢地主阶段（供外部判断）
    /// </summary>
    /// <returns></returns>
    public bool IsBidding()=>isBidding;
   
    /// <summary>
    /// 获取玩家名字（用于日志输出）。
    /// 【库洛风格】统一的名字获取入口，避免硬编码字符串分散在各处。
    /// </summary>
    /// <param name="index">玩家索引</param>
    /// <returns>玩家名字</returns>
    private string GetPlayerName(int index)
    {
        switch (index)
        {
            case 0: return "玩家";
            case 1: return "电脑AI(左)";
            case 2: return "电脑AI(右)";
            default: return $"未知({index})";
        }
    }

    /// <summary>
    /// 通过索引获取手牌数据。
    /// </summary>
    /// <param name="index">玩家索引</param>
    /// <returns>手牌列表，索引无效时返回 null</returns>
    private List<CardData> GetHandByIndex(int index)
    {
        switch (index)
        {
            case 0: return DeckManager.Instance.myHand;
            case 1: return DeckManager.Instance.leftHand;
            case 2: return DeckManager.Instance.rightHand;
            default:
                Debug.LogError($"[LandlordManager] GetHandByIndex: 非法索引 {index}");
                return null;
        }
    }
}