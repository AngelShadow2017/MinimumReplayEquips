using System;
using System.Runtime.CompilerServices;
using TrueSync;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.UpdateRelative
{
    public abstract class ZeroBehaviour : MonoBehaviour, IManualUpdate
    {
        private static long global_index = 0;
        private long _internal_creation_index=global_index++;
        public virtual ManualScriptExecutionOrderEnum ScriptExecutionOrder { get; } = ManualScriptExecutionOrderEnum.Default;

        public int CompareTo(IManualUpdate other)
        {
            int result = ScriptExecutionOrder - other.ScriptExecutionOrder;
            if (result == 0)
            {
                if (other is ZeroBehaviour zb)
                {
                    return _internal_creation_index-zb._internal_creation_index > 0 ? 1 : -1;
                }
                return 0;
            }
            return result;
        }

        public virtual void Awake() {
            MonoBehaviourManager.EnsureObj();
            ManualAwake();
            MonoBehaviourManager.updateManager.addUpdate(this);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ManualStart() { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ManualAwake() { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ManualDestroy() { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void OnPause() { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void OnResume() { }
        //手动渲染这一个坐标和下一个坐标之间的插值
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ManualUpdate(float lerpVal = default) { }
        // 执行逻辑帧
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void ManualFixedUpdate() {}
        //同步销毁的方法
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(ZeroBehaviour obj) {
            obj.OnDestroy();
            MonoBehaviour.Destroy(obj);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(GameObject obj)
        {
            MonoBehaviourManager.DestroyAnObjectAtThisFrameEnd(obj);
            //MonoBehaviour.DestroyImmediate(obj);
        }
        public new static void Destroy(Object obj)
        {
            if (obj == null) { return; }
            if (obj is ZeroBehaviour zb)
            {
                zb.OnDestroy();
            }
            MonoBehaviour.Destroy(obj);
        }
        public bool ReadyDestroy { get; } = false;
        public bool Destroyed { get; set; } = false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void OnDestroy()
        {
            if (MonoBehaviourManager.thisManager&& MonoBehaviourManager.updateManager.removeUpdate(this))
            {
                ManualDestroy();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ManualCheckNull()
        {
            return this == null;
        }

        public FakePosition LocalInstantiate(GameObject obj,TSVector position)
        {
            var go = Instantiate(obj);
            var fakePos = go.GetComponent<FakePosition>()??go.AddComponent<FakePosition>();
            fakePos.setLocalPosition(position);
            return fakePos;
        }
        public FakePosition LocalInstantiate(GameObject obj,Transform father,TSVector position)
        {
            var go = Instantiate(obj,father);
            var fakePos = go.GetComponent<FakePosition>()??go.AddComponent<FakePosition>();
            fakePos.setLocalPosition(position);
            return fakePos;
        }
    }
}
