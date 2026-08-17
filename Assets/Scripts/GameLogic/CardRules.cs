using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

/// <summary>
/// 卡牌规则类：识别牌型、校验合法性、 比较大小（本教程先做识别）。
/// </summary>
public static class CardRules
{
    /// <summary>
    /// 识别一组牌的牌型。
    /// </summary>
    /// <param name="cards">要识别的牌（玩家选中的牌）</param>
    /// <returns></returns>
    public static CardType CheckCardType(List<CardData> cards)
    {
        int count = cards.Count;
        if (count == 1)
        {
            return CardType.Single;
        }
        if (count == 2)
        {
            if (IsRocket(cards))
            {
                return CardType.Rocket;
            }
            else if (IsAllSameWeight(cards)) return CardType.Pair;
        }
        if (count == 3)
        {
            if (IsAllSameWeight(cards))
            {
                return CardType.Triple;
            }
        }
        if (count == 4)
        {
            if (IsAllSameWeight(cards)) return CardType.Bomb;
        }
        //复杂牌型
        Dictionary<int, int> dict = CountByWeight(cards);
        int distinct = dict.Count; //有几种不同的点数
        //有一组是三张牌的
        if (distinct == 2 && HasCount(dict, 3))
        {
            if (count == 4) return CardType.TripleWithOne;
            if (count == 5) return CardType.TripleWithPair;
        }
        //四带二
        if (count == 6 && HasCount(dict, 4)) return CardType.FourWithTwo;
        //顺子
        if (distinct == count && IsConsecutive(dict) && count >= 5) return CardType.Straight;
        //连队
        if (count >= 6 && AllCountIs(dict, 2) && IsConsecutive(dict))
        {
            return CardType.PairStraight;
        }
        //飞机
        if (IsAirplane(dict))
        {
            return CardType.Airplane;
        }
        return CardType.None;
    }
    /// <summary>
    /// 判断一组牌是否全部同点数。
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    public static bool IsAllSameWeight(List<CardData> cards)
    {
        int firstWeight = cards[0].weight;
        for (int i = 1; i < cards.Count; i++)
        {
            if (cards[i].weight != firstWeight)
            {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// 判断两张牌是否是小王+大王
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    public static bool IsRocket(List<CardData> cards)
    {
        if (cards.Count != 2) return false;
        bool hasSmall = cards[0].rank == CardRank.SmallKing || cards[1].rank == CardRank.SmallKing;
        bool hasBig = cards[0].rank == CardRank.BigKing || cards[1].rank == CardRank.BigKing;
        return hasSmall && hasBig;
    }
    /// <summary>
    /// 统计每种点数有几张。返回字典：键=权重，值=张数。
    /// </summary>
    /// <param name="cards"></param>
    /// <returns></returns>
    private static Dictionary<int, int> CountByWeight(List<CardData> cards)
    {
        Dictionary<int, int> dict = new Dictionary<int, int>();
        foreach (CardData card in cards)
        {
            int weight = card.weight;
            if (dict.ContainsKey(weight))
            {
                dict[weight]++;
            }
            else
            {
                dict[weight] = 1;
            }
        }
        return dict;
    }
    /// <summary>判断统计表里是否存在某种点数正好出现 targetCount 次。</summary>
    private static bool HasCount(Dictionary<int, int> dict, int targetCount)
    {
        foreach (var pair in dict)
        {
            if (pair.Value == targetCount) return true;
        }
        return false;
    }
    /// <summary>判断出现的点数是否连续（相邻差1），且不含2和王。</summary>
    private static bool IsConsecutive(Dictionary<int, int> dict)
    {
        List<int> weight = new List<int>(dict.Keys);
        weight.Sort();
        if (weight[weight.Count - 1] >= 15)
        {
            return false;
        }
        for (int i = 1; i < weight.Count; i++)
        {
            if (weight[i] - weight[i - 1] != 1)
            {
                return false;
            }
        }
        return true;
    }
    ///<summary>判断统计表里所有点数都正好出现target 次。</summary>
    private static bool AllCountIs(Dictionary<int, int> dict, int target)
    {
        foreach (var pair in dict)
        {
            if (pair.Value != target) return false;
        }
        return true;
    }
    /// <summary>
    /// 判断是否飞机（带翅膀或不带翅膀）
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    private static bool IsAirplane(Dictionary<int, int> dict)
    {
        List<int> body = new List<int>(); // 主体：出现3次的点数
        // 翅膀总张数
        int wingCount = 0;
        bool wingAllSingle = true; //翅膀是否为单张
        bool wingAllPair = true; //翅膀是否为对子
        foreach (var pair in dict)
        {
            // 第一步：拆主体和翅膀
            if (pair.Value == 3)
            {
                body.Add(pair.Key);
            }
            else
            {
                wingCount += pair.Value;
                if (pair.Value!= 1)
                {
                    wingAllSingle = false;
                }
                if (pair.Value != 2)
                {
                    wingAllPair = false;
                }
            }
        }
        //第二步：主体连续 + 不含2和王
        int n = body.Count;
        if (n < 2)
        {
            return false;
        }
        body.Sort();
        for (int i = 1; i < body.Count; i++)
        {
            if (body[i] - body[i - 1] != 1)
            {
                return false;
            }
           
        }
        if (body[body.Count - 1] >= 15)
        {
            return false;
        }
        // 第三步：翅膀数量核对
        if (wingCount == 0) return true;
        if (wingAllSingle && wingCount == n)
        {
            return true;
        }
        if (wingAllPair && wingCount == 2 * n)
        {
            return true;
        }

        return false;
    }
    // <summary>判断 mine 能否压过last。last 为空表示自由出牌。</summary>
    public static bool CanBeat(List<CardData> mine,List<CardData> last)
    {
        if (last.Count == 0 || last == null) return true;
        CardType myType=CheckCardType(mine);
        CardType lastType = CheckCardType(last);
        // 王炸压一切
        if (myType==CardType.Rocket)return true;
        if (lastType == CardType.Rocket) return false;
        // 炸弹压"非炸弹"
        if (myType == CardType.Bomb && lastType != CardType.Bomb) return true;
        if (myType != CardType.Bomb && lastType == CardType.Bomb) return false;
        // 到这里：要么都是炸弹，要么都不是炸弹；牌型必须相同
        if (myType != lastType) return false;
        return GetMainWeight(mine) > GetMainWeight(last);
    }
    /// <summary>获取一组牌的"主牌权重"（决定大小的那张牌）。</summary>
    public static int GetMainWeight(List<CardData> cards)
    {
        Dictionary<int, int> dict = CountByWeight(cards);
        int maxCount = 0; //出现的最多次数
        int mainWeight = 0; //主牌权重
        foreach(var pair in dict)
        {
            if (pair.Value > maxCount||pair.Value==maxCount && pair.Key > mainWeight)
            {
                maxCount = pair.Value;
                mainWeight = pair.Key;
            }
        }
        return mainWeight;
    }
}

/// <summary>
/// 卡牌类型
/// </summary>
public enum CardType
{
    None, //无法识别
    Single, //单张
    Pair, //对子
    Triple, //三张
    TripleWithOne, //三带一
    TripleWithPair,
    Straight, //顺子
    PairStraight, //连对
    Airplane, //飞机
    Bomb, //炸弹
    FourWithTwo, //四带二
    Rocket //王炸
}
