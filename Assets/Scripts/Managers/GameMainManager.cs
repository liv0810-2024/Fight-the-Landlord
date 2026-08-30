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
    protected override void Awake()
    {
        base.Awake();
        EventCenter.Instance.Register(GameEvent.Game_DealCardFinish,OnDealFInish);
        EventCenter.Instance.Register(GameEvent.Game_GrabLandlord,OnGrabLandlord);
        EventCenter.Instance.Register(GameEvent.Game_RoundOver,OnRoundOver);
    }
    private IEnumerator Start()
    {
        InitAllFramework();
        // 【防御式编程】等卡牌数据加载完成，再开始发牌。
        // WaitUntil(条件)：协程会""停在这里"，直到条件返回 true 才继续。
        yield return new WaitUntil(() => DataManager.Instance.isDataLoaded);
        StartGame();
    }

    /// <summary>
    /// 启动游戏：进入发牌阶段，触发发牌。
    /// </summary>
    public void StartGame()
    {
        SwitchGameState(GameState.DealCard);
        DeckManager.Instance.DealCards();
    }

    /// <summary>
    /// 回调：发牌完成 →进入抢地主阶段。
    /// </summary>
    /// <param name="param"></param>
    public void OnDealFInish(object param)
    {
        SwitchGameState(GameState.GrabLandlord);
        EventCenter.Instance.Trigger(GameEvent.UI_OpenGrabPanel);
        Debug.Log("请抢地主");
    }

    /// <summary>
    /// 回调：地主确定 →进入出牌阶段，且地主先出。
    /// </summary>
    /// <param name="param"></param>
    public void OnGrabLandlord(object param)
    {
        SwitchGameState(GameState.PlayCard);
        //事件参数就是地主索引（LandlordManager触发时传进来的）。
        int landlord = (int)param;
        PlayCardManager.Instance.StartPlay(landlord);
    }

    /// <summary>
    /// 回调：有人出完牌 →进入结算阶段。
    /// </summary>
    /// <param name="param"></param>
    public void OnRoundOver(object param)
    {
        SwitchGameState(GameState.RoundOver);
        Debug.Log("本局结束，进入结算");
    }
    /// <summary>
    /// 初始化全部底层管理器模块
    /// </summary>
    private void InitAllFramework()
    {
        //// 触发ObjectPoolManager 的单例初始化（第一次访问 Instance 会自动创建）
        var pool = ObjectPoolManager.Instance;
        var data = DataManager.Instance;
        var deck=DeckManager.Instance;
        var layout=CardLayoutManager.Instance;
        var play = PlayCardManager.Instance;
        var landlord=LandlordManager.Instance;
        var ui=UIManager.Instance;
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
    public void RestarGame()
    {
        UIManager.Instance.ClosePanel(UIName.ResultPanel);
        //清空出牌区
        CardLayoutManager.Instance.ShowPlayArea(null);
        LandlordManager.Instance.ResetState();
        PlayCardManager.Instance.ResetState();
        StartGame();
    }
}
