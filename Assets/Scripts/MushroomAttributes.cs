using UnityEngine;

[DisallowMultipleComponent]
public class MushroomAttributes : MonoBehaviour
{
    [SerializeField] int maxHealth = 3;
    [SerializeField] float moveSpeed = 1.45f;
    [SerializeField] float chaseSpeed = 2.4f;
    [SerializeField] bool useAttack2;

    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public float ChaseSpeed => chaseSpeed;
    public bool UseAttack2 => useAttack2;

    void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        chaseSpeed = Mathf.Max(0.1f, chaseSpeed);
    }
}
