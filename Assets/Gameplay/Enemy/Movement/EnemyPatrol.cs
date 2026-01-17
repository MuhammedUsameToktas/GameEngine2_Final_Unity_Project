using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float patrolDistance = 3f;

    private Vector3 startPos;
    private int direction = 1;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * direction * speed * Time.deltaTime);

        if (Vector3.Distance(startPos, transform.position) >= patrolDistance)
        {
            direction *= -1;
            transform.Rotate(0f, 180f, 0f);
        }
    }
}
