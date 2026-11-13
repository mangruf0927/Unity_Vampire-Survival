using UnityEngine;
using Unity.Profiling;

// 이벤트 박쥐의 고정 경로, 경로 복귀와 화면 진입 여부를 담당한다.
internal class EnemySwarmMovement
{
    private static readonly ProfilerMarker moveMarker = new("EnemySwarmMovement.Move");
    private readonly EnemyMovement enemy;
    private Vector3 swarmStart;
    private Vector3 swarmDirection;
    private bool swarmHasEnteredScreen;
    private float swarmSpeed;
    private float swarmMaxOffset;
    private float swarmRecoverySpeed;
    private float swarmAcceleration;

    internal Vector3 Direction => swarmDirection;

    internal EnemySwarmMovement(EnemyMovement enemy)
    {
        this.enemy = enemy;
    }

    internal void Reset()
    {
        swarmHasEnteredScreen = false;
    }

    internal bool ShouldReturnOffscreen(bool inside)
    {
        if (inside) swarmHasEnteredScreen = true;
        return swarmHasEnteredScreen && !inside;
    }

    internal void Configure(Vector3 start, Vector3 direction, float speed,
        float maxOffset, float recoverySpeed, float acceleration)
    {
        swarmStart = start;
        direction.y = 0f;
        swarmDirection = direction.normalized;
        swarmHasEnteredScreen = false;
        swarmSpeed = Mathf.Max(0.01f, speed);
        swarmMaxOffset = Mathf.Max(0f, maxOffset);
        swarmRecoverySpeed = Mathf.Max(0f, recoverySpeed);
        swarmAcceleration = Mathf.Max(0.1f, acceleration);
        enemy.transform.rotation = Quaternion.LookRotation(swarmDirection, Vector3.up);
    }

    internal Vector3 GetVelocity(Vector3 currentVelocity, float deltaTime)
    {
        using var scope = moveMarker.Auto();
        // 진행 기준을 실제 위치에 맞춘다. 충돌로 늦어진 거리를 빚처럼 쌓지 않는다.
        Vector3 fromStart = enemy.framePosition - swarmStart;
        fromStart.y = 0f;
        float progress = Vector3.Dot(fromStart, swarmDirection);
        Vector3 offset = fromStart - swarmDirection * progress;

        // 무기 넉백은 기존 지속 시간을 소비하며 구동 가속도와 별도로 적용한다.
        if (enemy.TryGetKnockbackMovement(deltaTime, out Vector3 knockback))
            return knockback / deltaTime;

        // 경로에 가까워질수록 복귀 속도를 줄여 가속도 제한으로 인한 왕복 흔들림을 줄인다.
        float recoveryScale = swarmRecoverySpeed / Mathf.Max(0.1f, swarmMaxOffset);
        Vector3 recoveryVelocity = Vector3.ClampMagnitude(-offset * recoveryScale, swarmRecoverySpeed);
        Vector3 desiredVelocity = swarmDirection * swarmSpeed + recoveryVelocity;
        currentVelocity.y = 0f;
        // 충돌로 느려진 속도를 단번에 덮어쓰지 않고 스텝당 속도 변화량을 제한한다.
        return Vector3.MoveTowards(currentVelocity, desiredVelocity, swarmAcceleration * deltaTime);
    }
}
