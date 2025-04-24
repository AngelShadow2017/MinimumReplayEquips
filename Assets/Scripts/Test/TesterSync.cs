using Core.UpdateRelative;
using TrueSync;
using UnityEngine;

namespace Test
{
    //尽量用sealed，能够最好的得到编译器优化
    public sealed class TesterSync:ZeroBehaviour
    {
        private FakePosition fakeTransform;
        public FP speed = 0.1;
        public override void ManualAwake()
        {
            fakeTransform = gameObject.AddComponent<FakePosition>();
        }

        public override void ManualFixedUpdate()
        {
            
            //Debug.Log("update");
            fakeTransform.localPosition+=TSVector.down*speed;
        }
    }
}