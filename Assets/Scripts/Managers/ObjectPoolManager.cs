using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池管理器
/// </summary>
public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    public Dictionary<string, Queue<GameObject>> poolDir;
    /// <summary>
    /// 回收物体的父节点，所有正在池子里"休眠"的物体会挂在它下面
    /// </summary>
    public Transform poolRoot;
    protected override void Awake()
    {
        base.Awake();
        poolDir = new Dictionary<string, Queue<GameObject>>();
        if (poolRoot == null)
        {
            //在场景里创建一个空节点，专门存放回收的物体
            GameObject rootObj = new GameObject("PoolRoot");
            DontDestroyOnLoad(rootObj);
            poolRoot = rootObj.transform;
        }
    }
    /// <summary>
    /// 从池子里取出一个物体
    /// </summary>
    /// <param name="name">池子名字</param>
    /// <param name="prefab">预制体：如果池子空了，用它来Instantiate</param>
    /// <param name="parent">取出来的物体挂到哪个父节点下，不传就挂在根上</param>
    public GameObject GetGameObject(string name,GameObject prefab,Transform parent = null)
    {
        GameObject obj;
        if (!poolDir.ContainsKey(name))
        {
            poolDir[name] = new Queue<GameObject>();
        }
        if (poolDir[name].Count > 0)
        {
            obj = poolDir[name].Dequeue();
        }
        else
        {
            //没了就克隆
            obj=GameObject.Instantiate(prefab);
        }
        if (parent != null)
        {
            //SetParent(parent)：把物体挂到指定父节点下
            obj.transform.SetParent(parent);
        }
        obj.SetActive(true);
        return obj;
    }
    /// <summary>
    /// 把用完的物体还回池子
    /// </summary>
    /// <param name="name">回收物体名字</param>
    /// <param name="obj">要回收的物体</param>
    public void Recycle(string name,GameObject obj)
    {
        obj.SetActive(false);
        if (!poolDir.ContainsKey(name))
        {
            poolDir[name] = new Queue<GameObject>();
        }
        poolDir[name].Enqueue(obj);
        obj.transform.SetParent(poolRoot);
    }
    /// <summary>
    /// 预热池子：提前创建好count个物体存着，防止游戏中途Instantiate 产生卡顿
    /// </summary>
    /// <param name="name">池子名字</param>
    /// <param name="prefab">预制体</param>
    /// <param name="count">预加载数量</param>
    public void Prewarm(string name,GameObject prefab,int count)
    {
        if (!poolDir.ContainsKey(name))
        {
            poolDir[name] = new Queue<GameObject>();
        }
        for(int i = 0; i < count; i++)
        {
            GameObject gameObject1 = GameObject.Instantiate(prefab);
            gameObject1.SetActive(false);
            gameObject1.transform.SetParent(poolRoot);
            poolDir[name].Enqueue(gameObject1);
        }
    }
    /// <summary>
    /// 清空某个池子的所有物体（场景切换时用）
    /// </summary>
    /// <param name="name"></param>
    public void ClearPool(string name)
    {
        if (poolDir.ContainsKey(name))
        {
            while (poolDir[name].Count > 0)
            {
                GameObject obj = poolDir[name].Dequeue();
                Destroy(obj);
            }
            poolDir.Remove(name);
        }
    }
    /// <summary>
    /// 清空所有池子
    /// </summary>
    public void ClearAll()
    {
        //遍历字典的每一个键值对
        foreach(var kvp in poolDir)
        {
            Queue<GameObject> queue = kvp.Value;
            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                Destroy(obj);
            }
        }
        poolDir.Clear(); // 清空整个字典
    }
}
