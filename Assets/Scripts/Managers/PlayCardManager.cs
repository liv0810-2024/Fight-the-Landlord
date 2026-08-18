using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>出牌管理器：负责收集选牌、校验、出牌、轮转。</summary>
public class PlayCardManager : Singleton<PlayCardManager>
{
    private int currentTurn = 0; //当前行动玩家 我是0
    private List<CardData> lastPlayedCards; //上一手出的牌（用于压牌比较）
    private Transform playArea;
    protected override void Awake()
    {
        base.Awake();
        playArea = new GameObject("playArea").transform;
    }
    private void Update()
    {
        //临时空格出牌
        if(Input.GetKeyDown(KeyCode.Space))
        {
            PlayerPlayCards();
        }
    }
    /// <summary>从手牌里收集所有被选中的牌，转成 CardData 列表。</summary>
    private List<CardData> CollectSelectedCards()
    {
        List<CardData> selectedData=new List<CardData>();
        foreach(Card card in CardLayoutManager.Instance.GetMyHandCards())
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
        if (currentTurn != 0) return;
        List<CardData> selected = CollectSelectedCards();
        //没选牌
        if (selected.Count == 0)
        {
            Debug.Log("请先选牌！");
            return;
        }
        //牌型无效
        if (CardRules.CheckCardType(selected) == CardType.None)
        {
            Debug.Log("这组牌不是合法牌型！");
            return;
        }
        //压牌校验
        if (!CardRules.CanBeat(selected, lastPlayedCards))
        {
            Debug.Log("压不过上家的牌！");
            return;
        }
        //真正出牌
        DoPlayCards(selected);
    }
    private void DoPlayCards(List<CardData> played)
    {
        List<CardData> myHand = DeckManager.Instance.myHand;
        //数据层
        foreach(CardData cardData in played)
        {
            myHand.Remove(cardData);
        }
        lastPlayedCards = played;
        CardLayoutManager.Instance.ShowMyHand(); //表现层刷新
        currentTurn = 1;
        //暂时只轮到左AI，AI 逻辑教程12做
        Debug.Log("玩家出了 " +played.Count + " 张牌，轮到左AI");
    }
}
