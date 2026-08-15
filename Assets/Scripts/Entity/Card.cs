using UnityEngine;

public class Card : MonoBehaviour
{
    //用 [HideInInspector] 让它在 Inspector 面板隐藏
    [HideInInspector] public CardData cardData;
    private SpriteRenderer spriteRenderer; //用于显示卡牌画面的组件引用
    [HideInInspector] public bool isSelected = false; //是否被选中
    [HideInInspector] public bool isPlayOut = false; //是否已经出牌
    [Header("原始Y坐标")]
    public float originalY; //卡牌未选中时的原始 Y坐标
    public const float SELECT_OFFSET_Y = 0.3f;
    public const float SELECT_SCALE = 1.1f; //放大百分之十

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
    /// <summary>
    /// 初始化卡牌
    /// </summary>
    /// <param name="data"></param>
    public void InitCard(CardData data)
    {
        cardData = data;
        isSelected = false;
        isPlayOut = false;
        originalY = transform.position.y;
        UpdateCardDisplay();
        gameObject.SetActive(true);
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
        switch (cardData.suit)
        {
            case CardSuit.Heart:
            case CardSuit.Diamond:
                // (1, 0.3, 0.3) 是柔和的红色
                spriteRenderer.color = new Color(1f, 0.3f, 0.3f);
                break;
            case CardSuit.Spade:
            case CardSuit.Club:
                spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f);
                break;
            case CardSuit.Nome:
                spriteRenderer.color = new Color(1f, 0.2f, 0.2f);
                break;
        }
        // ----- 第二步：获取点数显示文字 ----
        // 调用私有方法，把枚举转成可读文字
        string rankText = GetRankDisplayText();
        string suitSymbolText = GetSuitSymbol();
        gameObject.name = suitSymbolText + rankText;
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
        isSelected = selected;
        Vector3 newPos=gameObject.transform.position;
        if (isSelected)
        {
            newPos.y = originalY + SELECT_OFFSET_Y;
            transform.position = newPos;
            transform.localScale=new Vector3(1.1f, 1.1f, 1f);
        }
        else
        {
            newPos.y = originalY;
            transform.position = newPos;
            transform.localScale = Vector3.one;
        }
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
            gameObject.name = "背面";
        }
    }
    /// <summary>
    /// 回收卡牌
    /// </summary>
    public void ResetCard()
    {
        cardData = null;
        isPlayOut = false;
        isSelected = false;
        gameObject.name = "Card";
        transform.localScale = Vector3.one;
        spriteRenderer.color = Color.white;
        gameObject.SetActive(false);
    }
    /// <summary>
    /// 点击卡牌响应
    /// </summary>
    public void OnMouseDown()
    {
        if (isPlayOut) return;
        ToggleSelect();
    }
}