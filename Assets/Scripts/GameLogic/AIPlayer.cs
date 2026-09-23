using System.Collections.Generic;

/// <summary>
/// AI 玩家决策类 —— 负责根据手牌和场上局势，决定出什么牌。
/// </summary>
public class AIPlayer
{
    /// <summary>
    /// AI 出牌决策入口。
    /// </summary>
    /// <param name="handCards">AI 当前手牌</param>
    /// <param name="lastPlayedCards">上一手出的牌</param>
    /// <param name="lastPlayedPlayer">上一手是谁出的（如果是自己 = 自由出牌）</param>
    /// <param name="myIndex">AI 自己的索引 (1 或 2)</param>
    /// <returns></returns>
    public List<CardData> Decide(List<CardData> handCards, List<CardData> lastPlayedCards, int lastPlayedPlayer, int myIndex)
    {
        if (handCards == null || handCards.Count == 0)
        {
            return null;
        }
        // 判断是否自由出牌（上一手为空，或者上一手是自己出的）
        bool isFree = lastPlayedCards == null || lastPlayedCards.Count == 0 || lastPlayedPlayer == myIndex;
        // 按权重分组（核心数据结构，后续所有搜索都基于它）
        Dictionary<int, List<CardData>> weightGroups = GroupByWeight(handCards);
        //出牌策略
        if (isFree)
        {
            // === 自由出牌：按策略选择最优牌型 ===
            return FindBestFreePlay(weightGroups);
        }
        else
        {
            // === 跟牌：找同类型最小能压过的牌 ===
            CardType lastType = CardRules.CheckCardType(lastPlayedCards);
            int lastMainWeight = CardRules.GetMainWeight(lastPlayedCards);
            return FindBeatingPlay(weightGroups, lastType, lastMainWeight,lastPlayedCards.Count);
        }


    }
    #region 数据分组

    /// <summary>
    /// 按 weight 把手牌分组。
    /// </summary>
    /// <param name="hand"></param>
    /// <returns></returns>
    private Dictionary<int, List<CardData>> GroupByWeight(List<CardData> hand)
    {
        Dictionary<int, List<CardData>> groups = new Dictionary<int, List<CardData>>();
        foreach (CardData card in hand)
        {
            if (!groups.ContainsKey(card.weight))
            {
                groups[card.weight] = new List<CardData>();
            }
            // 【修复】不管是不是刚新建的 List，都要把这张牌放进去。
            // 原来写在 else 里，导致每种点数的第一张牌被丢掉，
            // 分组结果永远比手牌少 1 张（每个点数各少 1 张）。
            groups[card.weight].Add(card);
        }
        return groups;
    }
    #endregion 

    #region 自由出牌策略
    /// <summary>
    /// 找最优自由出牌
    /// </summary>
    /// <param name="cardDatas"></param>
   private List<CardData> FindBestFreePlay(Dictionary<int, List<CardData>> weightGroups)
    {
        // 【策略核心】自由出牌的目标是"拆散手牌"，而不是"拆散牌型"。
        // 所以优先出「孤张单牌」和「纯对子」—— 这两种牌出了不会破坏其它牌型。
        // 原来第一步用 FindSmallestSingle，会把 AAA 拆成一张单牌打出去。

        // 1. 孤张单牌（该点数只有 1 张；排除 2 和王，留作控场）
        List<CardData> single = FindSmallestLoneSingle(weightGroups, maxWeight: 14);
        if (single != null) return single;

        // 2. 纯对子（该点数正好 2 张，不拆三张、不拆炸弹）
        List<CardData> pair = FindSmallestPurePair(weightGroups, maxWeight: 14);
        if (pair != null) return pair;

        // 3. 三带一（优先带单张，省下对子）
        List<CardData> tripleWithOne = FindSmallestTripleWithOne(weightGroups, maxMainWeight: 14);
        if (tripleWithOne != null) return tripleWithOne;

        // 4. 三带二
        List<CardData> tripleWithPair = FindSmallestTripleWithPair(weightGroups, maxMainWeight: 14);
        if (tripleWithPair != null) return tripleWithPair;

        // 5. 顺子（优先最短的，即5张）
        List<CardData> straight = FindShortestStraight(weightGroups);
        if (straight != null) return straight;

        // 6. 连对（优先最短的，即3连对=6张）
        List<CardData> pairStraight = FindShortestPairStraight(weightGroups);
        if (pairStraight != null) return pairStraight;

        // 7. 飞机带翅膀
        List<CardData> airplane = FindShortestAirplane(weightGroups);
        if (airplane != null) return airplane;

        // 8. 实在没牌了，出单张 2
        single = FindSmallestSingle(weightGroups, minWeight: 15, maxWeight: 15);
        if (single != null) return single;

        // 9. 小王
        single = FindSmallestSingle(weightGroups, minWeight: 16, maxWeight: 16);
        if (single != null) return single;

        // 10. 大王
        single = FindSmallestSingle(weightGroups, minWeight: 17, maxWeight: 17);
        if (single != null) return single;

        // 11. 只剩炸弹了，出最小的炸弹
        List<CardData> bomb = FindSmallestBomb(weightGroups);
        if (bomb != null) return bomb;

        // 12. 火箭
        return FindRocket(weightGroups);
    }

    /// <summary>
    /// 跟牌时找最小能压过的牌。
    /// 核心思路：同类型、同长度、主权重更大、且是最小的那个。
    /// </summary>
    /// <param name="weightGroups">手牌分组</param>
    /// <param name="lastType">上一手的牌型</param>
    /// <param name="lastMainWeight">上一手的主权重</param>
    /// <param name="lastCount">上一手的牌数（用于匹配顺子长度等）</param>
    /// <returns></returns>
    private List<CardData> FindBeatingPlay(Dictionary<int, List<CardData>> weightGroups,
                                            CardType lastType, int lastMainWeight, int lastCount)
    {
        switch (lastType)
        {
            case CardType.Single:
                return FindSmallestSingleBeating(weightGroups, lastMainWeight);

            case CardType.Pair:
                return FindSmallestPairBeating(weightGroups, lastMainWeight);

            case CardType.Triple:
                return FindSmallestTripleBeating(weightGroups, lastMainWeight);

            case CardType.TripleWithOne:
                return FindSmallestTripleWithOneBeating(weightGroups, lastMainWeight);

            case CardType.TripleWithPair:
                return FindSmallestTripleWithPairBeating(weightGroups, lastMainWeight);

            case CardType.Straight:
                return FindSmallestStraightBeating(weightGroups, lastMainWeight, lastCount);

            case CardType.PairStraight:
                return FindSmallestPairStraightBeating(weightGroups, lastMainWeight, lastCount);

            case CardType.Airplane:
                return FindSmallestAirplaneBeating(weightGroups, lastMainWeight, lastCount);

            case CardType.FourWithTwo:
                return FindSmallestFourWithTwoBeating(weightGroups, lastMainWeight);

            case CardType.Bomb:
                // 炸弹：找更大的炸弹，没有就找火箭
                List<CardData> biggerBomb = FindSmallestBombBeating(weightGroups, lastMainWeight);
                if (biggerBomb != null) return biggerBomb;
                return FindRocket(weightGroups); // 火箭压炸弹

            case CardType.Rocket:
                return null; // 火箭最大，没人能压

            default:
                return null;
        }
    }

    /// 找最小的单张（可选权重范围）
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="minWeight"></param>
    /// <param name="maxWeight"></param>
    /// <returns></returns>
    // 【修复1】C# 规定：有默认值的参数必须写在没默认值的参数后面。
    //          原来 maxWeight 没默认值却排在 minWeight = 3 后面，编译不过。
    // 【修复2】改成 private，和类里其它 Find 方法保持一致（外部不需要调用）。
    private List<CardData> FindSmallestSingle(Dictionary<int, List<CardData>> groups,
                                              int minWeight = 3, int maxWeight = 17)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w >= minWeight && w <= maxWeight && groups[w].Count >= 1)
            {
                return new List<CardData> { groups[w][0] };
            }
        }
        return null;
    }

    /// <summary>找能压过的最小单张</summary>
    private List<CardData> FindSmallestSingleBeating(Dictionary<int, List<CardData>> groups,int lastMainWeight)
    {
        return FindSmallestSingle(groups, minWeight: lastMainWeight + 1);
    }

    /// <summary>
    /// 找最小的「孤张」单牌 —— 该点数在手牌里只有 1 张。
    /// 自由出牌时优先出这种牌，因为它出了不会拆散任何对子/三张/炸弹。
    /// </summary>
    private List<CardData> FindSmallestLoneSingle(Dictionary<int, List<CardData>> groups, int maxWeight)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            // 严格等于 1 张，才算"孤张"
            if (w <= maxWeight && groups[w].Count == 1)
            {
                return new List<CardData> { groups[w][0] };
            }
        }
        return null;
    }

    /// <summary>
    /// 找最小的「纯对子」—— 该点数正好 2 张。
    /// 用 Count == 2 而不是 >= 2，是为了不拆三张和炸弹。
    /// </summary>
    private List<CardData> FindSmallestPurePair(Dictionary<int, List<CardData>> groups, int maxWeight)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w <= maxWeight && groups[w].Count == 2)
            {
                return new List<CardData> { groups[w][0], groups[w][1] };
            }
        }
        return null;
    }

    /// <summary>
    /// 找最小的对子
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="maxWeight"></param>
    /// <returns></returns>
    private List<CardData> FindSmallestPair(Dictionary<int, List<CardData>> groups, int maxWeight)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w <= maxWeight && groups[w].Count >= 2)
            {
                return new List<CardData> { groups[w][0], groups[w][1] };
            }
        }
        return null;
    }
    /// <summary>
    /// 找能压过的最小对子
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="lastMainWeight"></param>
    /// <returns></returns>
    private List<CardData> FindSmallestPairBeating(Dictionary<int, List<CardData>> groups, int lastMainWeight)
    {
        // 只在 weight > lastMainWeight 的范围内找
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w > lastMainWeight && groups[w].Count >= 2)
            {
                return new List<CardData> { groups[w][0], groups[w][1] };
            }
        }
        return null;
    }

    /// <summary>
    /// 找最小的三张
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="maxWeight"></param>
    /// <returns></returns>
    private List<CardData> FindSmallestTriple(Dictionary<int, List<CardData>> groups, int maxWeight = 15)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w <= maxWeight && groups[w].Count >= 3)
            {
                return new List<CardData> { groups[w][0], groups[w][1], groups[w][2] };
            }
        }
        return null;
    }

    /// <summary>
    /// 找最小的三带一
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="maxMainWeight">主体（三张）的权重上限</param>
    /// <returns></returns>
    // 【修复】参数名原来是 maxWeight，但调用处写的是 maxMainWeight: 14（具名参数），
    //         名字对不上 → 编译报错。这里改成和调用处一致。
    private List<CardData> FindSmallestTripleWithOne(Dictionary<int, List<CardData>> groups,
                                                     int maxMainWeight = 15)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            // 超过上限，跳过
            if (w > maxMainWeight)
            {
                continue;
            }
            // 【策略】只取正好 3 张的点数当主体。写成 < 3 兼容 4 张的话，
            // AI 会为了凑三带一把手里的炸弹拆开。
            if (groups[w].Count != 3)
            {
                continue;
            }
            // 翅膀分两轮找：先找孤张（不破坏任何牌型），找不到才退而求其次
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (int wing in weights)
                {
                    if (wing == w) continue;
                    bool usable = (pass == 0) ? groups[wing].Count == 1 : groups[wing].Count >= 1;
                    if (!usable) continue;

                    return new List<CardData>
                    {
                        groups[w][0], groups[w][1], groups[w][2], groups[wing][0]
                    };
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 找最小的三带二
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="maxWeight"></param>
    /// <returns></returns>
    // 【修复】同上，参数名对齐调用处（maxMainWeight: 14）
    private List<CardData> FindSmallestTripleWithPair(Dictionary<int, List<CardData>> groups,
                                                      int maxMainWeight)
    {
        // 【修复】weights 必须从 groups.Keys 初始化，原来是空列表，
        //         循环一次都不会进，这个方法永远返回 null。
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w > maxMainWeight) continue;
            // 【策略】主体只取正好 3 张，不拆炸弹
            if (groups[w].Count != 3) continue;
            foreach (int wing in weights)
            {
                if (wing == w) continue;
                // 【策略】翅膀只取"纯对子"（正好 2 张），不拆三张也不拆炸弹
                if (groups[wing].Count == 2)
                {
                    List<CardData> result = new List<CardData>();
                    result.Add(groups[w][0]);
                    result.Add(groups[w][1]);
                    result.Add(groups[w][2]);
                    result.Add(groups[wing][0]);
                    result.Add(groups[wing][1]);
                    return result;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 找能压过的最小三张<
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="lastMainWeight"></param>
    /// <returns></returns>
    private List<CardData> FindSmallestTripleBeating(Dictionary<int, List<CardData>> groups, int lastMainWeight)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w > lastMainWeight && groups[w].Count >= 3)
            {
                return new List<CardData> { groups[w][0], groups[w][1], groups[w][2] };
            }
        }
        return null;
    }

    /// <summary>
    /// 找能压过的最小三带一
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="lastMainWeight"></param>
    /// <returns></returns>
    private List<CardData> FindSmallestTripleWithOneBeating(Dictionary<int, List<CardData>> groups, int lastMainWeight)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w <= lastMainWeight) continue;
            if (groups[w].Count < 3) continue;
            foreach (int wing in weights)
            {
                if (wing == w) continue;
                // 【修复】判断翅膀 wing 的张数，不是主体 w
                if (groups[wing].Count >= 1)
                {
                    List<CardData> result = new List<CardData>();
                    result.Add(groups[w][0]);
                    result.Add(groups[w][1]);
                    result.Add(groups[w][2]);
                    result.Add(groups[wing][0]);
                    return result;
                }
            }
        }
        return null;
    }
    /// <summary>
    /// 找能压过的最小三带二
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="lastMainWeight"></param>
    /// <returns></returns>
    private List<CardData> FindSmallestTripleWithPairBeating(Dictionary<int, List<CardData>> groups, int lastMainWeight)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();
        foreach (int w in weights)
        {
            if (w <= lastMainWeight) continue;
            if (groups[w].Count < 3) continue;
            foreach (int wing in weights)
            {
                if (wing == w) continue;
                // 【修复】判断翅膀 wing 的张数，不是主体 w
                if (groups[wing].Count >= 2)
                {
                    List<CardData> result = new List<CardData>();
                    result.Add(groups[w][0]);
                    result.Add(groups[w][1]);
                    result.Add(groups[w][2]);
                    result.Add(groups[wing][0]);
                    result.Add(groups[wing][1]);
                    return result;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 找最短的顺子（5张起）。
    /// 算法：遍历起点 weight (3~10)，检查连续5个 weight 是否都有 ≥1 张牌。
    /// </summary>
    /// <param name="groups"></param>
    /// <returns></returns>
    private List<CardData> FindShortestStraight(Dictionary<int, List<CardData>> groups)
    {
        // 顺子只能在 3~A (weight 3~14) 范围内
        // 起点最大是10（10,J,Q,K,A=5张）
        for (int start = 3; start <= 10; start++)
        {
            bool hasFive = true;
            for (int i = 0; i < 5; i++)
            {
                // 【修复】下标写错了：应该是 start + i（当前检查的那一位），
                //         写成 start + 1 会让检查永远只看第二张，顺子判断失效
                if (!groups.ContainsKey(start + i) || groups[start + i].Count < 1)
                {
                    hasFive = false;
                    break;
                }
            }
            if (!hasFive) continue;
            // 找到了！尝试延伸（看能不能更长，但优先最短的）
            // 这里我们直接返回最短的5张顺子
            List<CardData> result = new List<CardData>();
            for (int i = 0; i < 5; i++)
            {
                result.Add(groups[start + i][0]);
            }
            return result;
        }
        return null;
    }

    /// <summary>
    /// 找能压过的最小顺子。
    /// 条件：长度相同、起点 > last 的起点。
    /// </summary>
    /// <param name="groups"></param>
    /// <param name="lastMainWeight"></param>
    /// <returns></returns>
    private List<CardData> FindSmallestStraightBeating(Dictionary<int, List<CardData>> groups, int lastMainWeight, int lastCount)
    {
        // 顺子长度 = 上一手牌数
        int straightLen = lastCount;
        // 上一手的起点 = lastMainWeight（因为顺子的主牌就是最大那张，需要反推起点）
        // 实际上对于顺子，GetMainWeight 返回的是最大 weight
        // 所以起点 = 最大 weight - 长度 + 1
        int lastStart = lastMainWeight - straightLen + 1;
        for (int start = lastStart + 1; start <= 15 - straightLen + 1; start++)
        {
            // 不能包含2和王
            if (start + straightLen - 1 > 14) break;
            bool hasAll = true;
            for (int i = 0; i < straightLen; i++)
            {
                if (!groups.ContainsKey(start + i) || groups[start + i].Count < 1)
                {
                    hasAll = false;
                    break;
                }
            }
            if (!hasAll) continue;
            List<CardData> result = new List<CardData>();
            for (int i = 0; i < straightLen; i++)
            {
                result.Add(groups[i + start][0]);
            }
            return result;
        }
        return null;
    }

    /// <summary>
    /// 找最短的连对（3连对=6张起）
    /// </summary>
    /// <param name="groups"></param>
    /// <returns></returns>
    private List<CardData> FindShortestPairStraight(Dictionary<int, List<CardData>> groups)
    {
        // 【修复】起点可以取到 12（Q-K-A 也是合法三连对），所以是 <= 12 而不是 < 12
        for (int start = 3; start <= 12; start++)
        {
            bool hasThree = true;
            for (int i = 0; i < 3; i++)
            {
                if (!groups.ContainsKey(start + i) || groups[start + i].Count < 2)
                {
                    hasThree = false;
                    break;
                }
            }
            if (!hasThree) continue;
            // 找到了最短的连对
            List<CardData> result = new List<CardData>();
            for (int i = 0; i < 3; i++)
            {
                result.Add(groups[start + i][0]);
                result.Add(groups[start + i][1]);
            }
            return result;
        }
        return null;
    }
    /// <summary>找能压过的最小连对</summary>
    private List<CardData> FindSmallestPairStraightBeating(Dictionary<int, List<CardData>> groups,int lastMainWeight, int lastCount)
    {
        int pairCount = lastCount / 2; // 连对有几对
        int lastStart = lastMainWeight - pairCount + 1;

        for (int start = lastStart + 1; start <= 15 - pairCount; start++)
        {
            if (start + pairCount - 1 > 14) break;

            bool hasAll = true;
            for (int i = 0; i < pairCount; i++)
            {
                if (!groups.ContainsKey(start + i) || groups[start + i].Count < 2)
                {
                    hasAll = false;
                    break;
                }
            }
            if (!hasAll) continue;

            List<CardData> result = new List<CardData>();
            for (int i = 0; i < pairCount; i++)
            {
                result.Add(groups[start + i][0]);
                result.Add(groups[start + i][1]);
            }
            return result;
        }
        return null;
    }

    #endregion

    #region 炸弹搜索

    /// <summary>找最小的炸弹</summary>
    private List<CardData> FindSmallestBomb(Dictionary<int, List<CardData>> groups)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();

        foreach (int w in weights)
        {
            if (groups[w].Count >= 4)
            {
                return new List<CardData>
                {
                    groups[w][0], groups[w][1], groups[w][2], groups[w][3]
                };
            }
        }
        return null;
    }

    /// <summary>找能压过的最小炸弹</summary>
    private List<CardData> FindSmallestBombBeating(Dictionary<int, List<CardData>> groups,
                                                    int lastMainWeight)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();

        foreach (int w in weights)
        {
            if (w > lastMainWeight && groups[w].Count >= 4)
            {
                return new List<CardData>
                {
                    groups[w][0], groups[w][1], groups[w][2], groups[w][3]
                };
            }
        }
        return null;
    }

    /// <summary>找火箭（大小王）</summary>
    private List<CardData> FindRocket(Dictionary<int, List<CardData>> groups)
    {
        if (groups.ContainsKey(16) && groups.ContainsKey(17))
        {
            return new List<CardData> { groups[16][0], groups[17][0] };
        }
        return null;
    }

    #endregion

    #region 飞机搜索

    /// <summary>找最短的飞机带翅膀（2连三张=至少6张主体）</summary>
    private List<CardData> FindShortestAirplane(Dictionary<int, List<CardData>> groups)
    {
        // 飞机至少2连三张，在 3~A 范围内
        for (int start = 3; start <= 13; start++)
        {
            // 检查是否连续2个 weight 都有 3 张
            if (!groups.ContainsKey(start) || groups[start].Count < 3) continue;
            if (!groups.ContainsKey(start + 1) || groups[start + 1].Count < 3) continue;

            // 找到了2连飞机主体，找翅膀（2张单牌）
            List<CardData> result = new List<CardData>();
            for (int i = 0; i < 3; i++)
            {
                result.Add(groups[start][i]);
                result.Add(groups[start + 1][i]);
            }

            // 找2个翅膀
            int wingAdded = 0;
            List<int> weights = new List<int>(groups.Keys);
            weights.Sort();
            foreach (int w in weights)
            {
                if (w == start || w == start + 1) continue; // 不能和主体同 weight
                if (groups[w].Count >= 1)
                {
                    result.Add(groups[w][0]);
                    wingAdded++;
                    if (wingAdded >= 2) break;
                }
            }

            if (wingAdded >= 2) return result;
        }
        return null;
    }

    /// <summary>找能压过的最小飞机</summary>
    private List<CardData> FindSmallestAirplaneBeating(Dictionary<int, List<CardData>> groups,
                                                        int lastMainWeight, int lastCount)
    {
        // 上一手飞机：lastMainWeight 是最大三张的 weight
        // 简化处理：找比上一手主体更大的飞机
        // 实际中需要更复杂的匹配，这里做简化版
        for (int start = lastMainWeight; start <= 13; start++)
        {
            if (start + 1 > 14) break;

            if (!groups.ContainsKey(start) || groups[start].Count < 3) continue;
            if (!groups.ContainsKey(start + 1) || groups[start + 1].Count < 3) continue;

            List<CardData> result = new List<CardData>();
            for (int i = 0; i < 3; i++)
            {
                result.Add(groups[start][i]);
                result.Add(groups[start + 1][i]);
            }

            int wingAdded = 0;
            List<int> weights = new List<int>(groups.Keys);
            weights.Sort();
            foreach (int w in weights)
            {
                if (w == start || w == start + 1) continue;
                if (groups[w].Count >= 1)
                {
                    result.Add(groups[w][0]);
                    wingAdded++;
                    if (wingAdded >= 2) break;
                }
            }

            if (wingAdded >= 2) return result;
        }
        return null;
    }

    #endregion

    #region 四带二搜索

    /// <summary>找能压过的最小四带二</summary>
    private List<CardData> FindSmallestFourWithTwoBeating(Dictionary<int, List<CardData>> groups,
                                                           int lastMainWeight)
    {
        List<int> weights = new List<int>(groups.Keys);
        weights.Sort();

        foreach (int w in weights)
        {
            if (w <= lastMainWeight) continue;
            if (groups[w].Count < 4) continue;

            // 找到了四张主体，找2张翅膀
            List<CardData> result = new List<CardData>();
            for (int i = 0; i < 4; i++)
            {
                result.Add(groups[w][i]);
            }

            int wingAdded = 0;
            foreach (int wingW in weights)
            {
                if (wingW == w) continue;
                if (groups[wingW].Count >= 1)
                {
                    result.Add(groups[wingW][0]);
                    wingAdded++;
                    if (wingAdded >= 2) break;
                }
            }

            if (wingAdded >= 2) return result;
        }
        return null;
    }
    #endregion
}
