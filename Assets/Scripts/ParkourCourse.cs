using UnityEngine;

public class ParkourCourse : MonoBehaviour
{
    [SerializeField] Sprite platformSprite;
    [SerializeField] Transform player;
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

    static readonly Color Ground = new Color(0.28f, 0.32f, 0.36f);
    static readonly Color Stone = new Color(0.72f, 0.75f, 0.80f);
    static readonly Color Accent = new Color(0.55f, 0.62f, 0.48f);
    static readonly Color Slide = new Color(0.48f, 0.34f, 0.30f);
    static readonly Color Climb = new Color(0.30f, 0.38f, 0.52f);
    static readonly Color Gold = new Color(0.93f, 0.76f, 0.22f);

    // Jump reach ~2.5 high / ~3.0 across. Wall jump ~1.2 high / ~2.6 across.
    static readonly Checkpoint[] Checkpoints =
    {
        new Checkpoint(16.5f, -1f, 17.2f, 0.65f),
        new Checkpoint(31.5f, 7.2f, 32.4f, 8.55f),
        new Checkpoint(41.5f, 6.4f, 42.6f, 7.75f),
        new Checkpoint(55.5f, 7.6f, 56.4f, 8.95f)
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
    }

    static Block[] GetLayout()
    {
        return new[]
        {
            // Hall
            new Block("StartFloor", -6f, -0.5f, 16f, 1f, Ground),
            new Block("StartWall", -14.2f, 2.6f, 1.2f, 7.2f, Slide),

            new Block("A_Step", 4.1f, 0.4f, 2.6f, 0.5f, Stone),
            new Block("A_Pad", 8.2f, 1.15f, 2.8f, 0.5f, Stone),

            // Drop-slide. Hold D, ride the wall down, drop onto the pad.
            new Block("B_Ledge", 12.2f, 3.2f, 3.1f, 0.5f, Accent),
            new Block("B_Wall", 15.55f, 4f, 1.2f, 5.6f, Slide),
            new Block("B_Landing", 16.9f, 0.2f, 6.6f, 0.4f, Accent),
            new Block("B_TunnelFloor", 21.8f, 0.2f, 6.4f, 0.4f, Ground),
            new Block("B_TunnelCeiling", 21.8f, 1.52f, 5.2f, 0.7f, Slide),

            // Tight well. Inner gap ~2.55 — one short wall kick across.
            new Block("C_Entry", 22.4f, 0.2f, 3.2f, 0.4f, Stone),
            new Block("C_WallL", 25.15f, 4.7f, 1.15f, 9f, Climb),
            new Block("C_WallR_Low", 28.85f, 1.75f, 1.15f, 3.1f, Climb),
            new Block("C_Rest", 30.35f, 3.5f, 2.1f, 0.4f, Accent),
            new Block("C_WallR_High", 28.85f, 5.9f, 1.15f, 3.6f, Climb),
            new Block("C_Exit", 32.5f, 8.15f, 3.4f, 0.5f, Accent),

            // Committed 3.0 gap. Miss = fall.
            new Block("D_Run", 36.6f, 8.15f, 2.6f, 0.5f, Stone),
            new Block("D_Land", 42.4f, 7.35f, 3f, 0.5f, Accent),

            // Short second well, then a long slide down to the end.
            new Block("E_Entry", 46.8f, 7.35f, 2.6f, 0.5f, Stone),
            new Block("E_WallL", 49.2f, 6.2f, 1.15f, 7.4f, Climb),
            new Block("E_WallR", 52.85f, 4.7f, 1.15f, 5.2f, Climb),
            new Block("E_Exit", 56.5f, 8.55f, 3.2f, 0.5f, Accent),

            new Block("F_Ledge", 60.2f, 8.55f, 2.8f, 0.5f, Stone),
            new Block("F_Wall", 63.35f, 4.7f, 1.2f, 7.2f, Slide),
            new Block("F_Landing", 64.6f, 0.25f, 6.4f, 0.5f, Accent),

            new Block("Finish", 70.4f, 0.45f, 6.2f, 0.9f, Gold),
            new Block("FinishBack", 73.7f, 2.9f, 1.2f, 4f, Slide)
        };
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

        Gizmos.color = new Color(0.8f, 0.15f, 0.15f, 0.35f);
        Gizmos.DrawCube(new Vector3(30f, killY, 0f), new Vector3(90f, 0.2f, 0.2f));
    }
}
