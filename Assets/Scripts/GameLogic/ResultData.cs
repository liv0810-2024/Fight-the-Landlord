using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResultData 
{
    /// <summary>
    /// 谁先出完
    /// </summary>
    public int winnerIndex; 
    /// <summary>
    /// 本局是否地主获胜
    /// </summary>
    public bool isLandlordWin; 
    public int multiple; 
    public int baseScore;
    public int[] roundScores; //本局得分
    public int[] totalScores; //累计得分
    /// <summary>地主索引，UI 用它显示"你是地主/农民"</summary>
    public int landlordIndex = -1;
    /// <summary>是否触发春天/反春天（倍数已翻倍）</summary>
    public bool isSpring;
}
