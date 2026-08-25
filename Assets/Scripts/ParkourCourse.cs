using UnityEngine;

public class ParkourCourse : MonoBehaviour
{
    [SerializeField] Sprite platformSprite;
    [SerializeField] Transform player;
    [SerializeField] MushroomEnemy mushroomPrefab;
    [SerializeField] Vector3 mushroomScale = new Vector3(2.5f, 2.5f, 2.5f);
    [SerializeField] float killY = -8f;

    Vector3 _spawnPosition;
    Rigidbody2D _playerBody;
    bool _built;
    int _checkpointIndex;

    struct Block
    {
        public string Name;
        public Vector2 Position;
        public Vector2 Size;
        public Color Color;

        public Block(string name, float x, float y, float width, float height, Color color)
        {
            Name = name;
            Position = new Vector2(x, y);
            Size = new Vector2(width, height);
            Color = color;
        }
    }

    struct Checkpoint
    {
        public float UnlockX;
        public float MinY;
        public Vector3 Spawn;

        public Checkpoint(float unlockX, float minY, float spawnX, float spawnY)
        {
            UnlockX = unlockX;
            MinY = minY;
            Spawn = new Vector3(spawnX, spawnY, 0f);
        }
    }

    struct MushroomSpot
    {
        public float X;
        public float Y;
        public float MinX;
        public float MaxX;
        public int Dir;

        public MushroomSpot(float x, float y, float minX, float maxX, int dir)
        {
            X = x;
            Y = y;
            MinX = minX;
            MaxX = maxX;
            Dir = dir;
        }
    }

    static readonly Color Ground = new Color(0.28f, 0.32f, 0.36f);
    static readonly Color Stone = new Color(0.72f, 0.75f, 0.80f);
    static readonly Color Accent = new Color(0.55f, 0.62f, 0.48f);
    static readonly Color Slide = new Color(0.48f, 0.34f, 0.30f);
    static readonly Color Climb = new Color(0.30f, 0.38f, 0.52f);
    static readonly Color Gold = new Color(0.93f, 0.76f, 0.22f);

    // Mostly flat combat road. Jump ~2.5 high / ~3.0 across.
    static readonly Checkpoint[] Checkpoints =
    {
        new Checkpoint(9f, -1f, 9.4f, 0.05f),
        new Checkpoint(26f, -1f, 26.4f, 0.05f),
        new Checkpoint(48f, -1f, 48.4f, 0.05f),
        new Checkpoint(68f, -1f, 68.4f, 0.05f)
    };

    static readonly MushroomSpot[] MushroomSpawns =
    {
        new MushroomSpot(-2.2f, 1.3f, -6.4f, 0.4f, 1),
        new MushroomSpot(5.8f, 1.3f, 1.8f, 8.8f, -1),

        new MushroomSpot(15.2f, 1.3f, 12.8f, 18.4f, 1),
        new MushroomSpot(23.6f, 1.3f, 19.6f, 26.6f, -1),

        new MushroomSpot(33.4f, 1.3f, 31.0f, 36.4f, 1),
        new MushroomSpot(40.2f, 1.3f, 37.2f, 43.2f, -1),
        new MushroomSpot(46.8f, 1.3f, 44.0f, 48.6f, 1),

        new MushroomSpot(54.6f, 1.3f, 52.6f, 57.4f, -1),
        new MushroomSpot(61.2f, 1.3f, 58.4f, 63.8f, 1),
        new MushroomSpot(67.2f, 1.3f, 64.6f, 68.8f, -1),

        new MushroomSpot(74.2f, 1.3f, 72.2f, 76.6f, 1),
        new MushroomSpot(80.0f, 1.3f, 77.4f, 82.6f, -1)
    };

    void Awake()
    {
        if (player == null)
        {
            GameObject found = GameObject.Find("Player");
            if (found != null)
                player = found.transform;
        }

        if (player != null)
        {
            _playerBody = player.GetComponent<Rigidbody2D>();
            _spawnPosition = player.position;
        }

        if (platformSprite == null)
            platformSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);

        Build();
    }

    void Update()
    {
        if (player == null)
            return;

        if (_checkpointIndex < Checkpoints.Length)
        {
            Checkpoint next = Checkpoints[_checkpointIndex];
            if (player.position.x >= next.UnlockX && player.position.y >= next.MinY)
            {
                _spawnPosition = next.Spawn;
                _checkpointIndex++;
            }
        }

        if (player.position.y < killY)
            Respawn();
    }

    void Build()
    {
        if (_built)
            return;

        _built = true;
        Block[] layout = GetLayout();
        for (int i = 0; i < layout.Length; i++)
            CreateBlock(layout[i]);

        SpawnMushrooms();
    }

    static Block[] GetLayout()
    {
        return new[]
        {
            new Block("StartWall", -16.4f, 2.6f, 1.4f, 7.2f, Slide),
            new Block("StartFloor", -12f, -0.5f, 8f, 1f, Ground),

            new Block("A1_Floor", 1f, -0.5f, 18f, 1f, Ground),

            new Block("A2_Floor", 20f, -0.5f, 16f, 1f, Accent),

            new Block("A3_Floor", 40.1f, -0.5f, 19.8f, 1f, Ground),

            new Block("A4_Floor", 60.9f, -0.5f, 18.2f, 1f, Accent),

            new Block("Finish", 77.6f, -0.4f, 13.2f, 1.2f, Gold),
            new Block("FinishBack", 84.6f, 2.6f, 1.2f, 5.2f, Slide)
        };
    }

    void SpawnMushrooms()
    {
        if (mushroomPrefab == null)
            return;

        for (int i = 0; i < MushroomSpawns.Length; i++)
        {
            MushroomSpot spot = MushroomSpawns[i];
            MushroomEnemy enemy = Instantiate(
                mushroomPrefab,
                new Vector3(spot.X, spot.Y, 0f),
                Quaternion.identity);
            enemy.name = "Mushroom_" + (i + 1);
            enemy.transform.localScale = mushroomScale;
            enemy.transform.SetParent(transform, true);
            enemy.ConfigurePatrol(spot.MinX, spot.MaxX, spot.Dir);
        }
    }

    void CreateBlock(Block block)
    {
        GameObject go = new GameObject(block.Name);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(block.Position.x, block.Position.y, 0f);
        go.transform.localScale = new Vector3(block.Size.x, block.Size.y, 1f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = platformSprite;
        renderer.color = block.Color;
        renderer.sortingOrder = -1;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.offset = Vector2.zero;
        collider.size = Vector2.one;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;
    }

    void Respawn()
    {
        if (player == null)
            return;

        player.position = _spawnPosition;
        if (_playerBody != null)
            _playerBody.linearVelocity = Vector2.zero;
    }

    void OnDrawGizmos()
    {
        Block[] layout = GetLayout();
        for (int i = 0; i < layout.Length; i++)
        {
            Block block = layout[i];
            Gizmos.color = new Color(block.Color.r, block.Color.g, block.Color.b, 0.9f);
            Gizmos.DrawCube(
                new Vector3(block.Position.x, block.Position.y, 0f),
                new Vector3(block.Size.x, block.Size.y, 0.2f));
        }

        Gizmos.color = new Color(0.85f, 0.25f, 0.2f, 0.7f);
        for (int i = 0; i < MushroomSpawns.Length; i++)
        {
            MushroomSpot spot = MushroomSpawns[i];
            Gizmos.DrawWireCube(new Vector3(spot.X, spot.Y, 0f), new Vector3(0.7f, 0.9f, 0.2f));
            Gizmos.DrawLine(new Vector3(spot.MinX, spot.Y, 0f), new Vector3(spot.MaxX, spot.Y, 0f));
        }

        Gizmos.color = new Color(0.8f, 0.15f, 0.15f, 0.35f);
        Gizmos.DrawCube(new Vector3(34f, killY, 0f), new Vector3(120f, 0.2f, 0.2f));
    }
}
