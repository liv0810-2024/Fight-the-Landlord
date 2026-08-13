using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 比较权重工具类
/// </summary>
//这个类实现了一个叫 IComparer<Card>的接口，接口要求你必须写一个 Compare方法，专门用来回答"a 和 b 谁大"。
public class CardWeightComparer : IComparer<Card>
{
    public int Compare(Card a, Card b)
    {
        int result = a.GetWeight().CompareTo(b.GetWeight());
        //大小相同比花色
        if (result == 0)
        {
            result = a.cardData.suit.CompareTo(b.cardData.suit);
        }
        return result;
    }
}

public static class CardUtils 
{
    public static void SortHandCard(List<Card> cards)
    {
        //传入一个实现了 IComparer<T> 的对象的比较器
        cards.Sort(new CardWeightComparer());
    }
    /// <summary>
    ///单张排比较大小
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static int CompareCard(Card a,Card b)
    {
        if (a == null || b == null)
        {
            Debug.LogError(" CardUtils.CompareCard 传入的卡牌为空");
            return 0;
        }
        return a.GetWeight().CompareTo(b.GetWeight());
    }
}
