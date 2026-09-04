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
    protected override void Awake()
    {
        base.Awake();
    }
    private void Update()
    {
        //临时空格出牌
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerPlayCards();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            PlayerPass();
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
        consecutivePassCount=0;
        CardLayoutManager.Instance.ShowMyHand(); //表现层刷新
        CardLayoutManager.Instance.ShowPlayArea(played);
        //暂时只轮到左AI，AI 逻辑教程12做
        Debug.Log("玩家出了 " + played.Count + " 张牌,轮到左AI");
        if (CheckWin(0)) return;
        PassTurn();
    }
    /// <summary>
    /// AI的决策
    /// </summary>
    private void AITurn()
    {
        if (isRoundOver) return;
        List<CardData> aiCards = GetHandByTurn(currentTurn);
        List<CardData> toPlay = AIDecide(aiCards);
        if (toPlay == null)
        {
            Debug.Log(GetNameByTurn(currentTurn) + "要不起");
            PassTurn();
            return;
        }
        //移除牌
        foreach (CardData data in toPlay)
        {
            aiCards.Remove(data);
        }
        lastPlayedCards = new List<CardData>(toPlay);
        lastPlayedPlayer = currentTurn;
        consecutivePassCount=0;
        //刷新ai的牌
        RefreshAiHand(currentTurn);
        CardLayoutManager.Instance.ShowPlayArea(toPlay);
        Debug.Log(GetNameByTurn(currentTurn) + " 出了 " + toPlay.Count + " 张牌 ");
        if(CheckWin(currentTurn))return;
        PassTurn();
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
        bool isFree = lastPlayedCards == null || lastPlayedCards.Count == 0 || lastPlayedPlayer == currentTurn;
        if (isFree)
        {
            if (handCards.Count == 0) return null;
            return new List<CardData> { handCards[0] };
        }
        foreach (CardData handCard in handCards)
        {
            List<CardData> mine = new List<CardData> { handCard };
            if (CardRules.CanBeat(mine, lastPlayedCards))
                return mine;
        }
        return null;
    }
    /// <summary>
    /// 轮到下一个人。轮到 AI时延迟出牌。
    /// </summary>
    private void PassTurn()
    {
        consecutivePassCount++;
        if (consecutivePassCount >= 2)
        {
            ResetTrick();
        }
        currentTurn = (currentTurn + 1) % 3;
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
        if (hand != null)
        {
            Debug.Log(
    $"检查胜利：玩家索引={playerIndex}," +$"玩家名称={GetNameByTurn(playerIndex)}," +$"手牌数量={hand.Count}");
        }
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
        consecutivePassCount=0;
        currentTurn = firstPlayer;
        lastPlayedCards = null;
        lastPlayedPlayer = -1;
        if (firstPlayer != 0)
        {
            StartCoroutine(AIPlayDelayed());
        }
        else
        {
            Debug.Log("你是地主请出牌");
        }
    }
    public void ResetState()
    {
        isRoundOver = false;
        currentTurn = 0;
        lastPlayedCards = null;
        lastPlayedPlayer = -1;
    }

/// <summary>
/// 负责清理当前这一轮牌权
/// </summary>
    private void ResetTrick()
    {
        lastPlayedPlayer=-1;
        lastPlayedCards=null;
        consecutivePassCount=0;
        //清空屏幕中央的上一手牌
        CardLayoutManager.Instance.ShowPlayArea(null);
    }

    /// <summary>
    /// 负责判断当前是不是自由出牌
    /// </summary>
    /// <returns></returns>
    private bool IsFreeTurn()
    {
        if (lastPlayedCards == null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}