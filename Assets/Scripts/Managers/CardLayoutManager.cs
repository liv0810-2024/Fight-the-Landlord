using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌布局管理器（表现层）—— 把DeckManager发好的牌数据，显示到屏幕上。
/// </summary>
public class CardLayoutManager : Singleton<CardLayoutManager>
{
    public float cardWidth = 0.7f; //每张牌宽度
    public float spacing = 0.45f; //相邻两张牌的中心距离
    private Transform myHandArea; //玩家手牌区的父节点
    public Transform bottomCardArea;
    //牌的y坐标
    public float myHandY = -0.35f;
    public float bottomCardY = 3f;

    //Ai的相关设置
    public Transform leftHandArea;
    public Transform rightHandArea;
    public float spacingY;
    public float leftHandX=-6.5f;
    public float rightHandX=6.5f;
    public float aiTotalCardLength=4f;
    protected override void Awake()
    {
        base.Awake();
        EventCenter.Instance.Register(GameEvent.Game_DealCardFinish, OnDealFinish);
        EventCenter.Instance.Register(GameEvent.Game_GrabLandlord, OnGrabLandlord);
        //动态创建父节点
        myHandArea = new GameObject("myHandArea").transform;
        bottomCardArea = new GameObject("bottomCardArea").transform;
        leftHandArea = new GameObject("leftHandArea").transform;
        rightHandArea = new GameObject("rightHandArea").transform;
    }
    /// <summary>
    /// 事件回调：发牌完成时被触发。
    /// 参数 param暂时用不到，但签名必须符合Action&lt;object&gt;。
    /// </summary>
    public void OnDealFinish(object param)
    {
        ShowMyHand();
        ShowBottomCards();
        ShowLeftHand();
        ShowRightHand();
    }
    /// <summary>
    /// 显示玩家手牌
    /// </summary>
    private void ShowMyHand()
    {
        ClearArea(myHandArea);
        LayoutCards(DeckManager.Instance.myHand, myHandArea, myHandY);
    }
    /// <summary>
    /// 显示玩家底牌
    /// </summary>
    private void ShowBottomCards()
    {
        ClearArea(bottomCardArea);
        LayoutCards(DeckManager.Instance.bottomCards, bottomCardArea, bottomCardY);
    }
    /// <summary>
    /// 玩家摆牌
    /// </summary>
    /// <param name="cards"></param>
    /// <param name="parent"></param>
    /// <param name="y"></param>
    private void LayoutCards(List<CardData> cards, Transform parent, float y)
    {
        int count = cards.Count;
        if (count == 0) return;
        //算总宽度
        float totalWidth = (count - 1) * spacing + cardWidth;
        //起始x（居中：从负半宽开始）
        float startX = -totalWidth / 2f;
        //遍历每一张牌
        for(int i = 0; i < count; i++)
        {
            // 数据 → 实体
            Card card =CardManager.Instance.CreateCard(cards[i],parent);
            // 计算这张牌的 x 坐标
            float x = startX + i * spacing;
            //设置位置
            card.transform.position=new Vector3(x,y,0);
        }
    }
    /// <summary>
    /// 【清空区域】把某个父节点下的所有牌回收进对象池。
    /// </summary>
    /// <param name="transform"></param>
    private void ClearArea(Transform parent)
    {
        for(int i = parent.childCount - 1; i >= 0; i--)
        {
            Card card=parent.GetChild(i).GetComponent<Card>();
            //有 Card 组件就回收（回收会隐藏物体、挂到对象池根节点下）
            if (card != null)
            {
                CardManager.Instance.RecycleCard(card);
            }
        }
    }
    /// <summary>
    /// 左边ai展示牌
    /// </summary>
    private void ShowLeftHand()
    {
        ClearArea(leftHandArea);
        LayoutAiCard(DeckManager.Instance.leftHand, leftHandArea, leftHandX);
    }
    /// <summary>
    /// 右边ai展示牌
    /// </summary>
    private void ShowRightHand()
    {
        ClearArea(rightHandArea);
        LayoutAiCard(DeckManager.Instance.rightHand, rightHandArea, rightHandX);
    }
    /// <summary>
    /// Ai展示牌的逻辑
    /// </summary>
    /// <param name="cardDatas"></param>
    /// <param name="parent"></param>
    /// <param name="x"></param>
    public void LayoutAiCard(List<CardData> cardDatas, Transform parent, float x)
    {
        int count = cardDatas.Count;
        if (count == 0) return;
        float spacingY = aiTotalCardLength / (count - 1);
        float totalLength = (count - 1) * spacingY + aiTotalCardLength;
        float startY = -totalLength / 2f;
        for (int i = 0; i < count; i++)
        {
            Card card = CardManager.Instance.CreateCard(cardDatas[i], parent);
            card.ShowCardBack();
            float y = startY + i * spacingY;
            card.transform.position = new Vector3(x, y, 0);
        }
    }
    /// <summary>抢地主完成后触发：重新显示所有手牌（地主此时已是20张）</summary>
    private void OnGrabLandlord(object param)
    {
        ShowMyHand();
        ShowLeftHand();
        ShowRightHand();
    }
}
