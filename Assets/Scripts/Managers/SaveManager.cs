using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档数据结构。
/// 【注意】JsonUtility 只能序列化 public 字段（属性不行），所以这里全部用 public 字段。
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>三人累计得分，下标=玩家索引</summary>
    public int[] totalScores = new int[3];

    /// <summary>总共打过的局数</summary>
    public int totalGames;

    /// <summary>玩家作为赢家阵营获胜的次数</summary>
    public int winCount;

    /// <summary>玩家输掉的次数</summary>
    public int loseCount;

    /// <summary>玩家当了几次地主</summary>
    public int landlordGames;

    /// <summary>玩家当地主赢了几次</summary>
    public int landlordWins;

    /// <summary>音乐音量 0~1</summary>
    public float musicVolume = 1f;

    /// <summary>音效音量 0~1</summary>
    public float soundVolume = 1f;
}

/// <summary>
/// 存档管理器：负责把游戏进度持久化到本地。
/// 用 PlayerPrefs + JsonUtility，无需第三方库，跨平台。
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    /// <summary>存档键名，带版本号方便以后改结构</summary>
    private const string SAVE_KEY = "FightTheGame.Save.v1";

    /// <summary>当前存档数据（唯一数据源，其它模块只读不写）</summary>
    private SaveData data;

    /// <summary>对外暴露的存档数据</summary>
    public SaveData Data => data;

    protected override void Awake()
    {
        base.Awake();
        Load();
    }

    /// <summary>
    /// 从本地读取存档。没有存档或存档损坏时，自动创建一份新的。
    /// </summary>
    public void Load()
    {
        data = null;

        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                // 存档损坏不能崩游戏，降级成新档
                Debug.LogError($"[SaveManager] 存档解析失败，将创建新存档：{e.Message}");
            }
        }

        if (data == null)
        {
            data = new SaveData();
            Debug.Log("[SaveManager] 未找到有效存档，已创建新存档");
        }

        // 【防御】老版本存档可能缺字段（JsonUtility 对缺失的数组会给 null 或空数组）
        if (data.totalScores == null || data.totalScores.Length != 3)
        {
            data.totalScores = new int[3];
        }
    }

    /// <summary>
    /// 写入本地。改过 Data 里的字段后记得调一次。
    /// </summary>
    public void Save()
    {
        if (data == null) return;
        try
        {
            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save(); // 立即落盘，避免程序被强杀时丢数据
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] 存档写入失败：{e.Message}");
        }
    }

    /// <summary>
    /// 一局结束后更新战绩统计并落盘。
    ///
    /// 【调用时机】必须在 ResultManager 算完本局得分之后 —— 因为 totalScores
    /// 和 ResultManager 共用同一个数组引用，这里读到的已经是累加后的值。
    /// </summary>
    /// <param name="isPlayerWin">玩家所在阵营是否获胜</param>
    /// <param name="isPlayerLandlord">玩家本局是不是地主</param>
    public void RecordGame(bool isPlayerWin, bool isPlayerLandlord)
    {
        data.totalGames++;
        if (isPlayerWin) data.winCount++;
        else data.loseCount++;

        if (isPlayerLandlord)
        {
            data.landlordGames++;
            if (isPlayerWin) data.landlordWins++;
        }

        Save();

        Debug.Log($"[SaveManager] 战绩已更新：总局数={data.totalGames}，" +
                  $"胜={data.winCount}，负={data.loseCount}");
    }

    /// <summary>玩家胜率（0~1），没打过返回 0</summary>
    public float GetWinRate()
    {
        if (data.totalGames <= 0) return 0f;
        return (float)data.winCount / data.totalGames;
    }

    /// <summary>清空存档（重新开始 / 调试用）</summary>
    public void ResetSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        data = new SaveData();
        Debug.Log("[SaveManager] 存档已清空");
    }
}
