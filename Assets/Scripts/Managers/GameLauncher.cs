using System.Collections;
using UnityEngine;

/// <summary>
/// 大厅 → 对局的唯一入口。
///
/// 【为什么单独抽一个类】"点开始游戏"要做的不止一件事：收大厅面板、等数据、发牌。
/// 这套动作以后只会多不会少，集中在一处比散落在按钮回调里安全。
///
/// 【架构说明】大厅和对局现在同处 GameScene，不切场景。
/// 之前写的 SceneManager.LoadScene 已经删掉 —— 当前场景本来就是 GameScene，
/// 再 Load 一次会把自己重新加载一遍：封面又弹出来、牌又发一次，直接死循环。
/// 以后真要把大厅独立成 HallScene，再把这里换回 LoadScene 即可。
/// </summary>
public class GameLauncher : Singleton<GameLauncher>
{
    /// <summary>开始游戏。</summary>
    public void EnterGame()
    {
        // 【必须先收大厅】三个面板和牌桌在同一个场景里，不收掉会一直盖在牌上面。
        // 关面板不需要判空：UIManager 找不到会自己 LogError，不必在外面重复判断。
        UIManager.Instance.ClosePanel(UIName.CoverPanel);
        UIManager.Instance.ClosePanel(UIName.MainMenuPanel);
        UIManager.Instance.ClosePanel(UIName.SettingsPanel);

        StartCoroutine(EnterGameRoutine());
    }

    private IEnumerator EnterGameRoutine()
    {
        // 【为什么等】玩家手速再快，也可能赶在配置表读完之前点进来，那时发牌会拿到空数据。
        // 等这一下成本几乎为零，却能彻底避免那种"偶尔开局没牌"的偶发 bug。
        yield return new WaitUntil(() => DataManager.Instance.isDataLoaded);
        GameMainManager.Instance.StartGame();
    }

    /// <summary>
    /// 退出游戏。
    /// 【编辑器里点它没反应是正常的】Application.Quit() 在播放模式下是空操作，
    /// 只有打包出来的可执行文件才真的会退出。所以编辑器里改用停播放模式代替。
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
