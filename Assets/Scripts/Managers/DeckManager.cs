using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 牌堆管理器 ——负责整副牌的生成、洗牌、发牌。
/// </summary>
public class DeckManager : Singleton<DeckManager>
{
    //手牌
    public List<CardData> myHand;
    public List<CardData> leftHand;
    public List<CardData> rightHand;

    private List<CardData> deck;
    public List<CardData> bottomCards;

    public bool isDealt { get;private set;  } //是否已经发过牌

    protected override void Awake()
    {
        base.Awake();
        myHand = new List<CardData>();
        leftHand = new List<CardData>();
        rightHand = new List<CardData>();
        bottomCards = new List<CardData>();
        deck = new List<CardData>();
    }
    /// <summary>
    /// 【发牌总流程】按顺序执行 4步：生成 → 洗牌 → 发牌 → 排序。
    /// </summary>
    public void DealCards()
    {
        //防御：数据没加载完就不发牌（时序保护）
        if (!DataManager.Instance.isDataLoaded)
        {
            Debug.LogError("[DeckManager] 卡牌数据还没有加载完，不能发牌");
            return;
        }
        CreateDeck();
        Shuffle(deck); //洗牌
        DealToPlayers(deck); //发牌
        SortAllHands();
        isDealt = true;
        Debug.Log("发牌完成");
        EventCenter.Instance.Trigger(GameEvent.Game_DealCardFinish);
    }
    /// <summary>
    /// 生成牌
    /// </summary>
    private void CreateDeck()
    {
        deck.Clear();
        //AddRange:把 DataManager 里存的所有牌，整批复制一份，塞进 deck 里
        deck.AddRange(DataManager.Instance.AllCards); //拿牌
    }
    /// <summary>
    /// 洗牌
    /// </summary>
    private void Shuffle(List<CardData> cardDatas)
    {
        for(int i=cardDatas.Count-1; i > 0; i--)
        {
            int j=Random.Range(0, i+1);
            CardData temp = cardDatas[i];
            cardDatas[i] = cardDatas[j];
            cardDatas[j] = temp;
        }
    }
    /// <summary>
    /// 发牌
    /// </summary>
    private void DealToPlayers(List<CardData> cards)
    {
        //先清空，防止重复发牌时数据叠加
        myHand.Clear();
        leftHand.Clear();
        rightHand.Clear();
        bottomCards.Clear();
        //一人一张来
        for (int i = 0; i < 51; i++)
        {
            if (i % 3 == 0)
            {
                myHand.Add(cards[i]);
            }else if(i % 3 == 1)
            {
                leftHand.Add(cards[i]);
            }
            else
            {
                rightHand.Add(cards[i]);
            }
        }
        for(int i = 51; i < 54; i++)
        {
            bottomCards.Add(cards[i]); //底牌
        }
    }
    /// <summary>
    /// 手排顺序
    /// </summary>
    public void SortAllHands()
    {
        //这里排的是CardData（纯数据），不是 Card 实体。
        myHand.Sort((a, b) => a.weight.CompareTo(b.weight));
        leftHand.Sort((a, b) => a.weight.CompareTo(b.weight));
        rightHand.Sort((a, b) => a.weight.CompareTo(b.weight));
        bottomCards.Sort((a,b)=>a.weight.CompareTo(b.weight));
    }
}
