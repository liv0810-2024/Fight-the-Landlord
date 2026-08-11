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
    private Dictionary<string, object> cacheDict = new Dictionary<string, object>();
  /// <summary>
  /// 同步加载
  /// </summary>
  /// <typeparam name="T">加载类型</typeparam>
  /// <param name="path">加载路径</param>
  /// <returns></returns>
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
        if (cacheDict.ContainsKey(path))
        {
            onLoaded?.Invoke(cacheDict[path] as T);
            return;
        }
        else
        {
            //缓存没有，启动协程去异步加载
            StartCoroutine(LoadAsyncCoroutine(path, onLoaded));
        }
    }
    /// <summary>
    /// 协程异步加载
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="onLoaded"></param>
    /// <returns></returns>
    private IEnumerator LoadAsyncCoroutine<T>(string path,Action<T> onLoaded) where T : UnityEngine.Object
    {
        //Resources.LoadAsync：Unity内置的异步加载 API
        //它会在后台线程加载资源 ，不影响主线程渲染
        //ResourceRequest 是 Unity 中用于异步加载 Resources 目录资源的一个类，继承自 AsyncOperation
        ResourceRequest request = Resources.LoadAsync<T>(path);
        //每帧 Unity 会检查 request.isDone 是否为 true
        yield return request;
        T result = request.asset as T;
        if (result != null)
        {
            cacheDict[path] = result;
            onLoaded?.Invoke(result);
        }
        else
        {
            Debug.Log($"[ResManager] 异步加载失败！路径：Resources/{path}，请确认文件存在且类型正确");
        }
    }
}
