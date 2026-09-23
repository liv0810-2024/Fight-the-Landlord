using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>出牌管理器：负责收集选牌、校验、出牌、轮转。</summary>
public class PlayCardManager : Singleton<PlayCardManager>
{
    public bool isRoundOver = false; //本局是否已结束
    private int currentTurn = 0; //当前行动玩家 我是0
    private List<CardData> lastPlayedCards; //上一手出的牌（用于压牌比较）
    private int lastPlayedPlayer = -1;  //上一手是谁出的：-1=没有，0我，1左AI，2右AI

    private int consecutivePassCount; //从上一手牌之后，连续有几名玩家选择了过牌
    private AIPlayer aiPlayer = new AIPlayer();

    /// <summary>每名玩家本局"出过几次牌"（不含过牌），下标=玩家索引。用于春天判定。</summary>
    private int[] playCounts = new int[3];

    /// <summary>提示按钮可选的出法列表（已按由小到大排好）。null 表示需要重新枚举。</summary>
    private List<List<CardData>> hintOptions;
    /// <summary>当前停在提示列表的第几个，-1 = 还没开始</summary>
    private int hintIndex = -1;
    /// <summary>正在批量应用提示选牌。用来区分"程序在选牌"和"玩家自己在点牌"。</summary>
    private bool isApplyingHint;

    protected override void Awake()
    {
        base.Awake();
        // 玩家自己动了选牌，提示的游标就作废，下次点提示从头开始
        EventCenter.Instance.Register(GameEvent.Card_Select, OnCardSelect);
    }

    private void OnDestroy()
    {
        EventCenter.Instance.UnRegister(GameEvent.Card_Select, OnCardSelect);
    }

    private void OnCardSelect(object param)
    {
        // ShowHint 自己也会触发这个事件，不能让它把自己的游标重置掉
        if (isApplyingHint) return;
        hintIndex = -1;
    }
    private void Update()
    {
        //临时快捷键（也可以挂到 PlayPanel 的按钮上）
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerPlayCards();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            PlayerPass();
        }
        // H = 提示：自动帮你选出"最小能压过"的牌
        if (Input.GetKeyDown(KeyCode.H))
        {
            ShowHint();
        }
    }
    /// <summary>从手牌里收集所有被选中的牌，转成 CardData 列表。</summary>
    private List<CardData> CollectSelectedCards()
    {
        List<CardData> selectedData = new List<CardData>();
        foreach (Card card in CardLayoutManager.Instance.GetMyHandCards())
        {
            if (card.isSelected)
            {
                selectedData.Add(card.cardData);
            }
        }
        return selectedData;
    }
    /// <summary>玩家出牌入口：收集 → 校验→ 出牌。</summary>
    public void PlayerPlayCards()
    {
        if (isRoundOver) return;
        if (currentTurn != 0) return;
        List<CardData> selected = CollectSelectedCards();
        if (!ValidatePlay(selected)) return;
        //真正出牌
        DoPlayCards(selected);
    }

    /// <summary>
    /// 检查玩家本次选择的牌是否可以出。
    /// </summary>
    /// <param name="selectedCards"></param>
    /// <returns></returns>
    public bool ValidatePlay(List<CardData> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count == 0)
        {
            Debug.Log("没有选牌");
            return false;
        }
        List<CardData> myHand = DeckManager.Instance.myHand;
        foreach (CardData selectedCard in selectedCards)
        {
            //判断是否属于自己的牌
            if (selectedCard == null || !myHand.Contains(selectedCard))
            {
                Debug.Log("当前选中的牌不是自己的手牌");
                return false;
            }
        }
        //牌型不对
        if (CardRules.CheckCardType(selectedCards) == CardType.None)
        {
            Debug.Log("这组牌不是合法牌型！");
            return false;
        }
        if (!CardRules.CanBeat(selectedCards, lastPlayedCards))
        {
            Debug.Log("压不过上家的牌！");
            return false;
        }
        return true;
    }
    ///<summary>玩家真正出牌：数据移除、刷新显示、记录上一手、轮转。</summary>
    private void DoPlayCards(List<CardData> played)
    {
        List<CardData> myHand = DeckManager.Instance.myHand;
        //数据层
        foreach (CardData cardData in played)
        {
            myHand.Remove(cardData);
        }
        lastPlayedCards = new List<CardData>(played);
        lastPlayedPlayer = 0; //我出的
        consecutivePassCount = 0;
        playCounts[0]++; // 记一次出牌，供春天判定
        CardLayoutManager.Instance.ShowMyHand(); //表现层刷新
        CardLayoutManager.Instance.ShowPlayArea(played);
        EventCenter.Instance.Trigger(GameEvent.Game_PlayCard, played);
        Debug.Log($"[PlayCardManager] 玩家出了 {played.Count} 张牌");
        if (CheckWin(0)) return;
        // 出牌只是推进回合，不是"过牌"，不能调 PassTurn
        GoToNextTurn();
    }
    /// <summary>
    /// AI的决策
    /// </summary>
    private void AITurn()
    {
        if (isRoundOver) return;

        // 获取 AI 手牌
        List<CardData> aiCards = GetHandByTurn(currentTurn);
        if (aiCards == null || aiCards.Count == 0)
        {
            Debug.LogWarning($"[PlayCardManager] AITurn: 玩家{currentTurn}手牌为空");
            return;
        }

        // 委托 AI 决策
        List<CardData> toPlay = AIDecide(aiCards);

        if (toPlay == null || toPlay.Count == 0)
        {
            // AI 选择过牌
            Debug.Log($"[PlayCardManager] {GetNameByTurn(currentTurn)} 要不起");
            PassTurn();
            return;
        }

        // 记录出的牌型（方便调试）
        CardType playedType = CardRules.CheckCardType(toPlay);
        Debug.Log($"[PlayCardManager] {GetNameByTurn(currentTurn)} 出了 {playedType}（{toPlay.Count}张）");

        // 数据层：从手牌中移除已出的牌
        foreach (CardData data in toPlay)
        {
            aiCards.Remove(data);
        }

        // 更新场上状态
        lastPlayedCards = new List<CardData>(toPlay);
        lastPlayedPlayer = currentTurn;
        consecutivePassCount = 0;
        playCounts[currentTurn]++; // 记一次出牌，供春天判定

        // 表现层：刷新 AI 手牌显示 + 显示打出的牌
        RefreshAiHand(currentTurn);
        CardLayoutManager.Instance.ShowPlayArea(toPlay);
        EventCenter.Instance.Trigger(GameEvent.Game_PlayCard, toPlay);

        // 检查胜利
        if (CheckWin(currentTurn)) return;

        // 轮到下一个人（出牌 ≠ 过牌）
        GoToNextTurn();
    }
    /// <summary>
    /// ai获取牌
    /// </summary>
    /// <param name="playerIndex"></param>
    /// <returns></returns>
    private List<CardData> GetHandByTurn(int playerIndex)
    {
        if (playerIndex == 0) return DeckManager.Instance.myHand;
        if (playerIndex == 1) return DeckManager.Instance.leftHand;
        if (playerIndex == 2) return DeckManager.Instance.rightHand;
        Debug.Log("获取手牌错误");
        return null;
    }
    /// <summary>
    /// AI决策：返回要出的牌，返回 null表示"过"
    /// </summary>
    /// <param name="handCards"></param>
    /// <returns></returns>
    private List<CardData> AIDecide(List<CardData> handCards)
{
    return aiPlayer.Decide(handCards, lastPlayedCards, lastPlayedPlayer, currentTurn);
}
    /// <summary>
    /// 过牌：记一次"过"，再推进到下一家。
    /// </summary>
    private void PassTurn()
    {
        consecutivePassCount++;
        GoToNextTurn();
    }

    /// <summary>
    /// 推进到下一家。出牌后和过牌后都走这里。
    ///
    /// 【注意】出牌后绝对不能调 PassTurn —— 那会凭空多记一次"过"，
    /// 导致只要一家过牌就被误判成"本圈结束"，牌权白白送给别人。
    /// </summary>
    private void GoToNextTurn()
    {
        // 连续两家过牌 = 本圈结束。清空上一手牌后照常轮转两格，
        // 正好回到最后出牌的那个人，由他重新自由出牌。
        if (consecutivePassCount >= 2)
        {
            ResetTrick();
        }
        currentTurn = (currentTurn + 1) % 3;
        // 牌权换了人，提示列表（尤其是"能压过上一手"的判断）需要重算
        InvalidateHint();
        // 广播出去：出牌面板据此显示/隐藏按钮
        EventCenter.Instance.Trigger(GameEvent.Game_TurnChanged, currentTurn);
        if (currentTurn != 0)
        {
            StartCoroutine(AIPlayDelayed());
        }
        else
        {
            Debug.Log("轮到你了！");
        }
    }
    private IEnumerator AIPlayDelayed()
    {
        yield return new WaitForSeconds(1f);
        AITurn();
    }
    /// <summary>
    /// 玩家过牌：非先手时才能过S
    /// </summary>
    public void PlayerPass()
    {
        if (isRoundOver) return;
        if (currentTurn != 0)
        {
            return;
        }
        if (lastPlayedCards == null || lastPlayedCards.Count == 0)
        {
            Debug.Log("你是先手必须先出牌!");
            return;
        }
        Debug.Log("玩家过牌");
        PassTurn();
    }
    public string GetNameByTurn(int turn)
    {
        if (turn == 1) return "左AI";
        if (turn == 0) return "玩家";
        return "右AI";
    }
    private void RefreshAiHand(int turn)
    {
        if (turn == 1)
        {
            CardLayoutManager.Instance.ShowLeftHand();
        }
        else
        {
            CardLayoutManager.Instance.ShowRightHand();
        }
    }
    /// <summary>
    /// 检查某个玩家是否出完了所有牌（胜利）。
    /// </summary>
    /// <param name="playerIndex"></param>
    private bool CheckWin(int playerIndex)
    {
        List<CardData> hand = GetHandByTurn(playerIndex);
        if (hand == null)
        {
            Debug.Log($"检查胜利失败：玩家索引={playerIndex} 无效");
            return false;
        }
        Debug.Log($"检查胜利：玩家索引={playerIndex}," + $"玩家名称={GetNameByTurn(playerIndex)}," + $"手牌数量={hand.Count}");

        if (hand.Count > 0)
        {
            return false;
        }
        isRoundOver = true;
        Debug.Log(GetNameByTurn(playerIndex) + "出完手上所有牌，胜利！！！");
        EventCenter.Instance.Trigger(GameEvent.Game_RoundOver, playerIndex);
        return true;

    }
    /// <summary>
    /// 初始化会和状态
    /// </summary>
    /// <param name="firstPlayer"></param>
    public void StartPlay(int firstPlayer)
    {
        isRoundOver = false;
        consecutivePassCount = 0;
        currentTurn = firstPlayer;
        lastPlayedCards = null;
        lastPlayedPlayer = -1;
        InvalidateHint();
        // 【顺序要紧】这一句触发时，PlayCardManager 在 Game_GrabLandlord 的回调链里。
        // 出牌面板只认这个事件，所以谁先出的牌权都能正确反映到按钮显隐上。
        EventCenter.Instance.Trigger(GameEvent.Game_TurnChanged, currentTurn);
        if (firstPlayer != 0)
        {
            StartCoroutine(AIPlayDelayed());
        }
        else
        {
            Debug.Log("你是地主请出牌");
        }
    }
    /// <summary>
    /// 出牌提示：把所有能打的出法由小到大列出来，每点一次换下一个，循环。
    ///
    /// 【和旧版的区别】旧版直接拿 AIPlayer 的最优解，只会给一个答案，
    /// 玩家不满意就没辙。现在改成遍历全部可行解 —— 第一次给最小的，
    /// 再点给次小的，点到底绕回开头。
    /// </summary>
    public void ShowHint()
    {
        if (isRoundOver) return;
        if (currentTurn != 0)
        {
            Debug.Log("[PlayCardManager] 还没轮到你，无法提示");
            return;
        }

        // 手牌和牌权都没变的话，复用上次枚举的结果，只是在列表里往后挪一格
        if (hintOptions == null)
        {
            hintOptions = PlayEnumerator.Enumerate(DeckManager.Instance.myHand, lastPlayedCards);
            hintIndex = -1;
        }

        if (hintOptions.Count == 0)
        {
            Debug.Log("[PlayCardManager] 提示：没有能出的牌，只能过牌");
            return;
        }

        // 绕圈：点到最后一条再点就回到第一条
        hintIndex = (hintIndex + 1) % hintOptions.Count;
        List<CardData> suggestion = hintOptions[hintIndex];

        // 一次遍历同时处理"选中建议牌"和"取消其它牌"
        // raiseEvent 传 false：批量选牌只响一次音效，不要一次炸出好几声
        // 【顺序有讲究】Trigger 必须在 isApplyingHint 还是 true 的时候发出去。
        // 反过来的话，下面这个事件会同步回调到 OnCardSelect，
        // 把刚算好的 hintIndex 又重置成 -1 —— 循环切换就永远停在第一组。
        isApplyingHint = true;
        foreach (Card card in CardLayoutManager.Instance.GetMyHandCards())
        {
            card.SetSelected(suggestion.Contains(card.cardData), false);
        }
        EventCenter.Instance.Trigger(GameEvent.Card_Select, null);
        isApplyingHint = false;

        Debug.Log($"[PlayCardManager] 提示 {hintIndex + 1}/{hintOptions.Count}：" +
                  $"出 {suggestion.Count} 张（{CardRules.CheckCardType(suggestion)}）");
    }

    /// <summary>手牌或牌权变了，提示缓存整体作废，下次点提示要重新枚举。</summary>
    private void InvalidateHint()
    {
        hintOptions = null;
        hintIndex = -1;
    }

    /// <summary>查询某名玩家本局出过几次牌（供春天判定使用）</summary>
    public int GetPlayCount(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex > 2)
        {
            Debug.LogWarning($"[PlayCardManager] GetPlayCount 索引非法: {playerIndex}");
            return 0;
        }
        return playCounts[playerIndex];
    }

    public void ResetState()
    {
        isRoundOver = false;
        currentTurn = 0;
        lastPlayedCards = null;
        lastPlayedPlayer = -1;
        // 【修复】原来漏了两个字段，重开一局后：
        //   consecutivePassCount 带着上一局的计数 → 第一手就可能被误判成"两家都过"
        //   playCounts 不清零 → 春天判定直接算错
        consecutivePassCount = 0;
        System.Array.Clear(playCounts, 0, playCounts.Length);
        InvalidateHint();
    }

    /// <summary>
    /// 负责清理当前这一轮牌权
    /// </summary>
    private void ResetTrick()
    {
        lastPlayedPlayer = -1;
        lastPlayedCards = null;
        consecutivePassCount = 0;
        //清空屏幕中央的上一手牌
        CardLayoutManager.Instance.ShowPlayArea(null);
    }

    /// <summary>
    /// 负责判断当前是不是自由出牌
    /// </summary>
    /// <returns></returns>
    private bool IsFreeTurn()
    {
        return lastPlayedCards == null || lastPlayedCards.Count == 0;
    }
}