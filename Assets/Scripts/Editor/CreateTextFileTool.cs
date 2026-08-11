using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

//using System.IO:提供文件读写相关的 API，比如创建文件、获取路径等
public class CreateTextFileTool 
{
    //"Assets/Create/Text文件":菜单路径。表示在 Project 窗口右键 → Create → Text文件
    //false:是否验证。false 表示不需要验证方法，直接可用
    //80:菜单显示顺序。数值越小越靠上，80 在 C# Script（约50）下面一点
    [MenuItem("Assets/Create/Json文件", false, 80)]
    static void CreateNewFile()
    {
        //AssetDatabase.GetAssetPath:获取该资源在项目中的路径（相对于 Assets 文件夹）
        //Selection.activeObject:当前在 Project 窗口中选中的资源对象
        //path:存储获取到的路径
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        //string.IsNullOrEmpty(path)：检查 path 是否为空或 null
        if (string.IsNullOrEmpty(path))
        {
            path = "Assets";
        }
        else if(!AssetDatabase.IsValidFolder(path))
        {
            path=Path.GetDirectoryName(path);
        }
        //AssetDatabase.GenerateUniqueAssetPath:检测重名，生成一个不会重复的路径
        string filePath = AssetDatabase.GenerateUniqueAssetPath(path + "/新建文本.json");
        //WriteAllText:System.IO 提供的方法，创建文件并写入文本
        File.WriteAllText(filePath, "");
        AssetDatabase.Refresh(); //刷新 Unity 资源数据库
    }
}
