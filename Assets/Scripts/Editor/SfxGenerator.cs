using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 扑克音效生成器 —— 用代码合成 WAV 文件，不依赖任何外部素材。
///
/// 【为什么用生成而不是下载】
/// 网上抓的音效授权五花八门，商业项目用错就是侵权。程序合成的波形是我们自己产生的，
/// 版权干净，而且体积小、可以随时调参数重生成。
/// 想换风格就改下面的频率/衰减系数，重新点一次菜单即可。
///
/// 【用法】Unity 菜单栏 → Tools → 生成扑克音效
/// 生成结果在 Assets/Resources/Audio/ 下，文件名和 GameAudio 里的路径常量一一对应。
/// </summary>
public static class SfxGenerator
{
    private const int SAMPLE_RATE = 44100;
    private const int BIT_DEPTH = 16;
    private const string OUTPUT_DIR = "Assets/Resources/Audio";

    /// <summary>用固定种子，保证每次生成的结果完全一样（方便对比调参效果）</summary>
    private static System.Random rng = new System.Random(20240920);

    [MenuItem("Tools/生成扑克音效")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(OUTPUT_DIR);

        WriteWav("SFX_Click", SynthClick());
        WriteWav("SFX_Select", SynthSelect());
        WriteWav("SFX_Deal", SynthDeal());
        WriteWav("SFX_PlayCard", SynthPlayCard());
        WriteWav("SFX_Bomb", SynthBomb());
        WriteWav("SFX_Win", SynthWin());
        WriteWav("SFX_Lose", SynthLose());

        AssetDatabase.Refresh();
        Debug.Log($"[SfxGenerator] 已生成 7 个音效到 {OUTPUT_DIR}，直接运行游戏就能听到");
    }

    #region 音效设计
    // 每个音效就是一条"时间 t → 振幅"的曲线。
    // 通用套路：一个短促的激励源（正弦音或噪声）× 一条指数衰减包络。

    /// <summary>按钮点击：高频"嗒"，极短</summary>
    private static float[] SynthClick()
    {
        return Render(0.05f, (t, i) =>
        {
            float env = Mathf.Exp(-70f * t);
            float tone = Mathf.Sin(2f * Mathf.PI * 1600f * t);
            return (tone * 0.7f + Noise() * 0.35f) * env;
        });
    }

    /// <summary>选牌：频率上滑的"叮"，听起来像"提起来"</summary>
    private static float[] SynthSelect()
    {
        const float dur = 0.10f;
        return Render(dur, (t, i) =>
        {
            float env = Mathf.Exp(-32f * t);
            //频率从 900 滑到 1600，比固定频率更有"选中"的上扬感
            float freq = Mathf.Lerp(900f, 1600f, t / dur);
            return Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f * env;
        });
    }

    /// <summary>发牌：纸牌摩擦的"唰"，白噪声快速衰减</summary>
    private static float[] SynthDeal()
    {
        return Render(0.13f, (t, i) =>
        {
            float env = Mathf.Exp(-26f * t);
            return Noise() * Mathf.Exp(-9f * t) * env * 0.7f;
        });
    }

    /// <summary>出牌："啪"，低频闷响 + 起始的噪声撞击</summary>
    private static float[] SynthPlayCard()
    {
        return Render(0.16f, (t, i) =>
        {
            float env = Mathf.Exp(-30f * t);
            float thud = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.5f;
            float hit = Noise() * Mathf.Exp(-45f * t);
            return (thud + hit) * env;
        });
    }

    /// <summary>炸弹：低频轰鸣 + 长尾噪声，明显比其他音效长</summary>
    private static float[] SynthBomb()
    {
        return Render(0.9f, (t, i) =>
        {
            float env = Mathf.Exp(-4.5f * t);
            float low = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.7f;
            float tail = Noise() * Mathf.Exp(-6f * t) * 0.8f;
            return (low + tail) * env;
        });
    }

    /// <summary>胜利：C5-E5-G5-C6 上行琶音</summary>
    private static float[] SynthWin()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
        return Arpeggio(notes, 0.11f, 7f, 0.35f, 0.80f);
    }

    /// <summary>失败：C5-G4-E4-C4 下行，衰减更快，听起来"泄气"</summary>
    private static float[] SynthLose()
    {
        float[] notes = { 523.25f, 392.00f, 329.63f, 261.63f };
        return Arpeggio(notes, 0.13f, 10f, 0.35f, 0.95f);
    }

    /// <summary>
    /// 琶音合成：每个音符从自己的起始时刻开始发声并自然衰减，全部叠加。
    /// 【为什么不逐个音符切换】那样旧音符会被硬切断，产生"咔"的爆音；
    /// 叠加的话每个音都完整衰减，衔接顺滑。
    /// </summary>
    private static float[] Arpeggio(float[] notes, float step, float decay, float gain, float duration)
    {
        return Render(duration, (t, i) =>
        {
            float sum = 0f;
            for (int n = 0; n < notes.Length; n++)
            {
                float start = n * step;
                if (t < start) continue;
                float lt = t - start;
                sum += Mathf.Sin(2f * Mathf.PI * notes[n] * lt) * Mathf.Exp(-decay * lt);
            }
            return sum * gain;
        });
    }

    #endregion

    #region 合成与写文件

    /// <summary>按给定的"时间 → 振幅"函数渲染一段单声道波形</summary>
    private static float[] Render(float duration, System.Func<float, int, float> gen)
    {
        int count = Mathf.CeilToInt(duration * SAMPLE_RATE);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            //钳到 [-1,1]，超出范围的采样值写进 16bit 会溢出成刺耳的爆音
            samples[i] = Mathf.Clamp(gen(t, i), -1f, 1f);
        }
        return samples;
    }

    /// <summary>白噪声，用固定种子的随机数发生器</summary>
    private static float Noise()
    {
        return (float)(rng.NextDouble() * 2.0 - 1.0);
    }

    /// <summary>把浮点波形写成标准 16bit PCM 的 WAV 文件</summary>
    private static void WriteWav(string fileName, float[] samples)
    {
        string path = Path.Combine(OUTPUT_DIR, fileName + ".wav");
        int dataSize = samples.Length * (BIT_DEPTH / 8);
        int byteRate = SAMPLE_RATE * (BIT_DEPTH / 8); //单声道：采样率 × 声道数(1) × 位深/8

        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter w = new BinaryWriter(fs))
        {
            // ---- RIFF 头 ----
            WriteTag(w, "RIFF");
            w.Write(36 + dataSize);   //后面所有内容的字节数
            WriteTag(w, "WAVE");

            // ---- fmt 块：描述音频格式 ----
            WriteTag(w, "fmt ");
            w.Write(16);              //fmt 块长度，PCM 固定 16
            w.Write((short)1);        //格式：1 = 无压缩 PCM
            w.Write((short)1);        //声道数：1 = 单声道
            w.Write(SAMPLE_RATE);
            w.Write(byteRate);
            w.Write((short)(BIT_DEPTH / 8)); //块对齐
            w.Write((short)BIT_DEPTH);

            // ---- data 块：真正的采样数据 ----
            WriteTag(w, "data");
            w.Write(dataSize);
            for (int i = 0; i < samples.Length; i++)
            {
                //float [-1,1] → short [-32767,32767]
                w.Write((short)(samples[i] * 32767f));
            }
        }
    }

    private static void WriteTag(BinaryWriter w, string tag)
    {
        w.Write(Encoding.ASCII.GetBytes(tag));
    }

    #endregion
}
