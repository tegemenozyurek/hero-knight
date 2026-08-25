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
    static readonly Color Drop = new Color(0.42f, 0.28f, 0.46f);
    static readonly Color Short = new Color(0.62f, 0.18f, 0.18f);
    static readonly Color Gold = new Color(0.93f, 0.76f, 0.22f);

    static readonly Checkpoint[] Checkpoints =
    {
        new Checkpoint(21f, -1f, 20.8f, 0.7f),
        new Checkpoint(39f, -1f, 39.2f, 0.7f),
        new Checkpoint(57f, 9.5f, 58.4f, 11.2f),
        new Checkpoint(75f, 8.5f, 76.8f, 10.1f),
        new Checkpoint(88f, -1f, 89f, 0.7f),
        new Checkpoint(110f, 8.5f, 111.2f, 10.1f)
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
            // Start
            new Block("StartFloor", -6f, -0.5f, 18f, 1f, Ground),
            new Block("StartWall", -15.2f, 3f, 1.2f, 8f, Slide),

            // A: long RIGHT slide (hold D). Wall hangs above the pad.
            new Block("A_Step1", 5.4f, 0.7f, 2.6f, 0.5f, Stone),
            new Block("A_Step2", 8.8f, 2.1f, 2.5f, 0.5f, Stone),
            new Block("A_Step3", 12.2f, 3.6f, 2.5f, 0.5f, Stone),
            new Block("A_Ledge", 16f, 5.3f, 3.4f, 0.55f, Accent),
            new Block("A_SlideWall", 19.6f, 6.4f, 1.3f, 8f, Slide),
            new Block("A_Landing", 20.8f, 0.25f, 8.4f, 0.5f, Accent),

            // B: LEFT slide (hold A). Drop into a hole, wall on your left.
            new Block("B_Rise1", 27.2f, 1.5f, 2.4f, 0.5f, Stone),
            new Block("B_Rise2", 30.4f, 3f, 2.4f, 0.5f, Stone),
            new Block("B_LedgeL", 33.4f, 6.9f, 4.4f, 0.55f, Accent),
            new Block("B_LeftWall", 35.35f, 3.45f, 1.25f, 6.5f, Slide),
            new Block("B_LedgeR", 41f, 6.9f, 3.4f, 0.55f, Stone),
            new Block("B_Landing", 39.2f, 0.25f, 8.2f, 0.5f, Accent),

            // C: tight climb. Right wall is shorter so you can exit over it.
            new Block("C_Entry", 47.2f, 0.25f, 3.6f, 0.5f, Stone),
            new Block("C_WallL", 50.4f, 6.2f, 1.2f, 12.2f, Climb),
            new Block("C_WallR", 54.6f, 4.7f, 1.2f, 8.4f, Climb),
            new Block("C_PitFloor", 52.5f, 0.15f, 3.2f, 0.3f, Ground),
            new Block("C_Exit", 58.4f, 10.7f, 4.2f, 0.6f, Accent),

            // D: wide climb. Longer wall jump.
            new Block("D_Entry", 63.2f, 10.7f, 3.4f, 0.6f, Stone),
            new Block("D_WallL", 66.4f, 6.4f, 1.2f, 11.4f, Climb),
            new Block("D_WallR", 72.8f, 4.9f, 1.2f, 8.6f, Climb),
            new Block("D_PitFloor", 69.6f, 0.15f, 5.4f, 0.3f, Ground),
            new Block("D_Exit", 76.8f, 9.6f, 4.2f, 0.6f, Accent),

            // E: walk off a high ledge into a wall, slide the rest.
            new Block("E_DropLedge", 81.8f, 9.6f, 3.6f, 0.6f, Stone),
            new Block("E_DropWall", 85.9f, 5.1f, 1.3f, 7.4f, Drop),
            new Block("E_Landing", 86.8f, 0.25f, 8f, 0.5f, Accent),

            // F: zigzag up and right. Stand on the RIGHT of each wall, hold A, jump off.
            new Block("F_Rise", 93.6f, 1.5f, 2.6f, 0.5f, Stone),
            new Block("F_Ledge1", 97.8f, 3.15f, 3.8f, 0.5f, Accent),
            new Block("F_Wall1", 96.2f, 5.5f, 1.2f, 5.2f, Climb),
            new Block("F_Ledge2", 104.2f, 6.35f, 3.6f, 0.5f, Stone),
            new Block("F_Wall2", 102.7f, 8.7f, 1.2f, 5.2f, Climb),
            new Block("F_Exit", 111.2f, 9.55f, 4f, 0.55f, Accent),

            // G: red short wall should not slide. Blue tall wall should.
            new Block("G_Floor", 117.2f, 0.25f, 7.2f, 0.5f, Stone),
            new Block("G_ShortWall", 121.8f, 0.85f, 1.2f, 0.7f, Short),
            new Block("G_Approach", 125.4f, 0.25f, 3.2f, 0.5f, Stone),
            new Block("G_TallWall", 124.1f, 3.2f, 1.25f, 5.4f, Climb),
            new Block("G_HighPad", 129.8f, 4.7f, 3.2f, 0.5f, Accent),

            new Block("Finish", 135.4f, 4.7f, 6.6f, 0.9f, Gold),
            new Block("FinishBack", 138.9f, 7.3f, 1.2f, 4.4f, Slide)
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
            Gizmos.DrawCube(new Vector3(block.Position.x, block.Position.y, 0f), new Vector3(block.Size.x, block.Size.y, 0.2f));
        }

        Gizmos.color = new Color(0.8f, 0.15f, 0.15f, 0.35f);
        Gizmos.DrawCube(new Vector3(60f, killY, 0f), new Vector3(160f, 0.2f, 0.2f));
    }
}
