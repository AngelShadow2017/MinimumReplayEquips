// PlayerController.cs

using Core.UpdateRelative;
using Test;
using TrueSync;
using UnityEngine;

public sealed class PlayerController : ZeroBehaviour
{
    public FakePosition fakeTransform;
    [Header("Movement")]
    public FP moveSpeed = 5.0;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float minSpeed = 2.0f;
    public float maxSpeed = 10.0f;

    private int fireCounter;

    public override void ManualAwake()
    {
        fakeTransform = gameObject.AddComponent<FakePosition>();
    }
    public override void ManualFixedUpdate()
    {
        // 从GameManager获取输入
        InputData input = GameManager.Instance.CurrentInput;
        MovePlayer(input.input);

        // 发射逻辑
        if (input.shoot&&ShouldFire())
        {
            FireProjectile();
        }
    }

    void MovePlayer(TSVector2 input)
    {
        fakeTransform.localPosition += (TSVector)(input * moveSpeed);
    }

    bool ShouldFire()
    {
        fireCounter = (fireCounter + 1) % 3;
        return fireCounter == 0;
    }

    private FP angD = 0;
    void FireProjectile()
    {
        int cnt = TSRandom.Range(1, 5);
        for(int i = 0;i<cnt;i++)
        {
            angD += 0.2;
            // 从GameManager获取新种子
            FP ang = (angD+i*72)*TSMath.Deg2Rad;//TSRandom.Range(0, Mathf.PI * 2);
            TSVector2 direction = new TSVector2(FP.FastCos(ang),FP.FastSin(ang));
            FP speed = TSRandom.Range(minSpeed, maxSpeed);

            // 生成弹丸
            FakePosition projectile = LocalInstantiate(
                projectilePrefab,
                fakeTransform.localPosition
            );
            var script = projectile.gameObject.AddComponent<BulletMove>();
            script.fakePosition=projectile;
            script.speed =(TSVector)(speed * direction);
        }
    }
}