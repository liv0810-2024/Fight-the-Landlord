using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
///  JSON 解析用的包装类
///  JSON（JavaScript Object Notation）是一种轻量级的数据交换格式，用纯文本的方式存储结构化数据。
/// </summary>
[System.Serializable]
public class CardDataWrapper
{
    /// <summary>
    /// 卡牌列表，名字必须和JSON 里的字段名一致（cards），否则解析不到
    /// </summary>
    public List<CardData> cards;
}

public class DataManager : Singleton<DataManager>
{
    public List<CardData> AllCards { get; private set; }
    /// <summary>
    /// 标记数据是否加载完成
    /// </summary>
    public bool isDataLoaded { get; private set; }
    /// <summary>
    /// 根据卡牌 id快速查找卡牌数据
    /// </summary>
    public Dictionary<int, CardData> cardDataDict;
    protected override void Awake()
    {
        base.Awake();
        cardDataDict = new Dictionary<int, CardData>();
        isDataLoaded = false;
    }
    private void Start()
    {
        LoadCardData();
    }
    /// <summary>
    /// 从 JSON文件加载全部卡牌配置
    /// </summary>
    public void LoadCardData()
    {
        //用ResManager 加载Resources / Data / CardData.json文件
        // TextAsset：Unity里代表纯文本文件（.json .txt .csv 等）
        TextAsset textAsset = ResManager.Instance.Load<TextAsset>("Data/CardData");
        if (textAsset == null)
        {
            Debug.LogError($"[DataManager]加载CardData.json失败！请检查Resources/Data/目录下是否存在该文件");
            return;
        }
        // 第二步：用JsonUtility 把 JSON字符串反序列化成 C# 对象
        //FromJson<CardDataWrapper>()会创建一个 CardDataWrapper对象，并按照 JSON内容填充其字段
        CardDataWrapper wrapper = JsonUtility.FromJson<CardDataWrapper>(textAsset.text);
        if (wrapper == null || wrapper.cards == null || wrapper.cards.Count == 0)
        {
            Debug.LogError("[DataManager]  解析 CardData.json 失败！JSON 格式可能有误");
            return;
        }
        // 第三步：存入AllCards 列表（全局只读缓存）
        AllCards = wrapper.cards;
        //第四步：构建字典，方便后续按id 快速查找
        cardDataDict.Clear();
        foreach (CardData card in AllCards)
        {
            // 防止重复 id导致报错：如果字典里已经有了这个 id，跳过
            if (!cardDataDict.ContainsKey(card.id))
            {
                cardDataDict.Add(card.id, card);
            }
            else
            {
                Debug.LogWarning($"[DataManager]  CardData.json 中有重复的 id：{card.id}，已跳过");
            }
        }
        isDataLoaded = true;
        Debug.Log($"[DataManager]卡牌数据加载完成！共加载{AllCards.Count}张卡牌配置");
    }
    /// <summary>
    /// 根据 id获取单张卡牌的配置数据
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public CardData GetCardById(int id)
    {
        if (cardDataDict.TryGetValue(id, out CardData card))
        {
            return card;
        }
        Debug.LogWarning($"[DataManager] 找不到 id = {id}的卡牌数据，请检查CardData.json 配置");
        return null;
    }
    /// <summary>
    /// 根据点数 rank获取所有该点数的卡牌（比如拿到所有"A")
    /// </summary>
    /// <param name="rank"></param>
    /// <returns></returns>
    public List<CardData> GetCardByRank(CardRank rank)
    {
        List<CardData> result = new List<CardData>();
        foreach (CardData card in AllCards)
        {
            if (card.rank == rank)
            {
                result.Add(card);
            }
        }
        return result;
    }
}
