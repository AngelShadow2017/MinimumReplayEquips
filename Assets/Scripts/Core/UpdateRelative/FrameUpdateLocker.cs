using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.UpdateController
{
    
    /// <summary>
    /// 管理所有帧同步锁的类
    /// </summary>
    public class FrameLockManager
    {
        public enum FrameLockType
        {
            Default,
        }
        
        private Dictionary<FrameLockManager.FrameLockType, IFrameLock> _locks = new Dictionary<FrameLockManager.FrameLockType, IFrameLock>();
        
        /// <summary>
        /// 是否所有锁都已打开，允许帧更新
        /// </summary>
        public bool AllLocksOpen => _locks.Count == 0 || _locks.Values.All(l => l.IsOpen);
        
        /// <summary>
        /// 添加一个锁
        /// </summary>
        /// <param name="frameLock">要添加的锁</param>
        public void AddLock(IFrameLock frameLock)
        {
            if (!_locks.ContainsKey(frameLock.Id))
            {
                _locks.Add(frameLock.Id, frameLock);
            }
            else
            {
                Debug.LogWarning($"锁 {frameLock.Id} 已存在，无法重复添加");
            }
        }
        
        /// <summary>
        /// 移除一个锁
        /// </summary>
        /// <param name="lockId">要移除的锁的ID</param>
        public void RemoveLock(FrameLockManager.FrameLockType lockId)
        {
            if (_locks.ContainsKey(lockId))
            {
                _locks.Remove(lockId);
            }
        }
        
        /// <summary>
        /// 获取一个锁
        /// </summary>
        /// <param name="lockId">锁的ID</param>
        /// <returns>对应的锁，如果不存在则返回null</returns>
        public IFrameLock GetLock(FrameLockManager.FrameLockType lockId)
        {
            return _locks.TryGetValue(lockId, out var frameLock) ? frameLock : null;
        }
        
        /// <summary>
        /// 打开特定的锁
        /// </summary>
        /// <param name="lockId">要打开的锁的ID</param>
        public void OpenLock(FrameLockManager.FrameLockType lockId)
        {
            var frameLock = GetLock(lockId);
            frameLock?.Open();
        }
        
        /// <summary>
        /// 关闭特定的锁
        /// </summary>
        /// <param name="lockId">要关闭的锁的ID</param>
        public void CloseLock(FrameLockManager.FrameLockType lockId)
        {
            var frameLock = GetLock(lockId);
            frameLock?.Close();
        }
        
        /// <summary>
        /// 清除所有锁
        /// </summary>
        public void ClearAllLocks()
        {
            _locks.Clear();
        }

        /// <summary>
        /// 更新所有锁的状态
        /// 这个方法应该在每帧末尾调用
        /// </summary>
        public void UpdateLocksState()
        {
            foreach (var lockPair in _locks)
            {
                lockPair.Value.UpdateState();
            }
        }
    }

    /// <summary>
    /// 帧同步锁接口，用于控制游戏更新流程
    /// </summary>
    public interface IFrameLock
    {
        /// <summary>
        /// 锁的唯一标识符
        /// </summary>
        FrameLockManager.FrameLockType Id { get; }
        
        /// <summary>
        /// 锁的状态，true表示锁已打开（允许更新），false表示锁已关闭（阻止更新）
        /// </summary>
        bool IsOpen { get; }
        
        /// <summary>
        /// 打开锁，允许帧更新
        /// </summary>
        void Open();
        
        /// <summary>
        /// 关闭锁，阻止帧更新
        /// </summary>
        void Close();
        /// <summary>
        /// 更新状态
        /// </summary>
        void UpdateState();
    }
    
    /// <summary>
    /// 基础的同步锁实现
    /// </summary>
    public class DemoLocker : IFrameLock
    {
        public FrameLockManager.FrameLockType Id => FrameLockManager.FrameLockType.Default;
        // 当前实际状态
        public bool IsOpen { get; private set; }
        
        // 下一帧将要更新的状态
        private bool _nextFrameState;
        
        // 是否有待处理的状态更新
        private bool _hasStateChange = false;
        
        public DemoLocker(bool initiallyOpen = true)
        {
            IsOpen = initiallyOpen;
            _nextFrameState = initiallyOpen;
        }
        
        /// <summary>
        /// 设置锁在下一帧末尾打开
        /// </summary>
        public void Open()
        {
            _nextFrameState = true;
            _hasStateChange = true;
        }
        
        /// <summary>
        /// 设置锁在下一帧末尾关闭
        /// </summary>
        public void Close()
        {
            _nextFrameState = false;
            _hasStateChange = true;
        }
        
        /// <summary>
        /// 在帧结束时更新锁的实际状态
        /// 此方法应由FrameLockManager在每帧末尾调用
        /// </summary>
        public void UpdateState()
        {
            if (_hasStateChange)
            {
                IsOpen = _nextFrameState;
                _hasStateChange = false;
            }
        }
    }
}