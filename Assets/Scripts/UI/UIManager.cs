using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    //面板注册表
    public Dictionary<string, BasePanel> panelDict = new Dictionary<string, BasePanel>();
    /// <summary>
    /// 添加面板
    /// </summary>
    /// <param name="name">面板名字</param>
    /// <param name="basePanel">面板本身</param>
    public void RegisterPanel(string name, BasePanel basePanel)
    {
        if (basePanel != null)
        {
            panelDict[name] = basePanel;
        }
        else
        {
            Debug.LogError($"[UIManager]注册面板失败：{basePanel}的实例为空");
            return;
        }
    }

    /// <summary>
    /// 打开面板(按名字)
    /// </summary>
    /// <param name="name"></param>
    public void OpenPanel(string name)
    {
        BasePanel panel = GetPanel(name);
        if (panel != null)
        {
            panel.Open();
        }
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    /// <param name="name"></param>
    public void ClosePanel(string name)
    {
        BasePanel basePanel = GetPanel(name);
        if (basePanel != null)
        {
            basePanel.Close();
        }
    }

    /// <summary>
    /// 获取面板
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private BasePanel GetPanel(string name)
    {
        if (panelDict.TryGetValue(name, out BasePanel panel))
        {
            return panel;
        }
        Debug.LogError($"[UIManager]找不到面板：{name}，请检查是否已RegisterPanel");
        return null;
    }
}

