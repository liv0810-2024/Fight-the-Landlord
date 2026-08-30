using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 抢地主面板
/// </summary>
public class GradPanel : BasePanel
{
    [SerializeField]public Button grabButton;  //抢地主按钮
    [SerializeField] public Button passButton;  //不抢按钮
    private void Awake()
    {
        UIManager.Instance.RegisterPanel(UIName.GrabPanel, this); //登记
        //监听打开
        EventCenter.Instance.Register(GameEvent.UI_OpenGrabPanel,OnOpenGrabPanel);
        //监听关闭
        EventCenter.Instance.Register(GameEvent.Game_GrabLandlord,OnGrabFinish);
        //.onClick:Unity UI 按钮自带的事件，当用户点击按钮时会触发
        grabButton.onClick.AddListener(OnClickGrab);
        //.AddListener():添加监听器	把你要执行的"方法"注册到这个事件上，一点击就会调用
        passButton.onClick.AddListener(OnClickPass);
        gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        EventCenter.Instance.UnRegister(GameEvent.Game_GrabLandlord, OnGrabFinish);
        EventCenter.Instance.UnRegister(GameEvent.UI_OpenGrabPanel,OnOpenGrabPanel);
        grabButton.onClick.RemoveListener(OnClickGrab);
        passButton.onClick.RemoveListener(OnClickPass);
    }
    private void OnOpenGrabPanel(object param)
    {
        Open();
    }
    private void OnGrabFinish(object param)
    {
        Close();
    }

    /// <summary>点击"抢地主"：转发给LandlordManager。</summary>
    private void OnClickGrab()
    {
        LandlordManager.Instance.PlayerChooseGrab(true);
    }
    
    /// <summary>
    /// 点击不抢回调
    /// </summary>
    private void OnClickPass()
    {
        LandlordManager.Instance.PlayerChooseGrab(false);
    }
}
