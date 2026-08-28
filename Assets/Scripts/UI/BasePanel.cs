using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 面板基类：所有面板继承它，统一"打开 / 关闭"的行为。
/// </summary>
public class BasePanel : MonoBehaviour
{
   public bool IsOpen { get;private set;  }

    /// <summary>
    /// 面板打开方法
    /// </summary>
   public void Open()
    {
        gameObject.SetActive(true);
        IsOpen = true;
        OnOpen();
    }

    /// <summary>
    /// 面板关闭
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        OnClose();
        gameObject.SetActive(false);
    }
    /// <summary>
    /// 给子类重写的打开之后的方法操作
    /// </summary>
    protected virtual void OnOpen()
    {

    }
    /// <summary>
    /// 给子类重写的关闭之后的方法操作
    /// </summary>
    protected virtual void OnClose()
    {

    } 
}
