using UnityEngine;
using Unity.Profiling;

// Collider 전환 이전의 고정 경로 이동과 경로 이탈 제한을 사용한다.
internal class EnemySwarmMovement
{
    private static readonly ProfilerMarker moveMarker = new("EnemySwarmMovement.Move");
    private readonly EnemyMovement enemy;
    private Vector3 swarmStart;
    private Vector3 swarmDirection;
    private bool swarmHasEnteredScreen;
    private float swarmProgress;
    private float swarmSpeed;
    private float swarmPushStrength;
    private float swarmMaxOffset;
    private float swarmRecoverySpeed;

    internal Vector3 Direction => swarmDirection;
    internal float CorrectionWeight => 1f / swarmPushStrength;

    internal EnemySwarmMovement(EnemyMovement enemy)
    {
        this.enemy = enemy;
    }

    internal void Reset()
    {
        swarmProgress = 0f;
        swarmHasEnteredScreen = false;
    }

    internal bool ShouldReturnOffscreen(bool inside)
    {
        if (inside) swarmHasEnteredScreen = true;
        return swarmHasEnteredScreen && !inside;
    }

    internal void Configure(Vector3 start, Vector3 direction, float speed,
        float pushStrength, float maxOffset, float recoverySpeed)
    {
        swarmStart = start;
        direction.y = 0f;
        swarmDirection = direction.normalized;
        swarmProgress = 0f;
        swarmHasEnteredScreen = false;
        swarmSpeed = Mathf.Max(0.01f, speed);
        swarmPushStrength = Mathf.Max(1f, pushStrength);
        swarmMaxOffset = Mathf.Max(0f, maxOffset);
        swarmRecoverySpeed = Mathf.Max(0f, recoverySpeed);
    }

    internal Vector3 Move(float deltaTime)
    {
        using var scope = moveMarker.Auto();
        Vector3 offset = enemy.framePosition - (swarmStart + swarmDirection * swarmProgress);
        offset.y = 0f;
        offset = Vector3.MoveTowards(offset, Vector3.zero, swarmRecoverySpeed * deltaTime);
        if (enemy.TryGetKnockbackMovement(deltaTime, out Vector3 knockback)) offset += knockback;
        swarmProgress += swarmSpeed * deltaTime;
        return swarmStart + swarmDirection * swarmProgress + Vector3.ClampMagnitude(offset, swarmMaxOffset);
    }

    internal Vector3 ClampPosition(Vector3 position)
    {
        Vector3 ideal = swarmStart + swarmDirection * swarmProgress;
        Vector3 offset = position - ideal;
        offset.y = 0f;
        return ideal + Vector3.ClampMagnitude(offset, swarmMaxOffset);
    }
}
