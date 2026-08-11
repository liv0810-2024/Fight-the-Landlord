using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 卡牌数据模型【纯数据类，不要挂到场景物体上】
/// </summary>
[Serializable]
public class CardData
{
    public int id; 
    /// <summary>卡牌花色：黑桃/红桃/梅花/方块/无(大小王)</summary>
    public CardSuit suit;
    /// <summary>卡牌点数：3、4 ... A、2、小王、大王</summary>
    public CardRank rank;
    /// <summary>权重，出牌比对大小直接拿这个数字，越大牌越大</summary>
    public int weight;
    /// <summary>是否小王，只读</summary>
    public bool isSamllKing => rank == CardRank.SmallKing;
    /// <summary>是否大王，只读，外部只能读不能改</summary>
    public bool isBigKing=>rank==CardRank.BigKing;
}
/// <summary>卡牌花色枚举</summary>
public enum CardSuit
{
    Spade,
    Heart,
    Club,
    Diamond,
    Nome //大小王没有花色
}
/// <summary>卡牌点数枚举，枚举int值直接当做权重使用</summary>
public enum CardRank
{
    Three=3,
    Four=4,
    Five=5,
    Six=6,
    Seven=7,
    Eight=8,
    Nine=9,
    Ten=10,
    J=11,
    Q=12,
    K=13,
    A=14,
    Two=15,
    SmallKing=16,
    BigKing=17
}
