using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌生成管理器
/// </summary>
public class CardManager : Singleton<CardManager>
{
    /// <summary>
    /// 预制体
    /// </summary>
    private GameObject cardPrefab;
    private const string CARD_POOL_NAME = "CardPool"; //对象池名字
    public const int PREWARM_COUNT = 54; //预加载卡牌数量

    protected override void Awake()
    {
        base.Awake();
        // 从 Resources 加载卡牌预制体
        cardPrefab = ResManager.Instance.Load<GameObject>("Prefabs/Card/Card");
        if (cardPrefab == null)
        {
            Debug.Log("加载卡牌预制体失败！请检查Resources/Prefabs/Card/Card 是否存在");
            return;
        }
        //预加载池子
        ObjectPoolManager.Instance.Prewarm(CARD_POOL_NAME, cardPrefab, PREWARM_COUNT);
        Debug.Log("卡牌管理器卡牌管理器初始化完成，已预热" + PREWARM_COUNT + "张牌");
    }
    /// <summary>
    /// 生成一张牌。
    /// </summary>
    /// <returns></returns>
    public Card CreateCard(CardData data,Transform parent=null)
    {
        GameObject obj = ObjectPoolManager.Instance.GetGameObject(CARD_POOL_NAME, cardPrefab, parent);
        Card card1=obj.GetComponent<Card>();
        if(card1 == null)
        {
            Debug.Log("卡牌预制体上少了Card的组件");
            return null;
        }
        card1.InitCard(data);
        return card1;
    }
    /// <summary>
    /// 回收一些牌
    /// </summary>
    /// <param name="card"></param>
    public void RecycleCard(Card card)
    {
        if (card == null)
        {
            Debug.Log("[CardManager]RecycleCard传入了null");
            return;
        }
        card.ResetCard();
        //②还回对象池（Recycle 内部会SetActive(false) 并挂到 PoolRoot 下）
        ObjectPoolManager.Instance.Recycle(CARD_POOL_NAME, card.gameObject);
    }
}
