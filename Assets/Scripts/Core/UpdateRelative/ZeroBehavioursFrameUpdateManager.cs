
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.DataStructure;
using Core.UpdateRelative;
using TrueSync;
using UnityEngine;

namespace Core.UpdateController
{
    //这玩意只管更新，不管生命周期，那没事了
    public class FrameUpdateManager<T> where T : IManualUpdate,IComparable<T>
    {
        public BufferedCollection<LinkedList<T>,SortedSet<T>,T> manualUpdates = new BufferedCollection<LinkedList<T>,SortedSet<T>,T>(Enum.GetValues(typeof(ManualScriptExecutionOrderEnum)).Length);
        private HashSet<T> readyToRemove = new HashSet<T>(), readyToRemoveLastFrame = new HashSet<T>();
        /// <summary>
        /// 保存当前帧跑完后被清除组件的集合，供给其他需要这些脚本的地方使用
        /// </summary>
        public List<T> removedSavedThisFrame = new List<T>();
        public bool IsActive { get; private set; } = true;


        //没有Fixed的用途，Fixed本身也不需要同步
        //个人感觉对象池那边的更新只需要来个Dictionary判断一下这个对象是哪种东西就行，不需要单独给它开一个更新的函数，我觉得放在这里面统一更新就行
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void addUpdate(T tmp)
        {
            //Debug.Log("Awake: "+tmp);
            manualUpdates.buffer.Add(tmp);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool removeUpdate(T tmp)
        {
            bool ret = readyToRemove.Add(tmp);
            bool ret2 = readyToRemoveLastFrame.Add(tmp);
            //如果更新正好在它后面，这一帧就可以删掉
            return ret || ret2;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasRemoved(T tmp)
        {
            return (tmp == null || tmp.Equals(null) || tmp.Destroyed);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool checkNull(T x)
        {
            return x==null||x.ManualCheckNull();
        }
        /// <summary>
        /// 逻辑帧更新
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PerformFrame()
        {
            if (!IsActive)
            {
                Debug.Log("FrameUpdateManager: Skip frame update - manager is inactive");
                return;
            }
            readyToRemoveLastFrame.Clear();//清空上一帧需要删除过的东西，因为都已经删完了
            HashSet<T> __tmp__ = readyToRemoveLastFrame;
            readyToRemoveLastFrame = readyToRemove;
            readyToRemove = __tmp__;//交换两个集合，无gc处理新增集合的问题
            foreach(var currentUpdateGroup in manualUpdates.current)
            {
                LinkedListNode<T> updateNode = currentUpdateGroup.First, nextNode;
                T manualUpdate;
                while (updateNode != null)
                {
                    nextNode = updateNode.Next;
                    manualUpdate = updateNode.Value;
                    if (checkNull(manualUpdate) || readyToRemoveLastFrame.Contains(manualUpdate) || manualUpdate.ReadyDestroy)
                    {//如果已经设置了要删掉了，那就不用更新，直接删除
                    
                        //Debug.Log(updateNode.Value + " " + updateNode + " " + updateNode.List);
                        currentUpdateGroup.Remove(updateNode);
                        //由系统删除的时候设置
                        if (manualUpdate != null)
                        {
                            manualUpdate.Destroyed = true;
                        }
                        removedSavedThisFrame.Add(manualUpdate);//添加进当前主逻辑帧已销毁名单
                        updateNode = nextNode;//这里记得更新，不然无限循环力
                        continue;
                    }
                    //速度设置部分删除，没有实际用途，而且在代码中很难控制，每帧更新一次即可
                    manualUpdate.ManualFixedUpdate();
                    updateNode = nextNode;
                }
            }
        }
        /// <summary>
        /// 下一帧前准备函数，把所有update推入更新队列
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NextFramePreparation()
        {
            //把在update中增加的东西增加进update列表里面
            if (manualUpdates.buffer.Count != 0)
            {//fixedUpdate增加时update也会增加，所以不用管。
                SortedSet<T> currentBuffer=null;
                //直到所有的新创建物体都清空
                while (manualUpdates.buffer.Count > 0)
                {
                    currentBuffer = manualUpdates.buffer;
                    manualUpdates.SwapBuffer();//新加的元素放到交换过的buffer里面
                    foreach(var node in currentBuffer)
                    {
                        if (checkNull(node))
                        {
                            continue;
                        }
                        manualUpdates.current[(int)node.ScriptExecutionOrder].AddLast(node);
                        node.ManualStart();
                    }
                    currentBuffer.Clear();
                }
            }
            removedSavedThisFrame.Clear();
        }
        /// <summary>
        /// 渲染帧，由于现在取消速度的设置，比例永远是一样的，不用每次都算，方便许多
        /// </summary>
        /// <param name="ratio">比例</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PerformRenderFrame(float ratio)
        {
            foreach (var group in manualUpdates.current)
            {
                foreach (T manualUpdate in group)
                {
                    if (checkNull(manualUpdate) || manualUpdate.ReadyDestroy)
                    {
                        continue;
                    }
                    manualUpdate.ManualUpdate(ratio);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pause()
        {
            if (!IsActive)
            {
                Debug.Log("FrameUpdateManager: Skip pause - manager is inactive");
                return;
            }

            IsActive = false;
            foreach (var group in manualUpdates.current)
            {
                foreach (T manualUpdate in group)
                {
                    if (checkNull(manualUpdate))
                    {
                        continue;
                    }

                    manualUpdate.OnPause();
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resume()
        {
            if (IsActive)
            {
                Debug.Log("FrameUpdateManager: Skip resume - manager is active");
                return;
            }
            IsActive = true;
            foreach (var group in manualUpdates.current)
            {
                foreach (T manualUpdate in group)
                {
                    if (checkNull(manualUpdate)) { continue; }
                    manualUpdate.OnResume();
                }
            }
        }
    }
}