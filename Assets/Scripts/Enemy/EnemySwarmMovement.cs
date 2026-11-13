using UnityEngine;

public class EnemySwarmMovement : EnemyMovement
{
    [SerializeField, Min(0f)] private float swarmOverlapCorrectionSpeed = 3f;
    private Vector3 swarmStart;
    private Vector3 swarmDirection;
    private bool swarmHasEnteredScreen;
    private float swarmProgress;
    private float swarmSpeed;
    private float swarmPushStrength;
    private float swarmMaxOffset;
    private float swarmRecoverySpeed;

    internal override bool IsSwarm => true;
    internal override float MovementRadius => BodyRadius;
    internal override Vector3 SwarmDirection => swarmDirection;
    internal override float CorrectionWeight => 1f / swarmPushStrength;
    internal override float CorrectionSpeed => swarmOverlapCorrectionSpeed * CorrectionWeight;

    protected override void ResetMovement()
    {
        swarmProgress = 0f;
        swarmHasEnteredScreen = false;
        swarmPushStrength = 1f;
    }

    protected override void StopMovement()
    {
        swarmProgress = 0f;
        swarmHasEnteredScreen = false;
    }

    internal override void PrepareSwarmStep(float deltaTime)
    {
        base.PrepareSwarmStep(deltaTime);
        crowdPosition = Move(deltaTime);
    }

    internal override void ApplyCrowdStep()
    {
        crowdPosition = ClampPosition(crowdPosition);
        if ((crowdPosition - body.position).sqrMagnitude > 0f) body.position = crowdPosition;
        body.rotation = Quaternion.LookRotation(swarmDirection, Vector3.up);
    }

    internal override bool CheckScreenExit(bool inside, float now)
    {
        if (inside) swarmHasEnteredScreen = true;
        return swarmHasEnteredScreen && !inside;
    }

    public void ConfigureSwarm(Vector3 start, Vector3 direction, float speed,
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
        body.rotation = Quaternion.LookRotation(swarmDirection, Vector3.up);
    }

    internal Vector3 Move(float deltaTime)
    {
        Vector3 offset = framePosition - (swarmStart + swarmDirection * swarmProgress);
        offset.y = 0f;
        offset = Vector3.MoveTowards(offset, Vector3.zero, swarmRecoverySpeed * deltaTime);
        if (TryGetKnockbackMovement(deltaTime, out Vector3 knockback)) offset += knockback;
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
