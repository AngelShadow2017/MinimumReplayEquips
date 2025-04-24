using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.UpdateController;
using UnityEngine;

namespace Core.UpdateRelative
{
    public class MonoBehaviourManager : MonoBehaviour
    {
        public static GameObject thisObject;
        public static MonoBehaviourManager thisManager;
        public static readonly int logicFrameRate = (int)Math.Round(1f/Time.fixedDeltaTime);
        public double lastLogicTime = 0;
        public readonly static float updateOnceTimeFloat = Time.fixedDeltaTime;
        private FrameUpdateManager<IManualUpdate> frameUpdateManager = new FrameUpdateManager<IManualUpdate>();
        private LinkedList<UnityEngine.Object> TmpDestroyer = new LinkedList<UnityEngine.Object>();//在当前帧结束后统一清除对象
        // 新增锁管理器
        private FrameLockManager _frameLockManager = new FrameLockManager();

        public static FrameUpdateManager<IManualUpdate> updateManager => thisManager == null ? null : thisManager.frameUpdateManager;

        // 提供对锁管理器的访问
        public static FrameLockManager LockManager => thisManager == null ? null : thisManager._frameLockManager;

        void Awake()
        {
            if (thisObject != gameObject && thisObject)
            {
                Destroy(thisObject);
            }
            thisObject = gameObject;
            thisManager = this;
            DontDestroyOnLoad(gameObject);
        }
        
        public static void DestroyAnObjectAtThisFrameEnd(UnityEngine.Object obj)
        {
            thisManager.TmpDestroyer.AddLast(obj);
        }
        private void Update()
        {
            bool canUpdate = _frameLockManager.AllLocksOpen;
            
            if (!canUpdate)
            {
                return;
            }
            float curRatio=(float)(Time.timeAsDouble-lastLogicTime)/updateOnceTimeFloat;
            frameUpdateManager.PerformRenderFrame(curRatio);
            //更新你的动画插件，比如我的就是Dotween，用DoTween.ManualUpdate(Time.deltaTime什么的)。
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureObj()
        {
            if (!thisObject)
            {
                new GameObject("[__MonoBehaviourManager__IMPORTANT]").AddComponent<MonoBehaviourManager>();
            }

            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }

        private void FixedUpdate()
        {
            bool canUpdate = _frameLockManager.AllLocksOpen;
            if (!canUpdate)
            {
                // 如果有任何锁未打开，暂停更新
                if (frameUpdateManager.IsActive)
                {
                    frameUpdateManager.Pause();
                }
                _frameLockManager.UpdateLocksState();
                return;
            }
            else if (!frameUpdateManager.IsActive)
            {
                // 所有锁都打开，恢复更新
                frameUpdateManager.Resume();
            }
            lastLogicTime = Time.fixedTimeAsDouble;
            frameUpdateManager.PerformFrame();
            if (TmpDestroyer.Count > 0)
            {
                //待会看下这里
                LinkedListNode<UnityEngine.Object> __updNode = TmpDestroyer.First;
                LinkedListNode<UnityEngine.Object> __nxtNode;
                while (__updNode != null)
                {
                    __nxtNode = __updNode.Next;
                    DestroyImmediate(__updNode.Value);
                    __updNode = __nxtNode;
                }
                TmpDestroyer.Clear();
            }
            //把在update中增加的东西增加进update列表里面
            frameUpdateManager.NextFramePreparation();
            //如果啥都没有就销毁掉自己
            if (frameUpdateManager.manualUpdates.ItemCount==0)
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}