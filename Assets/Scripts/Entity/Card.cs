using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;

/// <summary>
/// 卡牌所在的显示区域。不同区域互相挨着，必须把渲染层级和 z 分开放，
/// 否则 20 张手牌铺满屏幕时会和左右 AI 的牌撞在同一层级上闪烁。
/// 序号越大越靠前（盖在越上面）。
/// </summary>
public enum CardLayer
{
    AiHand = 0,      //对手手牌（两侧）
    BottomCard = 1,  //底牌（上方）
    PlayerHand = 2,  //玩家手牌（下方，主要操作区）
    PlayArea = 3,    //出牌区（屏幕正中，永远在最上面）
}

public class Card : MonoBehaviour
{
    //用 [HideInInspector] 让它在 Inspector 面板隐藏
    [HideInInspector] public CardData cardData;
    private SpriteRenderer spriteRenderer; //用于显示卡牌画面的组件引用
    [HideInInspector] public bool isSelected = false; //是否被选中
    [HideInInspector] public bool isPlayOut = false; //是否已经出牌
    [HideInInspector] public int handIndex; //这张牌在整排里的位置（0=最左），决定它的层级
    [Header("原始Y坐标")]
    public float originalY; //卡牌未选中时的原始 Y坐标
    public const float SELECT_OFFSET_Y = 0.3f; //选中时往上抬多少
    private TextMeshPro rankText; // ← 新增：牌面数字+花色的文字组件
    private MeshRenderer rankTextRenderer; //TMP 是靠 MeshRenderer 渲染的，层级要跟 Sprite 一起改
    private BoxCollider2D boxCol; //出牌区/底牌/AI 的牌要关掉它，否则会挡住手牌的点击

    //【缩放基准】预制体上的 localScale 是 0.8，不是 1。
    //所有缩放都必须"以它为基准乘一个倍数"，直接写 1f/1.1f 会让牌回不到原样。
    private Vector3 baseScale = Vector3.one;

    //【层级规则】手牌会互相重叠，必须让"看得见的那张"和"点得到的那张"是同一张。
    //越靠右的牌 z 越小（越贴近相机）+ sortingOrder 越大 → 视觉上盖在上面，
    //点击重叠区域时物理射线也优先命中它。
    private const float Z_STEP = 0.01f;      //同一排内每张牌之间的 z 差
    private const float Z_LAYER_STEP = 0.2f; //不同区域之间的 z 差
    private const float Z_SELECTED = -1f;    //选中的牌直接提到所有牌前面
    private const int ORDER_PER_LAYER = 100; //每个区域独占 100 个 sortingOrder 名额
    //【层级分区】0~999 留给牌面，1000 以上留给文字。
    //选中的牌取 900：比所有未选中的牌面高（最多 300+），又低于文字区，不会盖掉别人的花色。
    private const int ORDER_SELECTED = 900;
    private const int ORDER_TEXT_BASE = 1000; //文字区起点，永远压在牌面之上

    private CardLayer layer = CardLayer.PlayerHand;

    //【悬停】鼠标贴上就浮起来，让人一眼看清正要选哪张
    private const float HOVER_OFFSET_Y = 0.2f;
    private bool isHovered;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        boxCol = GetComponent<BoxCollider2D>();
        // 趁还没被任何动画改过，把预制体上的原始缩放记下来（Awake 对每个实例只跑一次）
        baseScale = transform.localScale;
        rankText = GetComponentInChildren<TextMeshPro>();
        if(rankText == null)
        {
            Debug.LogWarning("Card：找不到子物体RankText 的TextMeshPro，牌面文字无法显示");
        }
        else
        {
            rankTextRenderer = rankText.GetComponent<MeshRenderer>();
        }
    }
    /// <summary>
    /// 初始化卡牌
    /// </summary>
    /// <param name="data"></param>
    public void InitCard(CardData data)
    {
        // 【修复】从对象池取出复用的牌，可能还残留着上一次的 DOTween 动画。
        // 不杀掉的话，动画会在下一帧把这张牌拽到旧位置，出现"牌乱飞"。
        transform.DOKill();
        cardData = data;
        isSelected = false;
        isPlayOut = false;
        isHovered = false;
        handIndex = 0;
        layer = CardLayer.PlayerHand;
        UpdateCardDisplay();
        RefreshSorting();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 【布局层调用】告诉这张牌"你在这一排里排第几"。
    /// 排得越靠右，它就越靠前（z 越小、sortingOrder 越大），
    /// 于是视觉上盖住左边那张，点击重叠区时也优先命中它。
    /// </summary>
    public void SetHandIndex(int index, CardLayer inLayer = CardLayer.PlayerHand)
    {
        handIndex = index;
        layer = inLayer;
        //z 直接赋值不走动画：位置动画统一由布局层负责，这里只定层级
        Vector3 pos = transform.position;
        pos.z = GetOrderZ();
        transform.position = pos;
        // 只有玩家手牌需要响应鼠标。出牌区、底牌、AI 的牌统统关掉碰撞体：
        // 既不会误触，射线还能穿透过去命中它们身后的手牌。
        if (boxCol != null) boxCol.enabled = (inLayer == CardLayer.PlayerHand);
        RefreshSorting();
    }

    /// <summary>
    /// 【布局层调用】把碰撞体缩到这张牌"露在外面的那一条"。
    /// 手牌互相重叠，每张牌只有左边一小条是看得见的。碰撞体如果保持整张宽，
    /// 就会伸进邻居的可点区 —— 点邻居反而命中自己（选中牌上浮后尤其明显）。
    /// 参数传世界坐标下的宽度和水平偏移（相对牌中心），内部折算成局部值。
    /// </summary>
    public void SetHitArea(float worldWidth, float worldOffsetX)
    {
        if (boxCol == null) return;
        //用 baseScale 而不是实时 localScale：牌可能正在播放缩放动画，
        //实时值会跟着动画变，碰撞体就会跟着抖。
        float s = Mathf.Max(0.0001f, Mathf.Abs(baseScale.x));
        boxCol.size = new Vector2(worldWidth / s, boxCol.size.y);
        boxCol.offset = new Vector2(worldOffsetX / s, boxCol.offset.y);
    }

    /// <summary>这张牌现在能不能被鼠标操作（出牌区/底牌/AI 的牌都不行）</summary>
    private bool CanInteract
    {
        get { return !isPlayOut && cardData != null && layer == CardLayer.PlayerHand; }
    }

    /// <summary>
    /// 这张牌当前应该在的 z。
    /// 选中时整体跳到最前（Z_SELECTED），但选中牌彼此之间仍保留 handIndex 的先后，
    /// 否则一次选中好几张时它们会挤在同一层互相闪。
    /// </summary>
    public float GetOrderZ()
    {
        float baseZ = isSelected ? Z_SELECTED : -(int)layer * Z_LAYER_STEP;
        return baseZ - handIndex * Z_STEP;
    }

    /// <summary>
    /// 把 Sprite 和牌面文字的渲染层级刷新成当前该有的顺序。
    ///
    /// 【为什么要分成两个区间】牌是矩形，选中上浮后必然会盖到右边那张牌。
    /// 如果牌面和文字混在一个区间里排序，选中牌的"牌面"就会连邻居的"花色"一起盖掉 ——
    /// 表现为点一张牌，它右边那张的花色消失。
    /// 把文字整体抬到牌面之上（1000 起步），选中牌就只挡牌面、不挡字。
    /// </summary>
    private void RefreshSorting()
    {
        int surfaceOrder = (isSelected ? ORDER_SELECTED : (int)layer * ORDER_PER_LAYER) + handIndex * 2;
        if (spriteRenderer != null) spriteRenderer.sortingOrder = surfaceOrder;
        if (rankTextRenderer != null) rankTextRenderer.sortingOrder = ORDER_TEXT_BASE + surfaceOrder;
    }
    /// <summary>
    /// 刷新卡牌
    /// </summary>
    public void UpdateCardDisplay()
    {
        if (cardData == null)
        {
            Debug.LogWarning("Card.UpdateCardDisplay: cardData 为空，无法更新显示");
            return;
        }
        // ----- 第一步：根据花色设置颜色 -----
        spriteRenderer.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        if (rankText != null)
        {
            rankText.text =GetDisplayText();   // 内容：如"A♠"、"10♥"、"小王"
            rankText.color = GetSuitColor();
        }
        gameObject.name=GetSuitSymbol()+GetRankDisplayText();
        //旧版
        //switch (cardData.suit)
        //{
        //    case CardSuit.Heart:
        //    case CardSuit.Diamond:
        //        // (1, 0.3, 0.3) 是柔和的红色
        //        spriteRenderer.color = new Color(1f, 0.3f, 0.3f);
        //        break;
        //    case CardSuit.Spade:
        //    case CardSuit.Club:
        //        spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f);
        //        break;
        //    case CardSuit.Nome:
        //        spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
        //        break;
        //}
        // ----- 第二步：获取点数显示文字 ----
        // 调用私有方法，把枚举转成可读文字
        //string rankText = GetRankDisplayText();
        //string suitSymbolText = GetSuitSymbol();
        //gameObject.name = suitSymbolText + rankText;
    }
    /// <summary>
    /// 【点数→文字】把CardRank 枚举值转成玩家看得懂的文字。
    /// </summary>
    /// <returns></returns>
    public string GetRankDisplayText()
    {
        switch (cardData.rank)
        {
            case CardRank.Three:
                return "3";
            case CardRank.Four:
                return "4";
            case CardRank.Five:
                return "5";
            case CardRank.Six:
                return "6";
            case CardRank.Seven:
                return "7";
            case CardRank.Eight:
                return "8";
            case CardRank.Nine:
                return "9";
            case CardRank.Ten:
                return "10";
            case CardRank.J:
                return "J";
            case CardRank.Q:
                return "Q";
            case CardRank.K:
                return "K";
            case CardRank.A:
                return "A";
            case CardRank.Two:
                return "2";
            case CardRank.SmallKing:
                return "小王";
            case CardRank.BigKing:
                return "大王";
            default:
                return "?";
        }
    }
    /// <summary>
    /// summary>
    /// 【花色→符号】把CardSuit 枚举值转成Unicode扑克牌花色符号。
    /// </summary>
    /// <returns></returns>
    public string GetSuitSymbol()
    {
        switch (cardData.suit)
        {
            case CardSuit.Spade:
                return "♠";
            case CardSuit.Heart:
                return "♥";
            case CardSuit.Club:
                return "♣";
            case CardSuit.Diamond:
                return "♦";
            case CardSuit.Nome:
                return "";
            default:
                return "?";
        }
    }
    /// <summary>
    /// 【获取权重】返回卡牌的权重值，用于排序和比较大小时直接比较
    /// </summary>
    /// <returns></returns>
    public int GetWeight()
    {
        if (cardData == null)
        {
            Debug.LogError("Card.GetWeight被调用,但是cardData为空");
            return 0;
        }
        return cardData.weight;
    }
    /// <summary>
    /// 【切换选中】点一下选中，再点一下取消
    /// </summary>
    public void ToggleSelect()
    {
        if (isSelected)
        {
            SetSelected(false);
        }
        else
        {
            SetSelected(true);
        }
    }
    /// <summary>
    /// 【设置选中状态】真正执行选中或取消的动作。
    /// </summary>
    /// <param name="selected"></param>
    public void SetSelected(bool selected)
    {
        SetSelected(selected, true);
    }
    /// <summary>
    /// 设置选中状态。
    /// </summary>
    /// <param name="selected">是否选中</param>
    /// <param name="raiseEvent">是否广播 Card_Select 事件。批量选牌时传 false，避免一次响好几声。</param>
    public void SetSelected(bool selected, bool raiseEvent)
    {
        isSelected = selected;
        isHovered = false; //选中状态接管位置，悬停不再插手
        // 目标y：选中则上浮，未选中则回到原始位置
        float targetY = selected ? originalY + SELECT_OFFSET_Y : originalY;

        //【层级】选中的牌提到最前，否则会被右边没选的牌压住半截
        RefreshSorting();
        //【只上浮，不放大】放大能让牌变宽，反而更多侵入左右邻居的区域；
        //而且"往上一走"这个反馈已经足够清楚了。
        //用 DOMove 而不是 DOMoveY：z 要跟着层级一起走
        transform.DOMove(new Vector3(transform.position.x, targetY, GetOrderZ()), 0.15f)
                 .SetEase(Ease.InOutQuad);
        if (!raiseEvent) return;
        //旧版本
        //Vector3 newPos=gameObject.transform.position;
        //if (isSelected)
        //{
        //    newPos.y = originalY + SELECT_OFFSET_Y;
        //    transform.position = newPos;
        //    transform.localScale=new Vector3(1.1f, 1.1f, 1f);
        //}
        //else
        //{
        //    newPos.y = originalY;
        //    transform.position = newPos;
        //    transform.localScale = Vector3.one;
        //}
        EventCenter.Instance.Trigger(GameEvent.Card_Select, this);
    }
    /// <summary>
    /// 显示卡牌背面（AI 的牌用）。
    /// </summary>
    public void ShowCardBack()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.25f, 0.35f, 0.7f);
        }
        // 【修复】原来只改了底色，牌面文字还亮着 —— AI 的手牌点数全被玩家看见了。
        // 显示背面时把点数文字清空。
        if (rankText != null)
        {
            rankText.text = "";
        }
        gameObject.name = "背面";
    }
    /// <summary>
    /// 回收卡牌
    /// </summary>
    public void ResetCard()
    {
        // 【修复】回收时要杀掉正在跑的 DOTween，并复位位置/旋转。
        // 否则牌被池子复用后会带着上一次的动画状态，出现位置错乱。
        transform.DOKill();
        cardData = null;
        isPlayOut = false;
        isSelected = false;
        //物体被 SetActive(false) 时 Unity 不会补发 OnMouseExit，
        //不清掉这个标记的话，下次复用时牌会一直以为鼠标还在自己身上。
        isHovered = false;
        handIndex = 0;
        layer = CardLayer.PlayerHand;
        originalY = 0f;
        gameObject.name = "Card";
        // 同样要回到 baseScale 而不是 Vector3.one，否则池子复用出来的牌会比新牌大一圈
        transform.localScale = baseScale;
        transform.localRotation = Quaternion.identity;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 0;
        }
        if (rankText != null) rankText.text = "";
        if (rankTextRenderer != null) rankTextRenderer.sortingOrder = 0;
        gameObject.SetActive(false);
    }
    /// <summary>
    /// 点击卡牌响应
    /// </summary>
    public void OnMouseDown()
    {
        if (!CanInteract) return;
        ToggleSelect();
    }

    /// <summary>
    /// 【悬停浮起】鼠标贴上就抬起来一点，让人一眼看清"现在点的是哪张"。
    /// 手牌重叠时这个反馈特别重要 —— 不用真的点下去就能确认。
    /// </summary>
    private void OnMouseEnter()
    {
        if (!CanInteract || isSelected) return;
        isHovered = true;
        MoveTo(originalY + HOVER_OFFSET_Y, 0.1f);
    }

    private void OnMouseExit()
    {
        if (!isHovered) return;
        isHovered = false;
        //已经选中的牌不能因为鼠标移开就掉下去
        if (isSelected || isPlayOut) return;
        MoveTo(originalY, 0.1f);
    }

    /// <summary>保持 x、z 不动，只把 y 移过去（统一走 DOMove，避免和别的 tween 抢轴）</summary>
    private void MoveTo(float y, float duration)
    {
        Vector3 target = new Vector3(transform.position.x, y, transform.position.z);
        transform.DOMove(target, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 【拼牌面文字】数字 + 花色；大小王没有花色，只显示"小王/大王"。
      /// </summary>
    private string GetDisplayText()
    {
        if (cardData.suit == CardSuit.Nome)
        {
            return GetRankDisplayText();
        }
        return GetRankDisplayText()+GetSuitSymbol();
    }


    /// <summary>
    /// 【花色→颜色】红桃♥、方块♦红色；黑桃♠、梅花♣黑色；大王红、小王黑。
      /// </summary>
    private Color GetSuitColor()
    {
        switch (cardData.suit) 
        {
            case CardSuit.Heart:
            case CardSuit.Diamond:
                return new Color(0.85f, 0.2f, 0.2f, 1f);
            case CardSuit.Spade:
            case CardSuit.Club:
                return new Color(0.15f, 0.15f, 0.15f, 1f);
            case CardSuit.Nome:
                return cardData.rank == CardRank.BigKing ? new Color(0.85f, 0.2f, 0.2f, 1f) : new Color(0.15f, 0.15f, 0.15f, 1f);
            default:
                return Color.black;
        }
    }
}