using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Profiling;
using System.Collections.Generic;

public partial class EnemyCrowdComparison
{
    [Header("Gameplay checks - test scene only")]
    [SerializeField] private bool manualPlayer;
    [SerializeField] private bool exerciseEffects;
    [SerializeField, Min(0.1f)] private float freezeDuration = 5f;
    [SerializeField, Min(0.1f)] private float effectRadius = 6f;
    [SerializeField, Min(1)] private int swarmCount = 20;
    [SerializeField, Min(0.1f)] private float swarmSpeed = 16f;
    [SerializeField, Min(1f)] private float swarmMass = 5f;
    private const int SwarmLayer = 29;
    private const int PlayerLayer = 8;
    private const int ExtraCapacity = 120;
    private readonly bool[] oldSwarmLayerRules = new bool[32];
    private readonly LegacyGridEnemyMovement[] gridActors = new LegacyGridEnemyMovement[Capacity + ExtraCapacity];
    private readonly ColliderCrowdTestEnemy[] colliderActors = new ColliderCrowdTestEnemy[Capacity + ExtraCapacity];
    private readonly ComparisonHealth[] actorHealth = new ComparisonHealth[Capacity + ExtraCapacity];
    private readonly Renderer[] actorRenderers = new Renderer[Capacity + ExtraCapacity];
    private readonly bool[] enteredScreen = new bool[Capacity + ExtraCapacity];
    private readonly Plane[] swarmPlanes = new Plane[6];
    private readonly List<Vector3> formationOffsets = new();
    private readonly int[] formationRows = new int[31];
    private int peakActiveCount;
    private long activeCountSum;
    private CharacterController playerController;
    private PlayerMovement playerMovement;
    private PlayerInput playerInput;
    private InputActionAsset testInput;
    private ComparisonHealth playerHealth;
    private float frozenUntil;
    private int activeSwarmCount, totalSwarmSpawned, killed;
    private int knockbackRequests, clockRequests;
    private int nextSwarmSequence;
    private float scenarioTime, nextKnockback, nextClock, nextSwarm, nextScreenCheck;
    private bool effectsStarted, runEffects, runManual;
    private CrowdMode runMode;
    private static readonly ProfilerMarker effectsMarker = new("EnemyComparison.TestEffects");
    public bool IsFrozen => Time.time < frozenUntil;

    private void AddPlayerControls(GameObject target)
    {
        target.SetActive(false);
        target.layer = PlayerLayer;
        playerController = target.AddComponent<CharacterController>();
        playerController.height = 1f;
        playerController.radius = 0.5f;
        playerController.skinWidth = 0.01f;
        playerController.stepOffset = 0f;
        playerController.slopeLimit = 0f;
        playerMovement = target.AddComponent<PlayerMovement>();
        playerMovement.enabled = false;
        playerHealth = target.AddComponent<ComparisonHealth>();
        playerHealth.Initialize(null, -1, 10000f, DataManager.Instance.SelectedPlayer.damageInterval);
        testInput = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap map = testInput.AddActionMap("Comparison");
        InputAction move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        playerInput = target.AddComponent<PlayerInput>();
        playerInput.actions = testInput;
        playerInput.defaultActionMap = "Comparison";
        playerInput.notificationBehavior = PlayerNotifications.SendMessages;
        playerInput.DeactivateInput();
        playerInput.enabled = false;
        target.SetActive(true);
    }

    private void BeginGameplay(CrowdMode selectedMode)
    {
        runMode = selectedMode;
        peakActiveCount = 0;
        activeCountSum = 0;
        runEffects = exerciseEffects;
        runManual = manualPlayer;
        frozenUntil = 0f;
        totalSwarmSpawned = killed = knockbackRequests = clockRequests = nextSwarmSequence = 0;
        effectsStarted = false;
        playerController.enabled = false;
        player.position = new Vector3(0f, Height, 0f);
        playerController.enabled = true;
        playerHealth.Initialize(null, -1, 10000f, DataManager.Instance.SelectedPlayer.damageInterval);
        playerMovement.enabled = runManual;
        playerInput.enabled = runManual;
        if (runManual) playerInput.ActivateInput();
        else playerInput.DeactivateInput();
    }

    private void StartEffects()
    {
        effectsStarted = true;
        scenarioTime = 0f;
        nextKnockback = 2f;
        nextClock = 5f;
        nextSwarm = 3f;
    }

    private void UpdateGameplay()
    {
        Keyboard keys = Keyboard.current;
        if (keys != null && keys.tKey.wasPressedThisFrame) Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
        if (Time.deltaTime <= 0f || Time.timeScale <= 0f) return;
        using var scope = effectsMarker.Auto();
        if (!runManual && movingTarget)
        {
            float x = Mathf.PingPong(runTime * 3f, 16f);
            playerController.Move(new Vector3(x - player.position.x, 0f, -player.position.z));
        }
        if (keys != null)
        {
            if (keys.fKey.wasPressedThisFrame) FreezeEnemies();
            if (keys.kKey.wasPressedThisFrame) KnockbackNearby();
            if (keys.bKey.wasPressedThisFrame) SpawnSwarm();
            if (keys.xKey.wasPressedThisFrame) DamageNearby();
        }
        if (effectsStarted && runEffects)
        {
            scenarioTime += Time.deltaTime;
            if (scenarioTime >= nextClock) { nextClock += 8f; FreezeEnemies(); }
            if (scenarioTime >= nextKnockback) { nextKnockback += 3f; KnockbackNearby(); }
            if (scenarioTime >= nextSwarm) { nextSwarm += 5f; SpawnSwarm(); }
        }
        if (activeSwarmCount > 0 && Time.time >= nextScreenCheck)
        {
            nextScreenCheck = Time.time + 0.2f;
            GeometryUtility.CalculateFrustumPlanes(testCamera, swarmPlanes);
            for (int i = activeCount - 1; i >= 0; i--)
            {
                if (!actorHealth[i].IsSwarm) continue;
                bool visible = GeometryUtility.TestPlanesAABB(swarmPlanes, actorRenderers[i].bounds);
                bool returned;
                if (runMode == CrowdMode.Grid) returned = gridActors[i].ShouldReturnOffscreen(visible, Time.time);
                else
                {
                    if (visible) enteredScreen[i] = true;
                    returned = enteredScreen[i] && !visible;
                }
                if (returned) ObjectPool.Instance.TryReturnToPool(active[i]);
            }
        }
    }

    private void LateUpdate()
    {
        if (testCamera != null && player != null)
            testCamera.transform.position = new Vector3(player.position.x, 50f, player.position.z - 50f);
    }

    private void RegisterEnemy(GameObject enemy, EnemyData data)
    {
        int index = activeCount++;
        peakActiveCount = Mathf.Max(peakActiveCount, activeCount);
        active[index] = enemy;
        enteredScreen[index] = false;
        actorRenderers[index] = enemy.GetComponent<Renderer>();
        actorHealth[index] = enemy.GetComponent<ComparisonHealth>();
        actorHealth[index].Initialize(this, index, data.maxHealth);
        gridActors[index] = enemy.GetComponent<LegacyGridEnemyMovement>();
        colliderActors[index] = enemy.GetComponent<ColliderCrowdTestEnemy>();
        if (runMode == CrowdMode.Grid) gridActors[index].SetTarget(player);
        else colliderActors[index].Initialize(this, player, data, runColliderRadius, runDepenetrationVelocity, runAcceleration);
    }

    public void RemoveEnemy(ComparisonHealth health)
    {
        int index = health.Index;
        if (index < 0 || index >= activeCount || actorHealth[index] != health) return;
        if (health.IsSwarm) activeSwarmCount--;
        int last = --activeCount;
        active[index] = active[last];
        actorHealth[index] = actorHealth[last];
        actorRenderers[index] = actorRenderers[last];
        gridActors[index] = gridActors[last];
        colliderActors[index] = colliderActors[last];
        enteredScreen[index] = enteredScreen[last];
        if (index != last) actorHealth[index].Index = index;
        active[last] = null;
        actorHealth[last] = null;
        actorRenderers[last] = null;
        gridActors[last] = null;
        colliderActors[last] = null;
    }

    private void FreezeEnemies()
    {
        clockRequests++;
        frozenUntil = Time.time + Mathf.Max(0.1f, freezeDuration);
        if (runMode == CrowdMode.Grid) LegacyGridEnemyCrowd.FreezeFor(freezeDuration);
        else for (int i = 0; i < activeCount; i++) colliderActors[i].SetFrozen(true);
    }

    private void KnockbackNearby()
    {
        knockbackRequests++;
        float squared = effectRadius * effectRadius;
        for (int i = 0; i < activeCount; i++)
        {
            Vector3 direction = active[i].transform.position - player.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > squared) continue;
            if (runMode == CrowdMode.Grid) gridActors[i].ApplyKnockback(direction, 1f);
            else colliderActors[i].ApplyKnockback(direction, 1f);
        }
    }

    private void DamageNearby()
    {
        float squared = effectRadius * effectRadius;
        for (int i = activeCount - 1; i >= 0; i--)
        {
            Vector3 direction = active[i].transform.position - player.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > squared) continue;
            ComparisonHealth health = actorHealth[i];
            health.TakeDamage(10f);
            if (health.IsDead) killed++;
        }
    }

    private void SpawnSwarm()
    {
        int count = Mathf.Min(Mathf.Clamp(swarmCount, 1, ExtraCapacity), ExtraCapacity - activeSwarmCount);
        if (count == 0 || activeCount + count > active.Length) return;
        Ray ray = testCamera.ViewportPointToRay(new Vector3((nextSwarmSequence++ & 1) == 0 ? 0f : 1f, 0.5f, 0f));
        Plane plane = new(Vector3.up, new Vector3(0f, Height, 0f));
        if (!plane.Raycast(ray, out float distance)) return;
        Vector3 center = ray.GetPoint(distance);
        Vector3 direction = player.position - center;
        direction.y = 0f;
        direction.Normalize();
        BuildSwarmOffsets(direction, Vector3.Cross(Vector3.up, direction), count);
        float front = 0f;
        for (int i = 0; i < formationOffsets.Count; i++) front = Mathf.Max(front, Vector3.Dot(formationOffsets[i], direction));
        center -= direction * (front + 4f);
        EnemyData data = DataManager.Instance.Enemies.Get(1);
        PoolTypeEnums type = runMode == CrowdMode.Grid ? PoolTypeEnums.Bat : PoolTypeEnums.Zombie;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = center + formationOffsets[i];
            GameObject enemy = ObjectPool.Instance.GetFromPool(type, position, Quaternion.LookRotation(direction, Vector3.up));
            if (enemy == null) break;
            enemy.layer = SwarmLayer;
            RegisterEnemy(enemy, data);
            int index = activeCount - 1;
            actorHealth[index].IsSwarm = true;
            activeSwarmCount++;
            totalSwarmSpawned++;
            if (runMode == CrowdMode.Grid) gridActors[index].ConfigureSwarm(position, direction, swarmSpeed, swarmMass, 0.25f, 4f);
            else colliderActors[index].ConfigureSwarm(position, direction, swarmSpeed, swarmMass, 0.25f, 4f);
        }
        Physics.SyncTransforms();
    }

    private void BuildSwarmOffsets(Vector3 direction, Vector3 side, int count)
    {
        formationOffsets.Clear();
        int rows = 2 * Mathf.CeilToInt(Mathf.Sqrt(count * 0.5f)) - 1;
        int middle = rows / 2;
        int totalWeight = (rows * rows + 1) / 2;
        int[] rowCounts = formationRows;
        int assigned = 0;
        for (int row = 0; row < rows; row++)
        {
            int weight = 2 * (middle - Mathf.Abs(row - middle)) + 1;
            rowCounts[row] = Mathf.Max(1, Mathf.FloorToInt(count * (float)weight / totalWeight));
            assigned += rowCounts[row];
        }
        int remaining = count - assigned;
        if (remaining % 2 == 1) { rowCounts[middle]++; remaining--; }
        for (int distance = 1; remaining > 0; distance++)
        {
            rowCounts[middle - distance]++;
            rowCounts[middle + distance]++;
            remaining -= 2;
        }
        for (int row = 0; row < rows; row++)
            for (int column = 0; column < rowCounts[row]; column++)
                formationOffsets.Add(side * ((column - (rowCounts[row] - 1) * 0.5f) * 1.3f)
                    + direction * ((middle - row) * 1.3f));
    }
}
