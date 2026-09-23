using System.Collections.Generic;

/// <summary>
/// 出牌枚举器 —— 把手牌里"所有打得出去的组合"列出来，并按由小到大排好。
///
/// 【和 AIPlayer 的分工】AIPlayer 只回答"我该出什么"，返回唯一的最优解；
/// 这里回答"我能出哪些"，把所有可行解都摆出来，让玩家自己挑。
/// 两个问题的答案不一样（AI 要选聪明的，提示要选全的），所以分成两个类。
///
/// 【为什么每种牌型只出一个代表】三带一光"主体×翅膀"就可能有几十种配法，
/// 全列出来玩家要点几十次才能轮一圈。这里每种主体只配一副最省牌的翅膀。
/// </summary>
public static class PlayEnumerator
{
    /// <summary>
    /// 列出手牌的所有建议出法，由小到大排序。
    /// </summary>
    /// <param name="hand">我的手牌</param>
    /// <param name="last">上一手牌（null 或空 = 自由出牌）</param>
    public static List<List<CardData>> Enumerate(List<CardData> hand, List<CardData> last)
    {
        List<List<CardData>> result = new List<List<CardData>>();
        if (hand == null || hand.Count == 0) return result;

        bool isFree = last == null || last.Count == 0;
        List<List<CardData>> all = EnumerateAllTypes(hand);

        foreach (List<CardData> play in all)
        {
            if (!isFree)
            {
                // 跟牌：压不过的直接淘汰
                if (!CardRules.CanBeat(play, last)) continue;
                // 【补规则】CanBeat 只比牌型和主权重，没比张数 —— 5 张顺子会被判成能压 6 张顺子。
                // 提示不能给出这种非法组合，所以这里补上张数校验。
                // 炸弹和火箭不受张数限制（4 张炸弹可以压任意长度的顺子）。
                CardType t = CardRules.CheckCardType(play);
                if (t != CardType.Bomb && t != CardType.Rocket && play.Count != last.Count) continue;
            }
            result.Add(play);
        }

        result.Sort((a, b) => Compare(a, b, isFree));
        return result;
    }

    /// <summary>排序：先比牌型档位，同档位再比主牌权重（都是升序 = 从小到大）</summary>
    private static int Compare(List<CardData> a, List<CardData> b, bool isFree)
    {
        int ra = Slot(a, isFree);
        int rb = Slot(b, isFree);
        if (ra != rb) return ra.CompareTo(rb);
        return CardRules.GetMainWeight(a).CompareTo(CardRules.GetMainWeight(b));
    }

    private static int Slot(List<CardData> play, bool isFree)
    {
        CardType t = CardRules.CheckCardType(play);
        if (isFree) return FreeSlot(t);
        // 跟牌不用比牌型档位（能压过的必然同类型），只要把炸弹和火箭排到最后
        if (t == CardType.Rocket) return 2;
        if (t == CardType.Bomb) return 1;
        return 0;
    }

    /// <summary>
    /// 自由出牌时的推荐顺序：从"最不亏"的散牌开始，炸弹火箭垫底。
    /// 和 AIPlayer.FindBestFreePlay 的策略顺序一致 —— 提示和 AI 的直觉统一。
    /// </summary>
    private static int FreeSlot(CardType type)
    {
        switch (type)
        {
            case CardType.Single: return 0;
            case CardType.Pair: return 1;
            case CardType.Triple: return 2;
            case CardType.TripleWithOne: return 3;
            case CardType.TripleWithPair: return 4;
            case CardType.Straight: return 5;
            case CardType.PairStraight: return 6;
            case CardType.Airplane: return 7;
            case CardType.FourWithTwo: return 8;
            case CardType.Bomb: return 9;
            case CardType.Rocket: return 10;
            default: return 99;
        }
    }

    #region 枚举各牌型

    /// <summary>把每种合法牌型都生成一个代表组合</summary>
    private static List<List<CardData>> EnumerateAllTypes(List<CardData> hand)
    {
        List<List<CardData>> list = new List<List<CardData>>();
        Dictionary<int, List<CardData>> g = GroupByWeight(hand);
        List<int> ws = new List<int>(g.Keys);
        ws.Sort();

        // ---- 单张 / 对子 / 三张 / 炸弹：同一个点数各出一个 ----
        foreach (int w in ws)
        {
            List<CardData> group = g[w];
            list.Add(Take(group, 1));
            if (group.Count >= 2) list.Add(Take(group, 2));
            if (group.Count >= 3) list.Add(Take(group, 3));
            if (group.Count >= 4) list.Add(Take(group, 4));
        }

        // ---- 三带一 / 三带二 ----
        foreach (int w in ws)
        {
            if (g[w].Count < 3) continue;
            HashSet<int> used = new HashSet<int> { w };
            List<CardData> body = Take(g[w], 3);

            List<CardData> wing1 = PickWings(g, ws, used, 1, 1);
            if (wing1 != null) list.Add(Concat(body, wing1));

            List<CardData> wing2 = PickWings(g, ws, used, 1, 2);
            if (wing2 != null) list.Add(Concat(body, wing2));
        }

        // ---- 四带二 ----
        foreach (int w in ws)
        {
            if (g[w].Count < 4) continue;
            HashSet<int> used = new HashSet<int> { w };
            List<CardData> wings = PickWings(g, ws, used, 2, 1);
            if (wings != null) list.Add(Concat(Take(g[w], 4), wings));
        }

        // ---- 顺子：长度 5~12，起点 3 起，不能含 2 和王 ----
        for (int len = 5; len <= 12; len++)
        {
            for (int start = 3; start + len - 1 <= 14; start++)
            {
                List<CardData> s = BuildRun(g, start, len, 1);
                if (s != null) list.Add(s);
            }
        }

        // ---- 连对：3 连对起 ----
        for (int pairs = 3; pairs <= 10; pairs++)
        {
            for (int start = 3; start + pairs - 1 <= 14; start++)
            {
                List<CardData> s = BuildRun(g, start, pairs, 2);
                if (s != null) list.Add(s);
            }
        }

        // ---- 飞机：2 连三张起，不带翅膀 / 带单 / 带对 ----
        for (int n = 2; n <= 5; n++)
        {
            for (int start = 3; start + n - 1 <= 14; start++)
            {
                List<CardData> body = BuildRun(g, start, n, 3);
                if (body == null) continue;

                HashSet<int> used = new HashSet<int>();
                for (int i = 0; i < n; i++) used.Add(start + i);

                // 不带翅膀的飞机本身就是合法牌型
                list.Add(body);

                List<CardData> w1 = PickWings(g, ws, used, n, 1);
                if (w1 != null) list.Add(Concat(body, w1));

                List<CardData> w2 = PickWings(g, ws, used, n, 2);
                if (w2 != null) list.Add(Concat(body, w2));
            }
        }

        // ---- 火箭 ----
        if (g.ContainsKey(16) && g.ContainsKey(17))
        {
            list.Add(new List<CardData> { g[16][0], g[17][0] });
        }

        return list;
    }

    /// <summary>
    /// 从 start 开始，连续 count 个点数，每个点数取 perGroup 张。
    /// 凑不齐返回 null。
    /// </summary>
    private static List<CardData> BuildRun(Dictionary<int, List<CardData>> g, int start, int count, int perGroup)
    {
        List<CardData> result = new List<CardData>();
        for (int i = 0; i < count; i++)
        {
            int w = start + i;
            if (!g.ContainsKey(w) || g[w].Count < perGroup) return null;
            for (int k = 0; k < perGroup; k++) result.Add(g[w][k]);
        }
        return result;
    }

    /// <summary>
    /// 挑 wings 组翅膀，每组 perGroup 张，不能碰主体已经用掉的点数。
    ///
    /// 【优先选张数少的点数】一个点数在手牌里越多，越可能是对子/三张/炸弹的一部分。
    /// 拿张数最少的去当翅膀，才不会为了凑一个三带一把手里的炸弹拆了。
    /// </summary>
    private static List<CardData> PickWings(Dictionary<int, List<CardData>> g, List<int> ws,
                                            HashSet<int> used, int groups, int perGroup)
    {
        List<int> candidates = new List<int>();
        foreach (int w in ws)
        {
            if (used.Contains(w)) continue;
            if (g[w].Count < perGroup) continue;
            candidates.Add(w);
        }
        if (candidates.Count < groups) return null;

        candidates.Sort((a, b) =>
        {
            int ca = g[a].Count, cb = g[b].Count;
            if (ca != cb) return ca.CompareTo(cb); // 张数少的优先
            return a.CompareTo(b);                 // 张数相同就小的优先
        });

        List<CardData> wings = new List<CardData>();
        for (int i = 0; i < groups; i++)
        {
            List<CardData> group = g[candidates[i]];
            for (int k = 0; k < perGroup; k++) wings.Add(group[k]);
        }
        return wings;
    }

    #endregion

    #region 小工具

    private static Dictionary<int, List<CardData>> GroupByWeight(List<CardData> hand)
    {
        Dictionary<int, List<CardData>> groups = new Dictionary<int, List<CardData>>();
        foreach (CardData card in hand)
        {
            if (!groups.ContainsKey(card.weight)) groups[card.weight] = new List<CardData>();
            groups[card.weight].Add(card);
        }
        return groups;
    }

    /// <summary>取一组牌里的前 n 张（手牌已排序，所以就是最小的 n 张）</summary>
    private static List<CardData> Take(List<CardData> cards, int n)
    {
        List<CardData> result = new List<CardData>();
        for (int i = 0; i < n && i < cards.Count; i++) result.Add(cards[i]);
        return result;
    }

    private static List<CardData> Concat(List<CardData> a, List<CardData> b)
    {
        List<CardData> result = new List<CardData>(a.Count + b.Count);
        result.AddRange(a);
        result.AddRange(b);
        return result;
    }

    #endregion
}
