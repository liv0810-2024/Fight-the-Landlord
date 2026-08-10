using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// 简易资源加载管理器
/// 加上缓存机制，避免同一个资源反复从磁盘读取.
/// </summary>
public class ResManager : Singleton<ResManager>
{
    /// <summary>
    /// 资源缓存字典
    /// </summary>
    private Dictionary<string, object> cacheDict = new Dictionary<string, object>;
    public T Load<T>(string path) where T : UnityEngine.Object
    {
        if (cacheDict.ContainsKey(path))
        {
            return cacheDict[path] as T;
        }
        T resources=Resources.Load<T>(path);
        if (resources == null)
        {
            Debug.LogError($"[Resources]加载资源失败！路径：Resources/{path}，请检查文件是否存在");
            return null;
        }
        cacheDict[path] = resources;
        return resources;
    }
    public void LoadAsync<T>(string path,Action<T> onLoaded) where T : UnityEngine.Object
    {

    }
    private IEnumerator LoadAsyncCoroutine<T>(string path,Action<T> onLoaded) where T : UnityEngine.Object
    {

    }
}
