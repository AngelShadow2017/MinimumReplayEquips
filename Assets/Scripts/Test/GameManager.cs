using System;
using System.Collections.Generic;
using Core.UpdateController;
using Core.UpdateRelative;
using MessagePack;
using TrueSync;
using UnityEngine;

// ***************************************************************************
//GameManager.cs
//作者：泠漪
//日期：24/04/2025
//描述：一个【带非常简单的replay的（实际中不要写得这么屎拜托！！！）】游戏管理核心类，负责以下核心功能：
//      1. 玩家输入的录制与回放系统
//      2. 随机数种子管理确保可重复性
//      3. 游戏状态管理（录制/回放模式切换）
//      4. 物体平均位置计算（用于调试/数据分析）
//      5. 演示模式锁定控制（暂停/恢复游戏）
//
//设计说明：
//  - 使用MessagePack进行高效二进制序列化
//  - 通过帧号与输入数据的字典结构实现精准回放
//  - 随机数种子绑定确保每次回放结果一致
//  - U键保存录制，L键控制随时锁定（暂停），P键快放/恢复，M键慢放/恢复
// ***************************************************************************

[MessagePackObject]
public struct InputData
{
    [Key(0)] public int frame;          // 当前帧编号
    [Key(1)] public TSVector2 input;    // 玩家方向输入（X/Y轴）
    [Key(2)] public bool shoot;         // 射击按键状态
}

[MessagePackObject]
public class Recorder
{
    [Key(0)] public int seed;           // 随机种子保证可重复性
    [Key(1)] public Dictionary<int, InputData> frames = new Dictionary<int, InputData>(); // 帧数据字典（帧号→输入数据）
}

public class GameManager : ZeroBehaviour
{
    #region 单例模式
    public static GameManager Instance { get; private set; } // 单例模式访问入口
    #endregion

    #region 核心配置
    [Header("Settings")]
    public int globalSeed = 12345;      // 全局随机种子
    Recorder recorder = new Recorder(); // 输入记录器
    private int frame = 0;              // 当前游戏帧计数
    private bool replaying = false;     // 是否处于回放模式
    public InputData CurrentInput { get; private set; } // 当前帧输入数据
    public bool loadReplay = false;     // 是否在启动时加载回放
    string path = "./recorder.replay";  // 录制文件保存路径
    private bool finished = false;      // 是否完成录制/回放
    DemoLocker locker = new DemoLocker(); // 锁定控制器Demo，可以自己写一个其他的来作为暂停，或者其他的锁都可以的。。。
    #endregion

    #region 初始化逻辑
    public override void ManualAwake()
    {
        // 单例模式初始化
        if(Instance != null)
        {
            DestroyImmediate(Instance.gameObject);
        }
        Instance = this;

        // 随机种子初始化
        globalSeed = (int)DateTime.Now.Ticks; // 新游戏使用当前时间作为种子

        recorder.seed = globalSeed; // 记录当前种子
        InitializeRandom();         // 初始化随机数生成器

        // 回放模式初始化
        if (loadReplay)
        {
            byte[] bytes = System.IO.File.ReadAllBytes(path); // 读取录制文件
            InitReplay(MessagePackSerializer.Deserialize<Recorder>(bytes)); // 反序列化回放数据
        }

        // 添加锁定管理
        MonoBehaviourManager.LockManager.AddLock(locker);
    }
    #endregion

    #region 回放初始化
    public void InitReplay(Recorder rep)
    {
        // 重置所有随机状态
        globalSeed = rep.seed;
        recorder = rep;
        InitializeRandom();

        // 进入回放模式
        replaying = true;
    }
    #endregion
    /// <summary>
    /// 呃呃其实这个是用物体平均位置来做哈希值，用肉眼可见的方法判断replay是否正确。。。
    /// </summary>
    public void outputAveragePosition()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        if (allObjects.Length == 0)
        {
            Debug.Log("没有找到物体！");
            return;
        }

        // 计算平均位置
        TSVector averagePosition = TSVector.zero;
        foreach (GameObject obj in allObjects)
        {
            var comp = obj.GetComponent<FakePosition>();
            if (!comp)
            {
                continue;
            }
            averagePosition += comp.localPosition;
        }
        averagePosition /= allObjects.Length;

        Debug.Log($"当前平均位置：{averagePosition}");

        // 可选：将结果应用到某个物体（如自身）
    }

    private bool lastU = false;
    #region 主循环逻辑
    public override void ManualFixedUpdate()
    {
        frame++; // 帧计数器递增

        if (replaying)
        {
            // 回放模式处理
            if (!recorder.frames.ContainsKey(frame))
            {
                // 回放结束条件
                outputAveragePosition(); // 输出最终位置
                locker.Close();          // 关闭演示模式锁定
                finished = true;         // 标记完成
            }
            else
            {
                // 读取对应的输入数据
                CurrentInput = recorder.frames[frame]; 
            }
            return;
        }

        // 录制模式处理
        bool isUPressed = Input.GetKey(KeyCode.U);
        if (isUPressed && !lastU)
        {
            // U键按下时保存录制
            lastU = true;
            System.IO.File.Delete(path); // 删除旧文件
            System.IO.File.WriteAllBytes(path, MessagePackSerializer.Serialize(recorder)); // 序列化保存
            Debug.Log("Saved to " + path);
            outputAveragePosition(); // 输出当前位置
            locker.Close();          // 关闭演示模式
            finished = true;         // 标记完成
            return;
        }
        else
        {
            lastU = isUPressed; // 更新按键状态
        }

        // 收集当前输入
        CurrentInput = new InputData()
        {
            frame = frame,
            input = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).ToTSVector2(),
            shoot = Input.GetKey(KeyCode.Z)
        };

        // 将输入记录到字典
        if (!replaying)
        {
            recorder.frames.Add(frame,CurrentInput);
        }
    }
    #endregion
    #region 随时暂停。
    public void Update()
    {
        if (!finished)
        {
            // L键控制演示模式锁定
            if (Input.GetKeyDown(KeyCode.L))
            {
                locker.Close();
            }
            else if (Input.GetKeyUp(KeyCode.L))
            {
                locker.Open();
            }
            //快放，慢放。
            if (Input.GetKeyDown(KeyCode.P))
            {
                Time.timeScale = 5;
            }
            else if (Input.GetKeyUp(KeyCode.P))
            {
                Time.timeScale = 1;
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                Time.timeScale = 0.25f;
            }
            else if (Input.GetKeyUp(KeyCode.M))
            {
                Time.timeScale = 1;
            }
        }
    }
    #endregion

    #region 随机数管理
    private void InitializeRandom()
    {
        // 初始化TrueSync的随机数生成器
        TSRandom.instance = TSRandom.New(globalSeed);
    }
    #endregion
}