// GameManager.cs

using System;
using System.Collections.Generic;
using Core.UpdateRelative;
using MessagePack;
using TrueSync;
using UnityEditor;
using UnityEngine;
[MessagePackObject]
public struct InputData
{
    [Key(0)]
    public int frame;
    [Key(1)]
    public TSVector2 input;
    [Key(2)]
    public bool shoot;
}
[MessagePackObject]
public class Recorder
{
    [Key(0)]
    public int seed;
    [Key(1)]
    public Dictionary<int, InputData> frames = new Dictionary<int, InputData>();
}
public class GameManager : ZeroBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    public int globalSeed = 12345;
    Recorder recorder = new Recorder();
    private int frame = 0;
    private bool replaying = false;
    // 当前帧输入状态
    public InputData CurrentInput { get; private set; }
    public bool loadReplay = false;
    string path = "./recorder.replay";
    
    public override void ManualAwake()
    {
        if(Instance!=null){
            DestroyImmediate(Instance.gameObject);
        }
        Instance = this;
        {
            globalSeed = (int)DateTime.Now.Ticks;
        }
        recorder.seed = globalSeed;
        InitializeRandom();
        if (loadReplay)
        {
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            InitReplay(MessagePackSerializer.Deserialize<Recorder>(bytes));
        }
    }

    public void InitReplay(Recorder rep)
    {
        globalSeed = rep.seed;
        recorder = rep;
        InitializeRandom();
        replaying = true;
        // 这里可以根据需要进行初始化
    }

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
    public override void ManualFixedUpdate()
    {
        frame++;
        if (replaying)
        {
            if (!recorder.frames.ContainsKey(frame))
            {
                outputAveragePosition();
                EditorApplication.isPaused = true;
            }
            else
            {
                CurrentInput = recorder.frames?[frame]??new InputData();
            }
            return;
        }
        if (Input.GetKey(KeyCode.U))
        {
            if (!lastU)
            {
                lastU = true;
                // 保存录制
                System.IO.File.Delete(path);
                System.IO.File.WriteAllBytes(path, MessagePackSerializer.Serialize(recorder));
                Debug.Log("Saved to " + path);
                outputAveragePosition();
                EditorApplication.isPaused = true;
            }
        }
        else
        {
            lastU = false;
        }
        // 收集输入
        CurrentInput = new InputData()
        {
            frame = frame,
            input = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            ).ToTSVector2(),
            shoot = Input.GetKey(KeyCode.Z)
        };
        if (!replaying)
        {
            recorder.frames.Add(frame, CurrentInput);
        }

    }

    void InitializeRandom()
    {
        TSRandom.instance = TSRandom.New(globalSeed);
    }
}