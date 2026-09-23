using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

/// <summary>
/// 卡牌布局管理器（表现层）—— 把DeckManager发好的牌数据，显示到屏幕上。
/// </summary>
public class CardLayoutManager : Singleton<CardLayoutManager>
{
    //【重要】这是牌的"世界宽度"，不是预制体上 BoxCollider2D 里填的 2。
    //预制体的 m_LocalScale 是 0.8，碰撞体尺寸要乘上缩放才是实际宽度：2 × 0.8 = 1.6。
    public float cardWidth = 1.6f; //每张牌宽度
    //spacing 决定牌与牌重叠多少：露在外面的宽度就是 spacing，能点的也只有那么宽。
    //0.72 大约露出 45%。改小的话露出的缝隙会变窄，越来越难点准。
    public float spacing = 0.72f; //相邻两张牌的中心距离
    private Transform myHandArea; //玩家手牌区的父节点
    public Transform bottomCardArea;
    //牌的y坐标
    public float myHandY = -3.2f;
    public float bottomCardY = 2.8f;

    //Ai的相关设置
    public Transform leftHandArea;
    public Transform rightHandArea;
    public float leftHandX=-7.5f;
    public float rightHandX=7.5f;
    public float aiTotalCardLength=4f;  //AI 手牌最多铺这么长（牌多时压缩间距塞进来）
    public float aiCardSpacingY=0.28f;  //每张 AI 牌露出多少（牌少时按这个摆，长度自然变短）
    public float aiCountTextY=3.6f;     //"剩余几张"文字的 y（AI 牌最多铺到 y=3.12，别压上去）
    private TextMeshPro leftCountText;
    private TextMeshPro rightCountText;
    //玩家手牌的Card 实体列表
    private List<Card> myHandCard=new List<Card>();
    public Transform playArea; //出牌区父节点
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
        playArea = new GameObject("playArea").transform;
        // 【修复】这些区域节点是独立创建的根物体，不挂到管理器的 transform 下的话，
        // 场景切换时不会跟着 DontDestroyOnLoad 一起保留，区域会突然变空。
        myHandArea.SetParent(transform);
        bottomCardArea.SetParent(transform);
        leftHandArea.SetParent(transform);
        rightHandArea.SetParent(transform);
        playArea.SetParent(transform);
        //两侧 AI 头顶的"剩余几张"。牌叠在一起根本数不清，必须给个数字。
        leftCountText = CreateCountText("LeftAiCount", new Vector3(leftHandX, aiCountTextY, 0f));
        rightCountText = CreateCountText("RightAiCount", new Vector3(rightHandX, aiCountTextY, 0f));
    }

    /// <summary>动态建一个显示剩余张数的世界空间文字（不用改预制体）</summary>
    private TextMeshPro CreateCountText(string objName, Vector3 pos)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(transform);
        go.transform.position = pos;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        //不指定字体的话文字渲染出来是空的
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }
        else
        {
            Debug.LogWarning("[CardLayoutManager] TMP 默认字体取不到，剩余张数可能显示不出来");
        }
        tmp.fontSize = 5;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.55f);
        tmp.sortingOrder = 6000; //压在牌上面
        tmp.text = "";
        return tmp;
    }
    /// <summary>获取玩家手牌的 Card实体列表（供出牌逻辑查询）。</summary>
    public List<Card> GetMyHandCards()
    {
        return myHandCard;
    }
    /// <summary>
    /// 事件回调：发牌完成时被触发。
    /// 参数 param暂时用不到，但签名必须符合Action&lt;object&gt;。
    /// </summary>
    public void OnDealFinish(object param)
    {
        ShowMyHand();
        // 【修复】底牌不在这里显示。此刻还在抢地主，底牌是藏起来的，
        // 提前亮出来等于把牌局信息漏给玩家。移到 OnGrabLandlord（地主确定后）。
        ShowLeftHand();
        ShowRightHand();
    }
    /// <summary>
    /// 显示玩家手牌
    /// </summary>
    public void ShowMyHand()
    {
        // 不再 ClearArea：交给 LayoutCards 做增量刷新，剩下的牌才会"平移"而不是重播发牌动画
        LayoutCards(DeckManager.Instance.myHand, myHandArea, myHandY, myHandCard, CardLayer.PlayerHand);
    }
    /// <summary>
    /// 显示玩家底牌
    /// </summary>
    public void ShowBottomCards()
    {
        // 【修复】不能再读 DeckManager.bottomCards —— 底牌发给地主时那个列表已经被 Clear 了，
        // 读它只会亮出 3 张空气。改读 LandlordManager 在清空前留下的快照。
        ClearArea(bottomCardArea);
        List<CardData> bottom = LandlordManager.Instance.GetBottomCardSnapshot();
        LayoutCards(bottom, bottomCardArea, bottomCardY, null, CardLayer.BottomCard);
    }
    /// <summary>清空全部显示区域（重开新一局时调用）。</summary>
    public void ClearAllAreas()
    {
        ClearArea(myHandArea);
        myHandCard.Clear();
        ClearArea(bottomCardArea);
        ClearArea(leftHandArea);
        ClearArea(rightHandArea);
        ClearArea(playArea);
    }
    /// <summary>
    /// 摆放一组牌（增量刷新）。
    /// 已经存在的牌 → 从当前位置平移过去；新出现的牌 → 从屏幕下方飞入。
    /// </summary>
    /// <param name="cards">牌数据列表</param>
    /// <param name="parent">摆到哪个区域下</param>
    /// <param name="y">这一排的 y 坐标</param>
    /// <param name="resultList">需要记录实体的话传进来，不需要传 null</param>
    /// <param name="layer">这一排属于哪个显示区域，决定它和别的区域谁盖谁</param>
    private void LayoutCards(List<CardData> cards, Transform parent, float y, List<Card> resultList, CardLayer layer)
    {
        // 先把已经不在手牌里的牌回收掉，并拿到还留着的那些
        Dictionary<CardData, Card> existing = SyncArea(cards, parent);

        int count = cards.Count;
        if (resultList != null) resultList.Clear();
        if (count == 0) return;

        //算总宽度
        float totalWidth = (count - 1) * spacing + cardWidth;
        //起始x（居中：从负半宽开始）
        float startX = -totalWidth / 2f;

        //遍历每一张牌
        for(int i = 0; i < count; i++)
        {
            bool isExisting = existing.TryGetValue(cards[i], out Card card);
            if (!isExisting)
            {
                card = CardManager.Instance.CreateCard(cards[i], parent);
            }

            // 【关键】先告诉牌"你排第几"。它会据此设置自己的 z 和 sortingOrder，
            // 于是越靠右的牌盖在越上面 —— 玩家看见哪张，点到的就是哪张。
            card.SetHandIndex(i, layer);
            card.originalY = y;

            // 【可点区】把碰撞体缩到这张牌露在外面的那一条。
            //每张牌被右边那张盖掉一部分，只有左边 spacing 宽看得见；
            //最右那张没人盖，整张都能点。
            //不这么做的话碰撞体会伸进邻居的可点区，点邻居会命中自己。
            float visibleWidth = (i == count - 1) ? cardWidth : spacing;
            float visibleOffsetX = -cardWidth / 2f + visibleWidth / 2f; //贴着牌的左边缘对齐
            card.SetHitArea(visibleWidth, visibleOffsetX);

            // z 用牌自己算出来的层级值，不要写死 0，否则 20 张牌全挤在一个平面上
            Vector3 targetPos = new Vector3(startX + i * spacing, y, card.GetOrderZ());

            if (isExisting)
            {
                // 老牌：平移到位（这就是"出完牌剩下的牌滑过去"的效果）
                card.transform.DOMove(targetPos, 0.25f).SetEase(Ease.OutCubic);
            }
            else
            {
                // 新牌：从下方飞入，带一点错峰延迟更好看
                card.transform.position = targetPos + new Vector3(0f, -8f, 0f);
                card.transform.DOMove(targetPos, 0.4f).SetEase(Ease.OutCubic).SetDelay(i * 0.03f);
            }

            if(resultList!=null)
            {
                resultList.Add(card);
            }
        }
    }

    /// <summary>
    /// 让区域里的 Card 实体和最新数据对齐：
    /// 已经不在列表里的牌回收掉，还留着的记进字典返回。
    /// </summary>
    private Dictionary<CardData, Card> SyncArea(List<CardData> cards, Transform parent)
    {
        Dictionary<CardData, Card> existing = new Dictionary<CardData, Card>();
        // 倒序遍历：回收会改变 childCount，从后往前删不影响前面的下标
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Card card = parent.GetChild(i).GetComponent<Card>();
            if (card == null) continue;

            if (card.cardData == null || !cards.Contains(card.cardData))
            {
                // 手牌里已经没这张了（被打出去了）→ 回收
                CardManager.Instance.RecycleCard(card);
            }
            else
            {
                existing[card.cardData] = card;
            }
        }
        return existing;
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
     public void ShowLeftHand()
    {
        LayoutAiCard(DeckManager.Instance.leftHand, leftHandArea, leftHandX, leftCountText);
    }
    /// <summary>
    /// 右边ai展示牌
    /// </summary>
    public void ShowRightHand()
    {
        LayoutAiCard(DeckManager.Instance.rightHand, rightHandArea, rightHandX, rightCountText);
    }
    /// <summary>
    /// Ai展示牌的逻辑
    /// </summary>
    /// <param name="cardDatas"></param>
    /// <param name="parent"></param>
    /// <param name="x"></param>
    public void LayoutAiCard(List<CardData> cardDatas, Transform parent, float x, TextMeshPro countText)
    {
        // 同样做增量刷新：只回收"已经打出去"的牌，剩下的复用
        Dictionary<CardData, Card> existing = SyncArea(cardDatas, parent);

        int count = cardDatas.Count;
        //先更新剩余张数：牌打光时下面会 return，不能把这一步漏掉
        if (countText != null)
        {
            countText.text = count.ToString();
            //剩 2 张以内变红，提醒玩家"对手快赢了"
            countText.color = (count <= 2) ? new Color(1f, 0.45f, 0.35f) : new Color(1f, 0.92f, 0.55f);
        }
        // 【修复】原来是 count <= 1 就 return，导致 AI 只剩 1 张牌时屏幕上直接不显示了，
        // 玩家看不到对手还剩几张。
        if (count == 0) return;
        // 【间距】牌多时压缩间距塞进 aiTotalCardLength；牌少时按 aiCardSpacingY 摆，
        // 于是整排长度跟着张数一起变短 —— 否则剩 3 张和剩 20 张看起来一样长，看不出打掉了多少。
        float spacingY = (count > 1) ? Mathf.Min(aiCardSpacingY, aiTotalCardLength / (count - 1)) : 0f;
        float startY = -(count - 1) * spacingY / 2f; //整排在竖直方向居中
        for (int i = 0; i < count; i++)
        {
            bool isExisting = existing.TryGetValue(cardDatas[i], out Card card);
            if (!isExisting)
            {
                card = CardManager.Instance.CreateCard(cardDatas[i], parent);
                card.ShowCardBack();
            }

            // AI 的牌同样要定层级：它们叠得比手牌还狠，
            // 不定层级的话 sortingOrder 全是 0，和玩家手牌撞在一起会闪。
            card.SetHandIndex(i, CardLayer.AiHand);

            float y = startY + i * spacingY;
            card.transform.position = new Vector3(x, y, card.GetOrderZ());
        }
    }
    /// <summary>抢地主完成后触发：重新显示所有手牌（地主此时已是20张）</summary>
    private void OnGrabLandlord(object param)
    {
        ShowMyHand();
        ShowLeftHand();
        ShowRightHand();
        // 地主已经确定，这时候才把底牌亮出来
        ShowBottomCards();
    }
    /// <summary>把打出的牌显示到屏幕中央的出牌区。</summary>
    public void ShowPlayArea(List<CardData> cards)
    {
        ClearArea(playArea);
        if(cards==null||cards.Count == 0) return;
        //居中水平排列
        float totalWidth = (cards.Count - 1) * spacing + cardWidth;
        float startX = -totalWidth / 2f;
        for(int i=0;i<cards.Count;i++)
        {
            Card card = CardManager.Instance.CreateCard(cards[i], playArea);
            card.SetHandIndex(i, CardLayer.PlayArea); //出牌区层级最高，压在所有区域之上
            float x = startX + i * spacing;
            Vector3 targetPos = new Vector3(x, 0, card.GetOrderZ());
            card.transform.position = targetPos + new Vector3(0, -2f, 0);
            card.transform.DOMove(targetPos, 0.3f).SetEase(Ease.OutBack);
        }
    }

}
