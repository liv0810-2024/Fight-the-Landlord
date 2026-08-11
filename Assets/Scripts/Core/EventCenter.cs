using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCenter :Singleton<EventCenter>
{
   private readonly Dictionary<string,Action<object>> _eventDict=new Dictionary<string,Action<object>>();
   /// <summary>
   /// 注册事件
   /// </summary>
   /// <param name="name"></param>
   /// <param name="action"></param>
    public void Register(string name,Action<object> action)
    {
        if (!_eventDict.ContainsKey(name))
        {
            //字典里面没有这个事件，先初始化赋值null 防止报错
            _eventDict[name] = null; 
        }
        _eventDict[name] += action; //+= 追加回调函数，多个脚本可以监听同一个事件
    }
    /// <summary>
    /// 移除事件
    /// </summary>
    public void UnRegister(string eventName,Action<object> actionName)
    {
       if(_eventDict.TryGetValue(eventName,out var action))
        {
            action-=actionName;
        }
    }
    public void Trigger(string eventName,object param=null)
    {
        //TryGetValue安全读取字典，拿到对应事件的全部回调(只查询字典 1 次)
        if (_eventDict.TryGetValue(eventName,out var action))
        {
            //?.Invoke()：action不为空才执行全部绑定的函数
            action?.Invoke(param);
        }
    }
    /// <summary>
    ///  清空全部事件，一局游戏结束调用，清理残留监听
    /// </summary>
    public void ClearAllEvent()=>_eventDict.Clear();
}
public static class GameEvent 
{
    /// <summary>游戏状态发生切换，携带参数：GameState枚举，通知所有模块游戏进入了哪个阶段</summary>
    public const string Game_StartGame = "Game_StartGame";
    /// <summary>发牌全部完成事件：所有玩家手牌已经分发完毕</summary>
    public const string Game_DealCardFinish = "Game_DealCardFinish";
    /// <summary>抢地主事件：有人做出抢地主/不抢地主操作，携带参数可以传递玩家选择结果</summary>
    public const string Game_GrabLandlord = "Game_GrabLandlord";
    /// <summary>本局游戏回合结束，结算事件，携带参数可以传本局分数、胜负结果</summary>
    public const string Game_RoundOver = "Game_RoundOver";
    /// <summary>玩家出牌事件：有玩家打出一组卡牌，携带参数传递打出去的卡牌集合</summary>
    public const string Game_PlayCard = "Game_PlayCard";
    //卡牌交互事件
    public const string Card_Select = "Card_Select";
    public const string Card_PlayOut = "Card_PlayOut";
    //UI事件
    /// <summary>打开抢地主选择面板事件，通知UI显示抢地主/不抢地主按钮弹窗</summary>
    public const string UI_OpenGrabPanel = "UI_OpenGrabPanel";
}

