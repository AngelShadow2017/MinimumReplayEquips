using Core.UpdateRelative;
using TrueSync;

namespace Test
{
    public class BulletMove:ZeroBehaviour
    {
        public FakePosition fakePosition;
        public TSVector speed;
        public int liveTime = 0;

        public override void ManualStart()
        {
            liveTime = (int)TSRandom.Range(0, 120f);
        }
        public override void ManualFixedUpdate()
        {
            fakePosition.localPosition+=speed;
            if (liveTime++ > 300)
            {
                Destroy(gameObject);
            }
        }
    }
}