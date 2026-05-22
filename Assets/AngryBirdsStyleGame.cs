using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AngryBirdsStyleGame : MonoBehaviour
{
    private const float LaunchPower = 8.4f;
    private const float MaxPullDistance = 3.35f;

    private readonly List<BirdData> birds = new();
    private readonly List<GameObject> activeEnemies = new();
    private readonly List<GameObject> activeObjects = new();
    private readonly List<LevelData> levels = new();

    private Camera mainCamera;
    private Transform slingshotAnchor;
    private LineRenderer slingLine;
    private Text hudText;
    private Text messageText;
    private GameObject currentBird;
    private Rigidbody2D currentBirdBody;
    private Bird currentBirdScript;
    private bool dragging;
    private bool launched;
    private int levelIndex;
    private int birdIndex;
    private int score;
    private float settleTimer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartGame()
    {
        if (FindObjectOfType<AngryBirdsStyleGame>() != null)
        {
            return;
        }

        new GameObject("Angry Birds Style Game").AddComponent<AngryBirdsStyleGame>();
    }

    private void Awake()
    {
        Physics2D.gravity = new Vector2(0f, -9.81f);
        BuildLevels();
        SetupCamera();
        ClearOldGameplay();
        BuildUi();
        LoadLevel(0);
    }

    private void Update()
    {
        HandleInput();
        UpdateCamera();
        UpdateHud();
        CheckRoundState();

        if (Input.GetKeyDown(KeyCode.R))
        {
            LoadLevel(levelIndex);
        }
    }

    private void BuildLevels()
    {
        levels.Add(new LevelData("Training Field", new[]
        {
            BlockSpec.Wood(new Vector2(5.8f, -2.55f), new Vector2(0.35f, 2.1f), 0f),
            BlockSpec.Wood(new Vector2(7.2f, -2.55f), new Vector2(0.35f, 2.1f), 0f),
            BlockSpec.Glass(new Vector2(6.5f, -1.35f), new Vector2(1.75f, 0.3f), 0f),
            BlockSpec.Wood(new Vector2(6.5f, -0.55f), new Vector2(1.35f, 0.3f), 0f),
        }, new[]
        {
            new EnemySpec(new Vector2(6.5f, -0.95f), 0.42f),
            new EnemySpec(new Vector2(7.95f, -2.6f), 0.42f),
        }, new[]
        {
            new HazardSpec(new Vector2(5.15f, -2.62f)),
        }));

        levels.Add(new LevelData("Glass Ridge", new[]
        {
            BlockSpec.Stone(new Vector2(5.2f, -2.55f), new Vector2(0.45f, 2.2f), 0f),
            BlockSpec.Glass(new Vector2(6.3f, -2.4f), new Vector2(0.28f, 2.55f), -8f),
            BlockSpec.Glass(new Vector2(7.45f, -2.4f), new Vector2(0.28f, 2.55f), 8f),
            BlockSpec.Wood(new Vector2(6.85f, -1.05f), new Vector2(2.5f, 0.3f), 0f),
            BlockSpec.Wood(new Vector2(6.85f, 0.05f), new Vector2(1.6f, 0.28f), 0f),
            BlockSpec.Stone(new Vector2(8.3f, -2.65f), new Vector2(0.6f, 0.6f), 45f),
        }, new[]
        {
            new EnemySpec(new Vector2(6.85f, -0.52f), 0.42f),
            new EnemySpec(new Vector2(6.85f, 0.55f), 0.38f),
            new EnemySpec(new Vector2(8.95f, -2.55f), 0.44f),
        }, new[]
        {
            new HazardSpec(new Vector2(7.7f, -2.55f)),
        }));

        levels.Add(new LevelData("Fort Boom", new[]
        {
            BlockSpec.Stone(new Vector2(5.1f, -2.5f), new Vector2(0.5f, 2.5f), 0f),
            BlockSpec.Stone(new Vector2(8.45f, -2.5f), new Vector2(0.5f, 2.5f), 0f),
            BlockSpec.Wood(new Vector2(6.75f, -1.15f), new Vector2(3.9f, 0.35f), 0f),
            BlockSpec.Glass(new Vector2(5.85f, -0.15f), new Vector2(0.28f, 1.75f), 0f),
            BlockSpec.Glass(new Vector2(7.65f, -0.15f), new Vector2(0.28f, 1.75f), 0f),
            BlockSpec.Stone(new Vector2(6.75f, 0.8f), new Vector2(2.3f, 0.35f), 0f),
            BlockSpec.Wood(new Vector2(6.1f, 1.35f), new Vector2(0.4f, 1.1f), -20f),
            BlockSpec.Wood(new Vector2(7.4f, 1.35f), new Vector2(0.4f, 1.1f), 20f),
        }, new[]
        {
            new EnemySpec(new Vector2(6.75f, -0.6f), 0.45f),
            new EnemySpec(new Vector2(5.85f, 1.22f), 0.38f),
            new EnemySpec(new Vector2(7.65f, 1.22f), 0.38f),
            new EnemySpec(new Vector2(9.25f, -2.55f), 0.46f),
        }, new[]
        {
            new HazardSpec(new Vector2(6.75f, -2.55f)),
            new HazardSpec(new Vector2(8.95f, -2.55f)),
        }));
    }

    private void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5.2f;
        mainCamera.transform.position = new Vector3(2.6f, 0.1f, -10f);
        mainCamera.backgroundColor = new Color(0.52f, 0.78f, 0.95f);
    }

    private void ClearOldGameplay()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root == gameObject || root.GetComponent<Camera>() != null || root.GetComponent<Canvas>() != null)
            {
                continue;
            }

            Destroy(root);
        }
    }

    private void BuildUi()
    {
        GameObject canvasObject = new("Game UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        hudText = CreateText(canvasObject.transform, "HUD", new Vector2(12f, -12f), TextAnchor.UpperLeft, 22, Color.white);
        messageText = CreateText(canvasObject.transform, "Message", new Vector2(0f, -18f), TextAnchor.UpperCenter, 30, new Color(1f, 0.94f, 0.42f));
        messageText.text = string.Empty;
    }

    private Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int size, Color color)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = anchor == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = anchor == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(900f, 120f);
        return text;
    }

    private void LoadLevel(int newLevelIndex)
    {
        levelIndex = Mathf.Clamp(newLevelIndex, 0, levels.Count - 1);
        birdIndex = 0;
        score = levelIndex == 0 ? 0 : score;
        launched = false;
        dragging = false;
        settleTimer = 0f;
        messageText.text = string.Empty;

        foreach (GameObject activeObject in activeObjects)
        {
            if (activeObject != null)
            {
                Destroy(activeObject);
            }
        }

        activeObjects.Clear();
        activeEnemies.Clear();
        birds.Clear();
        birds.Add(new BirdData(BirdKind.Red, "Red", new Color(0.95f, 0.15f, 0.1f)));
        birds.Add(new BirdData(BirdKind.Blue, "Split", new Color(0.18f, 0.5f, 1f)));
        birds.Add(new BirdData(BirdKind.Yellow, "Dash", new Color(1f, 0.85f, 0.05f)));
        birds.Add(new BirdData(BirdKind.Black, "Bomb", new Color(0.08f, 0.08f, 0.1f)));

        BuildWorld(levels[levelIndex]);
        SpawnNextBird();
    }

    private void BuildWorld(LevelData level)
    {
        CreateGround();
        CreateBackdrop();

        GameObject slingshotPost = CreatePrimitive("Slingshot Anchor", new Vector2(-5.5f, -1.55f), new Vector2(0.18f, 1.9f), new Color(0.32f, 0.15f, 0.04f), false);
        slingshotPost.GetComponent<Collider2D>().enabled = false;
        slingshotAnchor = slingshotPost.transform;
        GameObject fork = CreatePrimitive("Slingshot Fork", new Vector2(-5.25f, -0.72f), new Vector2(0.9f, 0.18f), new Color(0.35f, 0.18f, 0.06f), false);
        fork.GetComponent<Collider2D>().enabled = false;
        fork.transform.rotation = Quaternion.Euler(0f, 0f, 18f);

        GameObject lineObject = new("Launch Guide");
        slingLine = lineObject.AddComponent<LineRenderer>();
        slingLine.positionCount = 2;
        slingLine.startWidth = 0.05f;
        slingLine.endWidth = 0.05f;
        slingLine.material = new Material(Shader.Find("Sprites/Default"));
        slingLine.startColor = new Color(0.18f, 0.1f, 0.04f);
        slingLine.endColor = new Color(0.18f, 0.1f, 0.04f);
        activeObjects.Add(lineObject);

        foreach (BlockSpec block in level.Blocks)
        {
            CreateBlock(block);
        }

        foreach (EnemySpec enemy in level.Enemies)
        {
            CreateEnemy(enemy);
        }

        foreach (HazardSpec hazard in level.Hazards)
        {
            CreateTnt(hazard.Position);
        }
    }

    private void CreateBackdrop()
    {
        GameObject sun = CreateCircle("Sun", new Vector2(-2.8f, 2.9f), 0.62f, new Color(1f, 0.84f, 0.24f), false);
        sun.GetComponent<Collider2D>().enabled = false;
        GameObject cloudA = CreatePrimitive("Cloud", new Vector2(0f, 2.55f), new Vector2(2.2f, 0.45f), new Color(1f, 1f, 1f, 0.75f), false);
        cloudA.GetComponent<Collider2D>().enabled = false;
        GameObject cloudB = CreatePrimitive("Cloud", new Vector2(5.2f, 3.15f), new Vector2(1.9f, 0.38f), new Color(1f, 1f, 1f, 0.6f), false);
        cloudB.GetComponent<Collider2D>().enabled = false;
    }

    private void CreateGround()
    {
        GameObject ground = CreatePrimitive("Rolling Grass Ground", new Vector2(2.8f, -3.35f), new Vector2(18f, 0.7f), new Color(0.28f, 0.62f, 0.22f), false);
        ground.layer = LayerMask.NameToLayer("Default");
        ground.AddComponent<SurfaceEffector2D>();
        PhysicsMaterial2D groundMaterial = new() { friction = 0.8f, bounciness = 0.05f };
        ground.GetComponent<Collider2D>().sharedMaterial = groundMaterial;

        GameObject dirt = CreatePrimitive("Dirt", new Vector2(2.8f, -3.8f), new Vector2(18f, 0.35f), new Color(0.45f, 0.25f, 0.11f), false);
        dirt.GetComponent<Collider2D>().enabled = false;
    }

    private void CreateBlock(BlockSpec block)
    {
        GameObject blockObject = CreatePrimitive(block.MaterialName + " Block", block.Position, block.Size, block.Color, true);
        blockObject.transform.rotation = Quaternion.Euler(0f, 0f, block.Rotation);
        Destructible destructible = blockObject.AddComponent<Destructible>();
        destructible.Setup(block.Health, block.ScoreValue, this, block.ParticleColor, block.ExplosionRadius);
        Rigidbody2D rb = blockObject.GetComponent<Rigidbody2D>();
        rb.mass = block.Mass;
        rb.linearDamping = 0.15f;
        rb.angularDamping = 0.2f;
    }

    private void CreateEnemy(EnemySpec enemy)
    {
        GameObject enemyObject = CreateCircle("Grumpy Target", enemy.Position, enemy.Radius, new Color(0.3f, 0.92f, 0.28f), true);
        Enemy enemyScript = enemyObject.AddComponent<Enemy>();
        enemyScript.Setup(this);
        Rigidbody2D rb = enemyObject.GetComponent<Rigidbody2D>();
        rb.mass = 0.75f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.05f;
        activeEnemies.Add(enemyObject);

        GameObject brow = CreatePrimitive("Angry Brow", enemy.Position + new Vector2(0f, enemy.Radius * 0.45f), new Vector2(enemy.Radius * 1.2f, 0.08f), Color.black, false);
        brow.transform.SetParent(enemyObject.transform, true);
        brow.transform.rotation = Quaternion.Euler(0f, 0f, 12f);
    }

    private void CreateTnt(Vector2 position)
    {
        GameObject tnt = CreatePrimitive("TNT", position, new Vector2(0.58f, 0.58f), new Color(0.92f, 0.08f, 0.04f), true);
        Explosive explosive = tnt.AddComponent<Explosive>();
        explosive.Setup(this, 3.2f, 8.5f);
        Rigidbody2D rb = tnt.GetComponent<Rigidbody2D>();
        rb.mass = 0.7f;
    }

    private void SpawnNextBird()
    {
        if (birdIndex >= birds.Count)
        {
            currentBird = null;
            currentBirdBody = null;
            currentBirdScript = null;
            return;
        }

        BirdData data = birds[birdIndex];
        currentBird = CreateCircle(data.Name + " Bird", slingshotAnchor.position, 0.38f, data.Color, true);
        currentBirdBody = currentBird.GetComponent<Rigidbody2D>();
        currentBirdBody.bodyType = RigidbodyType2D.Kinematic;
        currentBirdBody.linearVelocity = Vector2.zero;
        currentBirdBody.angularVelocity = 0f;
        currentBirdScript = currentBird.AddComponent<Bird>();
        currentBirdScript.Setup(data.Kind, this);
        launched = false;
        birdIndex++;
    }

    private void HandleInput()
    {
        if (currentBird == null || launched)
        {
            if (currentBirdScript != null && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                currentBirdScript.ActivateAbility();
            }
            return;
        }

        Vector2 pointerWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0) && Vector2.Distance(pointerWorld, currentBird.transform.position) < 1.2f)
        {
            dragging = true;
        }

        if (dragging && Input.GetMouseButton(0))
        {
            Vector2 pull = pointerWorld - (Vector2)slingshotAnchor.position;
            pull = Vector2.ClampMagnitude(pull, MaxPullDistance);
            currentBird.transform.position = (Vector2)slingshotAnchor.position + pull;
        }

        if (dragging && Input.GetMouseButtonUp(0))
        {
            dragging = false;
            launched = true;
            currentBirdBody.bodyType = RigidbodyType2D.Dynamic;
            currentBirdBody.linearVelocity = ((Vector2)slingshotAnchor.position - (Vector2)currentBird.transform.position) * LaunchPower;
            currentBirdBody.angularVelocity = -180f;
        }

        slingLine.enabled = currentBird != null;
        if (slingLine.enabled)
        {
            slingLine.SetPosition(0, slingshotAnchor.position);
            slingLine.SetPosition(1, currentBird.transform.position);
        }
    }

    private void UpdateCamera()
    {
        Vector3 target = new(2.6f, 0.1f, -10f);
        if (currentBird != null && launched)
        {
            target.x = Mathf.Clamp(currentBird.transform.position.x, -1.5f, 6.3f);
            target.y = Mathf.Clamp(currentBird.transform.position.y, -0.5f, 2.0f);
        }

        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, target, Time.deltaTime * 2.3f);
    }

    private void UpdateHud()
    {
        LevelData level = levels[levelIndex];
        int enemiesLeft = 0;
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemiesLeft++;
            }
        }

        string activeBirdName = currentBirdScript != null ? currentBirdScript.DisplayName : "None";
        hudText.text =
            $"Level {levelIndex + 1}/{levels.Count}: {level.Name}\n" +
            $"Score: {score}\n" +
            $"Enemies left: {enemiesLeft}\n" +
            $"Bird: {activeBirdName}  | Birds left: {Mathf.Max(0, birds.Count - birdIndex)}\n" +
            "Drag the bird, release to launch. Click or Space after launch for special \n ability. R restarts.";
    }

    private void CheckRoundState()
    {
        activeEnemies.RemoveAll(enemy => enemy == null);
        if (activeEnemies.Count == 0)
        {
            if (levelIndex + 1 < levels.Count)
            {
                messageText.text = "Level cleared!";
                Invoke(nameof(NextLevel), 1.5f);
            }
            else
            {
                messageText.text = "All forts crushed. You win!";
            }
            enabled = false;
            return;
        }

        if (currentBird == null)
        {
            if (birdIndex >= birds.Count)
            {
                messageText.text = "Out of birds. Press R to retry.";
                return;
            }

            SpawnNextBird();
            return;
        }

        if (!launched)
        {
            return;
        }

        bool settled = currentBirdBody == null || currentBirdBody.linearVelocity.magnitude < 0.18f;
        bool outOfBounds = currentBird.transform.position.x > 12f || currentBird.transform.position.y < -6f;
        settleTimer = settled ? settleTimer + Time.deltaTime : 0f;
        if (settleTimer > 1.2f || outOfBounds)
        {
            Destroy(currentBird);
            currentBird = null;
            currentBirdBody = null;
            currentBirdScript = null;
            settleTimer = 0f;
        }
    }

    private void NextLevel()
    {
        enabled = true;
        LoadLevel(levelIndex + 1);
    }

    public void AddScore(int amount)
    {
        score += amount;
    }

    public void DestroyEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        AddScore(500);
        SpawnParticles(enemy.transform.position, new Color(0.35f, 1f, 0.25f), 32, 1.15f);
        activeEnemies.Remove(enemy);
        Destroy(enemy);
    }

    public void Explode(Vector2 position, float radius, float force)
    {
        SpawnParticles(position, new Color(1f, 0.42f, 0.08f), 70, 1.8f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);
        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody != null)
            {
                Vector2 direction = ((Vector2)hit.transform.position - position).normalized;
                hit.attachedRigidbody.AddForce(direction * force, ForceMode2D.Impulse);
            }

            Destructible destructible = hit.GetComponent<Destructible>();
            if (destructible != null)
            {
                destructible.Damage(4f);
            }

            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Damage(4f);
            }
        }
    }

    public void SpawnSplitBird(Vector2 position, Vector2 velocity, float verticalOffset)
    {
        GameObject split = CreateCircle("Split Bird", position + new Vector2(0f, verticalOffset), 0.26f, new Color(0.18f, 0.5f, 1f), true);
        Rigidbody2D rb = split.GetComponent<Rigidbody2D>();
        rb.linearVelocity = velocity + new Vector2(0f, verticalOffset * 3f);
        rb.mass = 0.65f;
        Bird bird = split.AddComponent<Bird>();
        bird.Setup(BirdKind.Red, this);
    }

    public void SpawnParticles(Vector2 position, Color color, int count, float size)
    {
        GameObject particleObject = new("Particle Explosion");
        particleObject.transform.position = position;
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
        main.startLifetime = 0.55f;
        main.startSpeed = 6f;
        main.startSize = size * 0.12f;
        main.maxParticles = count;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;
        ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        particles.Emit(count);
        Destroy(particleObject, 1.4f);
    }

    private GameObject CreatePrimitive(string name, Vector2 position, Vector2 size, Color color, bool dynamicBody)
    {
        GameObject obj = new(name);
        obj.transform.position = position;
        obj.transform.localScale = size;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = SpriteCache.Square;
        renderer.color = color;
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        if (dynamicBody)
        {
            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        else
        {
            collider.usedByEffector = false;
        }
        activeObjects.Add(obj);
        return obj;
    }

    private GameObject CreateCircle(string name, Vector2 position, float radius, Color color, bool dynamicBody)
    {
        GameObject obj = new(name);
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * radius * 2f;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = SpriteCache.Circle;
        renderer.color = color;
        CircleCollider2D collider = obj.AddComponent<CircleCollider2D>();
        if (dynamicBody)
        {
            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        else
        {
            collider.enabled = false;
        }
        activeObjects.Add(obj);
        return obj;
    }

    private readonly struct BirdData
    {
        public readonly BirdKind Kind;
        public readonly string Name;
        public readonly Color Color;

        public BirdData(BirdKind kind, string name, Color color)
        {
            Kind = kind;
            Name = name;
            Color = color;
        }
    }

    private readonly struct LevelData
    {
        public readonly string Name;
        public readonly BlockSpec[] Blocks;
        public readonly EnemySpec[] Enemies;
        public readonly HazardSpec[] Hazards;

        public LevelData(string name, BlockSpec[] blocks, EnemySpec[] enemies, HazardSpec[] hazards)
        {
            Name = name;
            Blocks = blocks;
            Enemies = enemies;
            Hazards = hazards;
        }
    }

    private readonly struct EnemySpec
    {
        public readonly Vector2 Position;
        public readonly float Radius;

        public EnemySpec(Vector2 position, float radius)
        {
            Position = position;
            Radius = radius;
        }
    }

    private readonly struct HazardSpec
    {
        public readonly Vector2 Position;

        public HazardSpec(Vector2 position)
        {
            Position = position;
        }
    }

    private readonly struct BlockSpec
    {
        public readonly string MaterialName;
        public readonly Vector2 Position;
        public readonly Vector2 Size;
        public readonly float Rotation;
        public readonly Color Color;
        public readonly Color ParticleColor;
        public readonly float Health;
        public readonly float Mass;
        public readonly int ScoreValue;
        public readonly float ExplosionRadius;

        private BlockSpec(string materialName, Vector2 position, Vector2 size, float rotation, Color color, Color particleColor, float health, float mass, int scoreValue, float explosionRadius)
        {
            MaterialName = materialName;
            Position = position;
            Size = size;
            Rotation = rotation;
            Color = color;
            ParticleColor = particleColor;
            Health = health;
            Mass = mass;
            ScoreValue = scoreValue;
            ExplosionRadius = explosionRadius;
        }

        public static BlockSpec Wood(Vector2 position, Vector2 size, float rotation)
        {
            return new BlockSpec("Wood", position, size, rotation, new Color(0.62f, 0.36f, 0.15f), new Color(0.76f, 0.48f, 0.22f), 2.2f, 1.1f, 120, 0.25f);
        }

        public static BlockSpec Glass(Vector2 position, Vector2 size, float rotation)
        {
            return new BlockSpec("Glass", position, size, rotation, new Color(0.45f, 0.85f, 1f, 0.82f), new Color(0.5f, 0.9f, 1f), 1.25f, 0.65f, 160, 0.2f);
        }

        public static BlockSpec Stone(Vector2 position, Vector2 size, float rotation)
        {
            return new BlockSpec("Stone", position, size, rotation, new Color(0.42f, 0.42f, 0.46f), new Color(0.5f, 0.5f, 0.55f), 3.8f, 2.4f, 220, 0.12f);
        }
    }

    private static class SpriteCache
    {
        private static Sprite square;
        private static Sprite circle;

        public static Sprite Square => square != null ? square : square = CreateSquareSprite();
        public static Sprite Circle => circle != null ? circle : circle = CreateCircleSprite();

        private static Sprite CreateSquareSprite()
        {
            Texture2D texture = new(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreateCircleSprite()
        {
            Texture2D texture = new(64, 64);
            Color[] pixels = new Color[64 * 64];
            Vector2 center = new(31.5f, 31.5f);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(32f - distance);
                    pixels[y * 64 + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}

public enum BirdKind
{
    Red,
    Blue,
    Yellow,
    Black
}

public class Bird : MonoBehaviour
{
    private BirdKind kind;
    private AngryBirdsStyleGame game;
    private Rigidbody2D rb;
    private bool abilityUsed;

    public string DisplayName => kind switch
    {
        BirdKind.Blue => "Split Bird",
        BirdKind.Yellow => "Dash Bird",
        BirdKind.Black => "Bomb Bird",
        _ => "Red Bird"
    };

    public void Setup(BirdKind birdKind, AngryBirdsStyleGame owner)
    {
        kind = birdKind;
        game = owner;
        rb = GetComponent<Rigidbody2D>();
        rb.mass = kind == BirdKind.Black ? 1.6f : 1f;
    }

    public void ActivateAbility()
    {
        if (abilityUsed || rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        abilityUsed = true;
        if (kind == BirdKind.Blue)
        {
            game.SpawnSplitBird(transform.position, rb.linearVelocity, 0.42f);
            game.SpawnSplitBird(transform.position, rb.linearVelocity, -0.42f);
        }
        else if (kind == BirdKind.Yellow)
        {
            rb.linearVelocity *= 1.8f;
            game.SpawnParticles(transform.position, new Color(1f, 0.9f, 0.12f), 18, 0.75f);
        }
        else if (kind == BirdKind.Black)
        {
            game.Explode(transform.position, 2.2f, 6f);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.relativeVelocity.magnitude > 5.5f)
        {
            game.SpawnParticles(transform.position, GetComponent<SpriteRenderer>().color, 12, 0.65f);
        }
    }
}

public class Destructible : MonoBehaviour
{
    private const float StartupArmDelay = 0.75f;

    private static readonly List<Destructible> StablePieces = new();
    private static bool structureReleased;

    private float health;
    private int scoreValue;
    private AngryBirdsStyleGame game;
    private Color particleColor;
    private float explosionRadius;
    private float armedAt;
    private Rigidbody2D rb;
    private RigidbodyConstraints2D originalConstraints;

    public void Setup(float startingHealth, int score, AngryBirdsStyleGame owner, Color particles, float blastRadius)
    {
        health = startingHealth;
        scoreValue = score;
        game = owner;
        particleColor = particles;
        explosionRadius = blastRadius;
        armedAt = Time.time + StartupArmDelay;
        structureReleased = false;
        rb = GetComponent<Rigidbody2D>();
        originalConstraints = rb.constraints;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        if (!StablePieces.Contains(this))
        {
            StablePieces.Add(this);
        }
    }

    public void Damage(float amount)
    {
        if (Time.time < armedAt)
        {
            return;
        }

        ReleaseStructure();
        health -= amount;
        if (health <= 0f)
        {
            Break();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time < armedAt)
        {
            return;
        }

        Damage(collision.relativeVelocity.magnitude * 0.28f);
    }

    private void OnDestroy()
    {
        StablePieces.Remove(this);
    }

    private void ReleaseStructure()
    {
        if (structureReleased)
        {
            return;
        }

        structureReleased = true;
        Enemy.ReleaseTargets();
        foreach (Destructible piece in StablePieces)
        {
            if (piece == null || piece.rb == null)
            {
                continue;
            }

            piece.rb.constraints = piece.originalConstraints;
            piece.rb.WakeUp();
        }
    }

    private void Break()
    {
        game.AddScore(scoreValue);
        game.SpawnParticles(transform.position, particleColor, 24, transform.localScale.magnitude);
        if (explosionRadius > 0.2f)
        {
            Collider2D[] nearObjects = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (Collider2D nearObject in nearObjects)
            {
                if (nearObject.attachedRigidbody != null)
                {
                    nearObject.attachedRigidbody.AddForce(((Vector2)nearObject.transform.position - (Vector2)transform.position).normalized * 1.2f, ForceMode2D.Impulse);
                }
            }
        }
        Destroy(gameObject);
    }
}

public class Enemy : MonoBehaviour
{
    private static readonly List<Enemy> StableTargets = new();
    private static bool targetsReleased;

    private AngryBirdsStyleGame game;
    private float health = 2.2f;
    private Rigidbody2D rb;
    private RigidbodyConstraints2D originalConstraints;

    public void Setup(AngryBirdsStyleGame owner)
    {
        game = owner;
        targetsReleased = false;
        rb = GetComponent<Rigidbody2D>();
        originalConstraints = rb.constraints;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        if (!StableTargets.Contains(this))
        {
            StableTargets.Add(this);
        }
    }

    public void Damage(float amount)
    {
        ReleaseTargets();
        health -= amount;
        if (health <= 0f)
        {
            game.DestroyEnemy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!targetsReleased && collision.gameObject.GetComponent<Bird>() == null)
        {
            return;
        }

        Damage(collision.relativeVelocity.magnitude * 0.35f);
    }

    private void OnDestroy()
    {
        StableTargets.Remove(this);
    }

    public static void ReleaseTargets()
    {
        if (targetsReleased)
        {
            return;
        }

        targetsReleased = true;
        foreach (Enemy target in StableTargets)
        {
            if (target == null || target.rb == null)
            {
                continue;
            }

            target.rb.constraints = target.originalConstraints;
            target.rb.WakeUp();
        }
    }
}

public class Explosive : MonoBehaviour
{
    private AngryBirdsStyleGame game;
    private float radius;
    private float force;
    private bool exploded;

    public void Setup(AngryBirdsStyleGame owner, float blastRadius, float blastForce)
    {
        game = owner;
        radius = blastRadius;
        force = blastForce;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!exploded && collision.gameObject.GetComponent<Bird>() != null && collision.relativeVelocity.magnitude > 2.8f)
        {
            exploded = true;
            game.Explode(transform.position, radius, force);
            Destroy(gameObject);
        }
    }
}
