using UnityEngine;

public class ParkourCourse : MonoBehaviour
{
    [SerializeField] Sprite platformSprite;
    [SerializeField] Transform player;
    [SerializeField] MushroomEnemy[] mushroomPrefabs;
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
        public int Prefab;
        public float X;
        public float Y;
        public float MinX;
        public float MaxX;
        public int Dir;

        public MushroomSpot(int prefab, float x, float y, float minX, float maxX, int dir)
        {
            Prefab = prefab;
            X = x;
            Y = y;
            MinX = minX;
            MaxX = maxX;
            Dir = dir;
        }
    }

    static readonly Color Ground = new Color(0.28f, 0.32f, 0.36f);
    static readonly Color Slide = new Color(0.48f, 0.34f, 0.30f);
    static readonly Color Gold = new Color(0.93f, 0.76f, 0.22f);
    static readonly Color BabyCol = new Color(0.70f, 0.78f, 0.52f);
    static readonly Color YoungCol = new Color(0.55f, 0.62f, 0.48f);
    static readonly Color FastCol = new Color(0.30f, 0.52f, 0.66f);
    static readonly Color MatureCol = new Color(0.46f, 0.38f, 0.34f);
    static readonly Color GiantCol = new Color(0.26f, 0.50f, 0.32f);

    static readonly Checkpoint[] Checkpoints =
    {
        new Checkpoint(-6.8f, -1f, -6.6f, 0.05f),
        new Checkpoint(3.2f, -1f, 4.8f, 0.05f),
        new Checkpoint(16.2f, -1f, 15.2f, 0.05f),
        new Checkpoint(39.2f, -1f, 38.2f, 0.05f),
        new Checkpoint(53.6f, -1f, 52.8f, 0.05f),
        new Checkpoint(72f, -1f, 72.8f, 0.15f)
    };

    static readonly MushroomSpot[] MushroomSpawns =
    {
        new MushroomSpot(0, -1.4f, 0.85f, -4.4f, 1.6f, 1),
        new MushroomSpot(1, 10f, 0.95f, 5.4f, 14.6f, -1),
        new MushroomSpot(2, 31f, 0.85f, 24.2f, 37.8f, 1),
        new MushroomSpot(3, 47f, 1.05f, 41.8f, 52.2f, -1),
        new MushroomSpot(4, 62.6f, 1.2f, 56f, 69.2f, 1)
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
            new Block("StartWall", -17.6f, 2.6f, 1.4f, 7.2f, Slide),
            new Block("StartFloor", -12f, -0.5f, 10f, 1f, Ground),
            new Block("BabyFloor", -1.4f, -0.5f, 8f, 1f, BabyCol),
            new Block("YoungFloor", 10f, -0.5f, 11.2f, 1f, YoungCol),
            new Block("HopA", 17.5f, 0.15f, 1.6f, 0.5f, Slide),
            new Block("HopB", 20.3f, 0.85f, 1.5f, 0.5f, Slide),
            new Block("FastFloor", 31f, -0.5f, 16f, 1f, FastCol),
            new Block("MatureFloor", 47f, -0.5f, 12.8f, 1f, MatureCol),
            new Block("GiantFloor", 62.6f, -0.5f, 16f, 1f, GiantCol),
            new Block("Finish", 77.2f, -0.4f, 10.4f, 1.2f, Gold),
            new Block("FinishBack", 82.8f, 2.8f, 1.2f, 5.6f, Slide)
        };
    }

    void SpawnMushrooms()
    {
        if (mushroomPrefabs == null || mushroomPrefabs.Length == 0)
            return;

        for (int i = 0; i < MushroomSpawns.Length; i++)
        {
            MushroomSpot spot = MushroomSpawns[i];
            if (spot.Prefab < 0 || spot.Prefab >= mushroomPrefabs.Length)
                continue;

            MushroomEnemy prefab = mushroomPrefabs[spot.Prefab];
            if (prefab == null)
                continue;

            MushroomEnemy enemy = Instantiate(
                prefab,
                new Vector3(spot.X, spot.Y, 0f),
                Quaternion.identity);
            enemy.name = prefab.name;
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
        Gizmos.DrawCube(new Vector3(33f, killY, 0f), new Vector3(110f, 0.2f, 0.2f));
    }
}
