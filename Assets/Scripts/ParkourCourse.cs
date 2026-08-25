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

    // Jump reach ~2.5 high / ~3.0 across. Wall jump ~1.2 high / ~2.6 across.
    static readonly Checkpoint[] Checkpoints =
    {
        new Checkpoint(12f, 0.9f, 13.6f, 1.55f),
        new Checkpoint(30f, -0.1f, 31.2f, 0.7f),
        new Checkpoint(49f, 7.6f, 50.2f, 8.6f),
        new Checkpoint(66f, 6.9f, 67.2f, 7.8f)
    };

    static readonly MushroomSpot[] MushroomSpawns =
    {
        new MushroomSpot(0.5f, 1.3f, -4.0f, 4.8f, 1),
        new MushroomSpot(16.2f, 2.7f, 12.2f, 20.1f, -1),
        new MushroomSpot(33.4f, 1.75f, 29.2f, 37.4f, 1),
        new MushroomSpot(52.4f, 9.7f, 48.4f, 56.4f, -1),
        new MushroomSpot(68.9f, 8.9f, 65.6f, 72.0f, 1)
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
            new Block("StartFloor", -10.5f, -0.5f, 9f, 1f, Ground),
            new Block("StartWall", -15.6f, 2.6f, 1.4f, 7.2f, Slide),

            new Block("M1_Floor", 0.5f, -0.5f, 11f, 1f, Ground),

            new Block("A_Step", 9.2f, 0.5f, 2.6f, 0.5f, Stone),
            new Block("M2_Floor", 16.2f, 1.15f, 10.2f, 0.5f, Accent),

            new Block("B_Ledge", 24.2f, 3.15f, 3.4f, 0.5f, Stone),
            new Block("B_Wall", 27.5f, 4f, 1.2f, 5.6f, Slide),
            new Block("M3_Floor", 33.4f, 0.22f, 10.4f, 0.44f, Accent),

            new Block("C_Entry", 40.2f, 0.22f, 3.2f, 0.44f, Stone),
            new Block("C_WallL", 42.5f, 4.7f, 1.15f, 9f, Climb),
            new Block("C_WallR_Low", 46.2f, 1.75f, 1.15f, 3.1f, Climb),
            new Block("C_Rest", 47.7f, 3.5f, 2f, 0.4f, Accent),
            new Block("C_WallR_High", 46.2f, 6.05f, 1.15f, 3.9f, Climb),
            new Block("M4_Floor", 52.4f, 8.15f, 10.4f, 0.5f, Accent),

            new Block("D_Run", 60.2f, 8.15f, 2.8f, 0.5f, Stone),
            new Block("M5_Floor", 68.9f, 7.35f, 8.6f, 0.5f, Accent),

            new Block("F_Ledge", 75.4f, 7.35f, 2.6f, 0.5f, Stone),
            new Block("F_Wall", 78.5f, 4.05f, 1.2f, 7.1f, Slide),
            new Block("Finish", 83.2f, 0.4f, 7.2f, 0.9f, Gold),
            new Block("FinishBack", 86.9f, 2.9f, 1.2f, 4.2f, Slide)
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
        Gizmos.DrawCube(new Vector3(36f, killY, 0f), new Vector3(110f, 0.2f, 0.2f));
    }
}
