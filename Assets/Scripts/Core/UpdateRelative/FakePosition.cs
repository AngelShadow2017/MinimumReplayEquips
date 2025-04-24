using TrueSync;
using UnityEngine;

namespace Core.UpdateRelative
{
    public class FakePosition : ZeroBehaviour
    {
        public sealed override ManualScriptExecutionOrderEnum ScriptExecutionOrder { get; } = ManualScriptExecutionOrderEnum.FakePosition;

        [HideInInspector]
        public Vector3 __lastPosition__,__lastlastPosition__;//上一个位置,上上个位置，用来计算加速度用的
        private TSVector __local_pos__;
        private Vector3 __lastRenderPos__;
        private bool lastLerped = false;
        private bool __hasSet_local_pos__ = false;
#if UNITY_EDITOR
        [SerializeField]
        private bool checkHasSetPos = true;
#endif
        public TSVector localPosition {
            get
            {
#if UNITY_EDITOR
                if (checkHasSetPos && !__hasSet_local_pos__)
                {
                    Debug.LogError("Not Set LocalPosition May Cause Replay Wrong: " + gameObject.name);
                }
#endif
                return __local_pos__;
            }
            set
            {
                __local_pos__ = value;
                __hasSet_local_pos__ = true;
            }
    
        }
        //下一帧的位置，和无lerp的localPosition是一个意思
        public virtual void setLocalPosition(TSVector pos) {
            __lastRenderPos__ = __lastlastPosition__ = __lastPosition__ = (localPosition = pos).ToVector();
            transform.localPosition = __lastlastPosition__;
        }
        public override void ManualStart()
        {
            base.ManualStart();
            if (!__hasSet_local_pos__)
            {
                __local_pos__ = gameObject.transform.localPosition.ToTSVector();
            }
            __lastlastPosition__ = __lastPosition__ = __local_pos__.ToVector();// = gameObject.transform.localPosition.ToTSVector();
        }
        Vector3 __tmp__;
        public override void ManualFixedUpdate()
        {
            __tmp__ = __local_pos__.ToVector();
            if (lastLerped)
            {
                __lastRenderPos__ = (__tmp__) * 2.5f - 2 * __lastPosition__ + (__lastlastPosition__ / 2f);
                lastLerped = false;
            }
            __lastlastPosition__ = __lastPosition__;
            __lastPosition__ = __tmp__;
        }
        public override void ManualUpdate(float lerpVal = default)
        {
            __tmp__ = __local_pos__.ToVector();
#if !(UNITY_STANDALONE||UNITY_WINRT)
        gameObject.transform.localPosition = __tmp__;
        return;
#else
            if (lerpVal!=0&& __lastPosition__ != __tmp__ && __lastlastPosition__ != __lastPosition__)//如果上一帧和当前帧一样的话那肯定是直接停下来。
            {
                lastLerped = true;
                /*
                首先，根据最近3秒的位置，计算出最近2秒的速度向量。假设最近3秒的位置分别为 r1​​，r2​​，r3​​，那么最近2秒的速度向量分别为 v1​​=r2​​−r1​​，v2​​=r3​​−r2​​。
                然后，根据最近2秒的速度向量，计算出加速度向量。假设加速度向量为 a，那么 a=(v2​​−v1)/2​​​。
                最后，根据当前的位置和加速度向量，计算出下一秒的位置。假设当前的位置为 r3​​，下一秒的位置为 r4​​，那么 r4​​=r3​​+v2​​+a。
             */
                //r3-r2-r2+r1
                Vector3 target = Vector3.Lerp(__lastRenderPos__, __tmp__ * 2.5f - 2 * __lastPosition__ + (__lastlastPosition__ / 2f), lerpVal);
                gameObject.transform.localPosition = target;
            }
            else
            {
                gameObject.transform.localPosition = __tmp__;
                __lastRenderPos__ = __tmp__;
            }
#endif
        }
    }
}
