using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏全局大状态
/// </summary>
public enum GameState
{
    Idle,
    DealCard, //发牌
    GrabLandlord,  //抢地主
    PlayCard,
    RoundOver //结算阶段面板
}
/// <summary>
/// 控制游戏整体大状态流转，游戏启动在这里初始化全部底层模块
/// </summary>
public class GameMainManager : Singleton<GameMainManager>
{
    //当前状态
    public GameState CurrentGameState { get; private set; }
    private void Start()
    {
        InitAllFramework();
    }
    /// <summary>
    /// 初始化全部底层管理器模块
    /// </summary>
    private void InitAllFramework()
    {
       
    }
    /// <summary>
    /// 切换游戏全局状态，所有状态变更强制调用这个方法
    /// </summary>
    /// <param name="newstate"></param>
    public void SwitchGameState(GameState newstate)
    {
        CurrentGameState = newstate;
        Debug.Log($"状态已切换为:{CurrentGameState}");
        //其他脚本监听 GameEvent.Game_StartGame，拿到 newState 参数执行对应业务逻辑
        EventCenter.Instance.Trigger(GameEvent.Game_StartGame, newstate);
    }
}
