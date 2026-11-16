using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Unity.Profiling;

// 별도 비교 씬 전용. 기존 Enemy 코드와 프로젝트 물리 설정 파일은 변경하지 않는다.
public partial class EnemyCrowdComparison : MonoBehaviour
{
    public enum CrowdMode { Grid, Collider }
    public enum LayoutMode { SameArea, SameDensity }

    [SerializeField] private Material enemyMaterial;
    [SerializeField] private Material groundMaterial;
    [SerializeField] private CrowdMode mode;
    [SerializeField] private LayoutMode layout;
    [SerializeField] private bool movingTarget;
    [SerializeField] private bool obstacles;
    [SerializeField] private int seed = 12345;
    [SerializeField, Min(0f)] private float innerRadius = 5f;
    [SerializeField, Min(1f)] private float outerRadiusAt500 = 22f;
    [SerializeField, Min(1f)] private float warmupSeconds = 5f;
    [SerializeField, Min(1f)] private float measurementSeconds = 15f;
    [SerializeField, Min(1)] private int suiteRepeats = 3;

    [Header("Collider test settings (visual size and target stop distance stay unchanged)")]
    [SerializeField, Range(0.1f, 0.5f)] private float colliderRadius = 0.45f;
    [SerializeField, Min(0.01f)] private float maxDepenetrationVelocity = 2f;
    [SerializeField, Min(0.01f)] private float chaseAcceleration = 24f;

    private const float SpawnSpacing = 1.1f;
    private const int Capacity = 1500;
    private const int SampleCapacity = 60000;
    private const int EnemyLayer = 30;
    private const int ObstacleLayer = 11;
    private const float Height = 0.5f;

    private readonly GameObject[] active = new GameObject[Capacity + ExtraCapacity];
    private readonly Vector3[] positions = new Vector3[Capacity];
    private readonly float[] frameMs = new float[SampleCapacity];
    private readonly float[] sortedMs = new float[SampleCapacity];
    private readonly List<GameObject> obstacleObjects = new();
    private readonly List<Bounds> obstacleBounds = new();
    private readonly List<string> results = new();
    private readonly List<Vector3> placementCandidates = new(8192);
    private float actualOuterRadius;
    private float runColliderRadius;
    private float runDepenetrationVelocity;
    private float runAcceleration;
    private readonly bool[] oldLayerRules = new bool[32];
    private Transform player;
    private GameObject templates;
    private GameObject gridTemplate;
    private GameObject colliderTemplate;
    private Camera testCamera;
    private PhysicsMaterial contactMaterial;
    private Coroutine run;
    private ProfilerRecorder mainThreadRecorder;
    private ProfilerRecorder gcRecorder;
    private readonly ComparisonPhysicsMetrics physicsMetrics = new();
    private int pendingFixedSteps, previousFrameFixedSteps, completedFrameFixedSteps;
    private SimulationMode oldSimulationMode;
    private bool ready;
    private bool settingsSaved;
    private bool ownsPools;
    private bool sampling;
    private int activeCount;
    private int sampleCount;
    private int cpuSamples;
    private int gcSamples;
    private int lastCount = 500;
    private int oldVSync;
    private int oldFrameRate;
    private float oldTimeScale;
    private float runTime;
    private float elapsed;
    private float cpuSumMs;
    private long gcSum;
    private string status = "Preparing pools...";

    private IEnumerator Start()
    {
        if (ObjectPool.Instance.TryGetPrefab(PoolTypeEnums.Slime, out _)
            || ObjectPool.Instance.TryGetPrefab(PoolTypeEnums.TurtleMonster, out _))
        {
            Debug.LogError("EnemyCrowdComparison은 기존 게임 씬과 함께 열지 말고 단독 실행해 주세요.", this);
            enabled = false;
            yield break;
        }
        DataManager.Instance.LoadAll();
        oldTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        oldVSync = QualitySettings.vSyncCount;
        oldFrameRate = Application.targetFrameRate;
        for (int i = 0; i < 32; i++)
        {
            oldLayerRules[i] = Physics.GetIgnoreLayerCollision(EnemyLayer, i);
            oldSwarmLayerRules[i] = Physics.GetIgnoreLayerCollision(SwarmLayer, i);
        }
        oldSimulationMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.FixedUpdate;
        settingsSaved = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        for (int i = 0; i < 32; i++)
        {
            Physics.IgnoreLayerCollision(EnemyLayer, i, i != EnemyLayer && i != SwarmLayer && i != ObstacleLayer);
            Physics.IgnoreLayerCollision(SwarmLayer, i, i != EnemyLayer && i != SwarmLayer);
        }

        BuildEnvironment();
        BuildTemplates();
        // CreatePrimitive로 생긴 임시 Collider를 Destroy한 뒤 풀을 준비한다.
        yield return null;
        PreparePools();
        mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        physicsMetrics.TryStartMissing();
        ready = true;
        status = "Ready: G/C mode, 1/2/3 count, M moving target, O obstacles, D density, P manual player, V effects, 4/5/6 radius, A suite";
        Debug.Log(status, this);
    }

    private void BuildEnvironment()
    {
        GameObject cameraObject = new("Comparison Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        testCamera = cameraObject.GetComponent<Camera>();
        testCamera.orthographic = true;
        testCamera.orthographicSize = 30f;
        testCamera.nearClipPlane = 0.1f;
        testCamera.farClipPlane = 200f;
        testCamera.backgroundColor = new Color(0.12f, 0.15f, 0.19f);
        testCamera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 50f, -50f), Quaternion.Euler(45f, 0f, 0f));

        GameObject lightObject = new("Comparison Light", typeof(Light));
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.None;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Visual ground - no physics";
        ground.transform.position = new Vector3(0f, -0.1f, 0f);
        ground.transform.localScale = new Vector3(150f, 0.2f, 150f);
        Destroy(ground.GetComponent<Collider>());
        ApplyMaterial(ground, groundMaterial);

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.name = "Target - radius 0.5, no collider or damage";
        target.transform.position = new Vector3(0f, Height, 0f);
        Destroy(target.GetComponent<Collider>());
        ApplyMaterial(target, groundMaterial);
        MaterialPropertyBlock targetColor = new();
        targetColor.SetColor("_BaseColor", new Color(1f, 0.5f, 0.15f));
        targetColor.SetColor("_Color", new Color(1f, 0.5f, 0.15f));
        target.GetComponent<MeshRenderer>().SetPropertyBlock(targetColor);
        player = target.transform;
        AddPlayerControls(target);

        AddObstacle(new Vector3(4f, 1f, 4f), new Vector3(2f, 2f, 5f));
        AddObstacle(new Vector3(-5f, 1f, 3f), new Vector3(2f, 2f, 2f));
        AddObstacle(new Vector3(2f, 1f, -6f), new Vector3(6f, 2f, 2f));
        SetObstacles();
    }

    private void AddObstacle(Vector3 position, Vector3 size)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "Comparison obstacle";
        obstacle.layer = ObstacleLayer;
        obstacle.transform.position = position;
        obstacle.transform.localScale = size;
        ApplyMaterial(obstacle, enemyMaterial);
        obstacleObjects.Add(obstacle);
        obstacleBounds.Add(new Bounds(position, size));
    }

    private void SetObstacles()
    {
        for (int i = 0; i < obstacleObjects.Count; i++) obstacleObjects[i].SetActive(obstacles);
        Physics.SyncTransforms();
    }

    private void ApplyMaterial(GameObject obj, Material material)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (material != null) renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void BuildTemplates()
    {
        templates = new GameObject("Inactive comparison templates");
        templates.SetActive(false);
        gridTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gridTemplate.name = "Grid template - archived grid movement, JSON Slime ID 1";
        gridTemplate.transform.SetParent(templates.transform);
        gridTemplate.SetActive(false);
        gridTemplate.layer = EnemyLayer;
        ApplyMaterial(gridTemplate, enemyMaterial);
        Destroy(gridTemplate.GetComponent<Collider>());
        gridTemplate.AddComponent<LegacyGridEnemyMovement>();
        gridTemplate.AddComponent<ComparisonHealth>();

        colliderTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        colliderTemplate.name = "Collider template - Rigidbody and SphereCollider";
        colliderTemplate.transform.SetParent(templates.transform);
        colliderTemplate.SetActive(false);
        colliderTemplate.layer = EnemyLayer;
        ApplyMaterial(colliderTemplate, enemyMaterial);
        contactMaterial = new PhysicsMaterial("Comparison contact - no friction or bounce")
        {
            staticFriction = 0f,
            dynamicFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        colliderTemplate.GetComponent<SphereCollider>().sharedMaterial = contactMaterial;
        Rigidbody body = colliderTemplate.AddComponent<Rigidbody>();
        body.mass = 1f;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        colliderTemplate.AddComponent<ColliderCrowdTestEnemy>();
        colliderTemplate.AddComponent<ComparisonHealth>();
    }

    private void PreparePools()
    {
        ObjectPool.Instance.Register(new PoolData { poolType = PoolTypeEnums.Slime, prefab = gridTemplate, size = Capacity + ExtraCapacity }, transform);
        ObjectPool.Instance.Register(new PoolData { poolType = PoolTypeEnums.TurtleMonster, prefab = colliderTemplate, size = Capacity + ExtraCapacity }, transform);
        ownsPools = true;
    }

    private void FixedUpdate()
    {
        pendingFixedSteps++;
    }

    private void Update()
    {
        // Recorder.LastValue와 동일한, 직전 완료 프레임의 물리 스텝 수를 사용한다.
        completedFrameFixedSteps = previousFrameFixedSteps;
        previousFrameFixedSteps = pendingFixedSteps;
        pendingFixedSteps = 0;
        if (!ready) return;
        if (run != null)
        {
            runTime += Time.deltaTime;
            UpdateGameplay();
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CancelRun();
            return;
        }
        Keyboard keys = Keyboard.current;
        if (keys == null) return;
        if (keys.pKey.wasPressedThisFrame) manualPlayer = !manualPlayer;
        if (keys.vKey.wasPressedThisFrame) exerciseEffects = !exerciseEffects;
        if (keys.gKey.wasPressedThisFrame) mode = CrowdMode.Grid;
        if (keys.cKey.wasPressedThisFrame) mode = CrowdMode.Collider;
        if (keys.digit4Key.wasPressedThisFrame || keys.numpad4Key.wasPressedThisFrame) colliderRadius = 0.5f;
        if (keys.digit5Key.wasPressedThisFrame || keys.numpad5Key.wasPressedThisFrame) colliderRadius = 0.45f;
        if (keys.digit6Key.wasPressedThisFrame || keys.numpad6Key.wasPressedThisFrame) colliderRadius = 0.4f;
        if (keys.mKey.wasPressedThisFrame) movingTarget = !movingTarget;
        if (keys.oKey.wasPressedThisFrame) { obstacles = !obstacles; SetObstacles(); }
        if (keys.dKey.wasPressedThisFrame) layout = layout == LayoutMode.SameArea ? LayoutMode.SameDensity : LayoutMode.SameArea;
        if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) BeginRun(500);
        else if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) BeginRun(1000);
        else if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) BeginRun(1500);
        else if (keys.rKey.wasPressedThisFrame) BeginRun(lastCount);
        else if (keys.aKey.wasPressedThisFrame) run = StartCoroutine(Suite());
    }

    private void BeginRun(int count)
    {
        if (run != null) return;
        lastCount = count;
        run = StartCoroutine(SingleRun(count, mode));
    }

    private IEnumerator SingleRun(int count, CrowdMode selectedMode)
    {
        yield return Measure(count, selectedMode);
        run = null;
    }

    private IEnumerator Suite()
    {
        for (int repeat = 0; repeat < Mathf.Max(1, suiteRepeats); repeat++)
            for (int count = 500; count <= Capacity; count += 500)
                for (int order = 0; order < 2; order++)
                    yield return Measure(count, (CrowdMode)((order + repeat) % 2));
        run = null;
    }

    private IEnumerator Measure(int count, CrowdMode selectedMode)
    {
        ClearEnemies();
        status = "Waiting for previous grid freeze to expire";
        while (LegacyGridEnemyCrowd.IsFrozen) yield return null;
        BeginGameplay(selectedMode);
        mode = selectedMode;
        SetObstacles();
        runTime = 0f;
        sampleCount = cpuSamples = gcSamples = 0;
        cpuSumMs = 0f;
        gcSum = 0;
        runColliderRadius = Mathf.Clamp(colliderRadius, 0.1f, 0.5f);
        runDepenetrationVelocity = Mathf.Max(0.01f, maxDepenetrationVelocity);
        runAcceleration = Mathf.Max(0.01f, chaseAcceleration);
        BuildPositions(count);
        Spawn(count, selectedMode);
        Debug.Log($"EnemyComparison BEGIN: {selectedMode}, {count}, {layout}, moving={movingTarget}, obstacles={obstacles}, collider radius={runColliderRadius}, actual outer radius={actualOuterRadius}", this);
        status = "Warmup";
        yield return null;
        physicsMetrics.TryStartMissing();
        elapsed = 0f;
        while (elapsed < Mathf.Max(1f, warmupSeconds))
        {
            yield return null;
            if (Time.timeScale > 0f && Time.deltaTime > 0f) elapsed += Time.unscaledDeltaTime;
        }
        status = "Recording";
        elapsed = 0f;
        sampling = true;
        StartEffects();
        physicsMetrics.Reset();
        while (elapsed < Mathf.Max(1f, measurementSeconds))
        {
            yield return null;
            if (Time.timeScale <= 0f || Time.deltaTime <= 0f) continue;
            float delta = Time.unscaledDeltaTime;
            elapsed += delta;
            if (sampleCount >= SampleCapacity) continue;
            frameMs[sampleCount++] = delta * 1000f;
            physicsMetrics.Sample(completedFrameFixedSteps);
            activeCountSum += activeCount;
            if (mainThreadRecorder.Valid && mainThreadRecorder.Count > 0)
            {
                cpuSumMs += mainThreadRecorder.LastValue * 0.000001f;
                cpuSamples++;
            }
            if (gcRecorder.Valid && gcRecorder.Count > 0)
            {
                gcSum += gcRecorder.LastValue;
                gcSamples++;
            }
        }
        sampling = false;
        FinishResult(count, selectedMode);
        ClearEnemies();
        // 결과 출력/풀 반환 프레임은 다음 실행의 기록에 포함하지 않는다.
        yield return null;
        status = "Complete - choose another run or export CSV";
    }

    private void BuildPositions(int count)
    {
        float inner = Mathf.Max(0f, innerRadius);
        float outer = Mathf.Max(inner + 1f, outerRadiusAt500);
        float areaScale = layout == LayoutMode.SameDensity ? count / 500f : 1f;
        actualOuterRadius = Mathf.Sqrt(inner * inner + (outer * outer - inner * inner) * areaScale);
        // 고정 영역에서는 항상 1500개가 들어가는 영역을 사용해 적 수별 영역 차이를 막는다.
        // 반경 비교도 동일 좌표를 쓰도록 초기 간격은 모든 경우 최대 반경 0.5를 기준으로 한다.
        int required = layout == LayoutMode.SameArea ? Capacity : count;
        for (int attempt = 0; attempt < 32; attempt++)
        {
            BuildPlacementCandidates(inner, actualOuterRadius);
            if (placementCandidates.Count >= required) break;
            actualOuterRadius += SpawnSpacing;
        }
        if (placementCandidates.Count < required)
            throw new InvalidOperationException("초기 배치 영역을 넓혀 주세요.");

        System.Random random = new(seed);
        for (int i = placementCandidates.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Vector3 temporary = placementCandidates[i];
            placementCandidates[i] = placementCandidates[j];
            placementCandidates[j] = temporary;
        }
        for (int i = 0; i < count; i++) positions[i] = placementCandidates[i];
        float extent = actualOuterRadius + (movingTarget ? 16f : 0f) + 3f;
        testCamera.orthographicSize = Mathf.Max(26f, Mathf.Max(extent, extent / Mathf.Max(0.1f, testCamera.aspect)));
    }

    private void BuildPlacementCandidates(float inner, float outer)
    {
        placementCandidates.Clear();
        System.Random random = new(seed);
        float angle = (float)random.NextDouble() * Mathf.PI * 2f;
        float cosine = Mathf.Cos(angle);
        float sine = Mathf.Sin(angle);
        float rowHeight = SpawnSpacing * Mathf.Sqrt(3f) * 0.5f;
        int rows = Mathf.CeilToInt(outer / rowHeight) + 1;
        int columns = Mathf.CeilToInt(outer / SpawnSpacing) + 1;
        for (int row = -rows; row <= rows; row++)
        {
            for (int column = -columns; column <= columns; column++)
            {
                // 육각 간격 + 작은 흔들림. 최악의 경우에도 중심 간격은 1.04 이상이다.
                float x = (column + ((row & 1) == 0 ? 0f : 0.5f)) * SpawnSpacing
                    + ((float)random.NextDouble() - 0.5f) * 0.04f;
                float z = row * rowHeight + ((float)random.NextDouble() - 0.5f) * 0.04f;
                float squaredRadius = x * x + z * z;
                if (squaredRadius < inner * inner || squaredRadius > outer * outer) continue;
                Vector3 position = new(x * cosine - z * sine, Height, x * sine + z * cosine);
                bool blocked = false;
                if (obstacles)
                    for (int i = 0; i < obstacleBounds.Count; i++)
                    {
                        Bounds bounds = obstacleBounds[i];
                        bounds.Expand(1.1f);
                        if (bounds.Contains(position)) { blocked = true; break; }
                    }
                if (!blocked) placementCandidates.Add(position);
            }
        }
    }

    private void Spawn(int count, CrowdMode selectedMode)
    {
        EnemyData data = DataManager.Instance.Enemies.Get(1);
        PoolTypeEnums type = selectedMode == CrowdMode.Grid ? PoolTypeEnums.Slime : PoolTypeEnums.TurtleMonster;
        for (int i = 0; i < count; i++)
        {
            Vector3 direction = player.position - positions[i];
            direction.y = 0f;
            GameObject enemy = ObjectPool.Instance.GetFromPool(type, positions[i], Quaternion.LookRotation(direction, Vector3.up));
            if (enemy == null) throw new InvalidOperationException("비교용 풀 대여 실패");
            enemy.layer = EnemyLayer;
            RegisterEnemy(enemy, data);
        }
        Physics.SyncTransforms();
    }

    private void ClearEnemies()
    {
        while (activeCount > 0)
        {
            int last = activeCount - 1;
            ComparisonHealth health = actorHealth[last];
            if (active[last] == null || health == null) { activeCount--; continue; }
            ObjectPool.Instance.TryReturnToPool(active[last]);
            if (activeCount > last) RemoveEnemy(health);
        }
        activeSwarmCount = 0;
        if (playerInput != null) { playerInput.DeactivateInput(); playerInput.enabled = false; }
        if (playerMovement != null) playerMovement.enabled = false;
        effectsStarted = false;
    }

    private void FinishResult(int count, CrowdMode selectedMode)
    {
        if (sampleCount == 0) return;
        Array.Copy(frameMs, sortedMs, sampleCount);
        Array.Sort(sortedMs, 0, sampleCount);
        double sum = 0;
        for (int i = 0; i < sampleCount; i++) sum += frameMs[i];
        float p95 = sortedMs[Mathf.Clamp(Mathf.CeilToInt(sampleCount * 0.95f) - 1, 0, sampleCount - 1)];
        string result = string.Format(CultureInfo.InvariantCulture,
            "{0},{1},{2},{3},{4},{5},{6:F3},{7:F3},{8:F3},{9},{10},{11:F4},{12},{13:F2},{14:F2},{15:F2},{16:F2},{17:F2},{18},{19:F2},{20:F2}",
            selectedMode, count, layout, movingTarget, obstacles, sampleCount, sum / sampleCount,
            p95, sortedMs[sampleCount - 1], cpuSamples > 0 ? (cpuSumMs / cpuSamples).ToString("F3", CultureInfo.InvariantCulture) : "NA",
            gcSamples > 0 ? ((double)gcSum / gcSamples).ToString("F1", CultureInfo.InvariantCulture) : "NA", Time.fixedDeltaTime, seed, innerRadius, outerRadiusAt500, warmupSeconds, measurementSeconds,
            selectedMode == CrowdMode.Grid ? 0.5f : runColliderRadius,
            selectedMode == CrowdMode.Grid ? "NA" : runDepenetrationVelocity.ToString("F2", CultureInfo.InvariantCulture),
            selectedMode == CrowdMode.Grid ? DataManager.Instance.Enemies.Get(1).moveSpeed * 8f : runAcceleration,
            actualOuterRadius);
        result += physicsMetrics.CsvValues();
        result += string.Format(CultureInfo.InvariantCulture, ",{0},{1},{2},{3},{4},{5},{6},{7},{8},{9:F2},{10:F2},{11:F2},{12},{13:F2},{14:F2}",
            runManual, runEffects, totalSwarmSpawned, activeCount, killed, playerHealth.Hits, knockbackRequests, clockRequests, peakActiveCount, (double)activeCountSum / sampleCount,
            freezeDuration, effectRadius, swarmCount, swarmSpeed, swarmMass);
        results.Add(result);
        Debug.Log("EnemyComparison result: " + result, this);
    }

    private void CancelRun()
    {
        StopCoroutine(run);
        Time.timeScale = 1f;
        run = null;
        sampling = false;
        ClearEnemies();
        status = "Cancelled - no result saved";
    }

    private void ExportCsv()
    {
        StringBuilder csv = new("mode,count,layout,movingTarget,obstacles,samples,frameMeanMs,frameP95Ms,frameMaxMs,mainThreadMeanMs,gcMeanBytes,fixedDeltaTime,seed,innerRadius,outerRadiusAt500,warmupSeconds,measurementSeconds,collisionRadius,maxDepenetrationVelocity,chaseAcceleration,actualOuterRadius" + ComparisonPhysicsMetrics.CsvHeader + ",manualPlayer,effects,swarmSpawned,remainingEnemies,killed,playerHits,knockbackRequests,clockRequests,peakEnemies,meanEnemies,freezeDuration,effectRadius,swarmCount,swarmSpeed,swarmMass\n");
        for (int i = 0; i < results.Count; i++) csv.AppendLine(results[i]);
        string path = Path.Combine(Application.persistentDataPath, "EnemyCrowdComparison.csv");
        File.WriteAllText(path, csv.ToString());
        Debug.Log("EnemyComparison CSV: " + path, this);
    }

    private void OnGUI()
    {
        // 측정/워밍업 중에는 GUILayout·문자열 생성·결과 UI를 실행하지 않는다.
        if (!ready || run != null || sampling) return;
        GUILayout.BeginArea(new Rect(12, 12, 1100, 640), GUI.skin.box);
        GUILayout.Label("Enemy crowd comparison - isolated test scene");
        GUILayout.Label(status);
        GUILayout.Label($"Mode: {mode} | Layout: {layout} | Moving: {movingTarget} | Obstacles: {obstacles}");
        GUILayout.Label("G Grid / C Collider | 1:500 2:1000 3:1500 | M/O/D toggle | 4/5/6: radius | A: repeat suite | R: repeat | Esc: cancel");
        GUILayout.Label($"Collider radius: {colliderRadius:F2} | Depenetration: {maxDepenetrationVelocity:F2} | Acceleration: {chaseAcceleration:F2}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("4: Radius 0.50")) colliderRadius = 0.5f;
        if (GUILayout.Button("5: Radius 0.45")) colliderRadius = 0.45f;
        if (GUILayout.Button("6: Radius 0.40")) colliderRadius = 0.4f;
        GUILayout.EndHorizontal();
        GUILayout.Label($"P: manual WASD player = {manualPlayer} | V: auto effects = {exerciseEffects}");
        GUILayout.Label("While running: F freeze / K knockback / B swarm / X area damage / T pause / Esc cancel");
        GUILayout.Label("Physics CSV columns are separate main-thread markers; never add overlapping marker times.");
        GUILayout.Label("During runs the HUD is hidden. Watch CPU/Physics in the Unity Profiler.");
        GUILayout.Label("CSV: mode,count,layout,moving,obstacles,samples,frameMean,p95,max,mainThreadMean,GC bytes,fixed dt");
        if (GUILayout.Button("Export accumulated results to CSV")) ExportCsv();
        for (int i = Mathf.Max(0, results.Count - 12); i < results.Count; i++) GUILayout.Label(results[i]);
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (run != null) StopCoroutine(run);
        ClearEnemies();
        if (ownsPools)
        {
            ObjectPool.Instance.Unregister(PoolTypeEnums.Slime);
            ObjectPool.Instance.Unregister(PoolTypeEnums.TurtleMonster);
        }
        mainThreadRecorder.Dispose();
        gcRecorder.Dispose();
        physicsMetrics.Dispose();
        if (settingsSaved)
        {
            Time.timeScale = oldTimeScale;
            QualitySettings.vSyncCount = oldVSync;
            Application.targetFrameRate = oldFrameRate;
            Physics.simulationMode = oldSimulationMode;
            for (int i = 0; i < 32; i++)
            {
                Physics.IgnoreLayerCollision(EnemyLayer, i, oldLayerRules[i]);
                Physics.IgnoreLayerCollision(SwarmLayer, i, oldSwarmLayerRules[i]);
            }
        }
        if (templates != null) Destroy(templates);
        if (contactMaterial != null) Destroy(contactMaterial);
        if (testInput != null) Destroy(testInput);
    }
}
