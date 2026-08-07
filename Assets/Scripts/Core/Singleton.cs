using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型单例基类
/// </summary>
/// <typeparam name="T"></typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                //创建物体，物体名字就用脚本类名
                GameObject gameObject = new GameObject(typeof(T).Name);
                _instance=gameObject.GetComponent<T>();
                //设置物体跨场景不销毁，切换场景不会被删掉
                DontDestroyOnLoad(gameObject);
            }
            return _instance;
        }
    }
    protected virtual void Awake()
    {
        //如果还没有实例，把自己赋值给_instance
        if ( _instance == null)
        {
            _instance=this as T;
            DontDestroyOnLoad(_instance);
        }
        else
        {
            Destroy( _instance );
        }
    }
}
