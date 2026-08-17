using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 抢地主管理器:负责抢地主流程、底牌归属、倍数计算。
/// </summary>
public class LandlordManager : Singleton<LandlordManager>
{
    /// <summary>地主索引：0=我，1=左边AI，2=右边AI，-1=还没确定</summary>
    private int landLordIndex=-1; //索引 记录谁是地主
    private int multiple = 1; //当前倍数
    private bool isLandlordConfirmed= false; 
    protected override void Awake()
    {
        base.Awake();
    }
    /// <summary>
    /// 【临时】Update：用键盘触发抢地主，后面做完 UI 按钮后删除。
      /// 按 G = 抢地主，按 P = 不抢。
      /// </summary>
    private void Update()
    {
        if (isLandlordConfirmed) return;
        if (Input.GetKeyDown(KeyCode.G))
        {
            PlayerChooseGrab(true);
        }
        if(Input.GetKeyDown(KeyCode.P))
        {
            PlayerChooseGrab(false);
        }
    }
    /// <summary>
    /// 玩家选择"抢"或"不抢"的入口。
    /// 
    /// </summary>
    /// <param name="grab">true=抢，false=不抢</param>
    public void PlayerChooseGrab(bool grab)
    {
        if (isLandlordConfirmed) return;
        if (grab)
        {
            multiple *= 2;
            ConfirmLandlord(0);  
        }
        else
        {
            ConfirmLandlord(1);
        }
    }
    /// <summary>
    /// 确定地主：记录身份、发底牌、翻倍数。
    /// </summary>
    /// <param name="index">玩家索引</param>
    public void ConfirmLandlord(int index)
    {
        landLordIndex = index;
        isLandlordConfirmed = true;
        AssignBottomCards(index);
        Debug.Log("[LandlordManager]地主确定：" + GetPlayerName(index) +"，倍数 " + multiple);
    }
    public void AssignBottomCards(int index)
    {
        //数据层：底牌加进地主手牌
        List<CardData> hand=GetHandByIndex(index);
        hand.AddRange(DeckManager.Instance.bottomCards);
        hand.Sort((a,b)=>a.weight.CompareTo(b.weight));
        EventCenter.Instance.Trigger(GameEvent.Game_GrabLandlord, index);
    }
    /// <summary>
    /// 获取玩家名字
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private string GetPlayerName(int index)
    {
        if (index == 0) return "玩家";
        if (index == 1) return "电脑AI（左）";
        return "电脑AI（右）";
    }
    /// <summary>
    /// 通过索引获取手牌
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private List<CardData> GetHandByIndex(int index)
    {
        if (index == 0) return DeckManager.Instance.myHand;
        if (index == 1) return DeckManager.Instance.leftHand;
        return DeckManager.Instance.rightHand;
    }
}
