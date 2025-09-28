using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyNav : MonoBehaviour
{
    public EnemyState enemyState;
    private List<Vector2> wayPoints = new List<Vector2>();
    private Rigidbody2D rb;
    private int curIndex = 0;
    private Vector2 FirstDirection;
    private float speed => enemyState.Speed;

    void Awake()
    {
        enemyState = Resources.Load<EnemyState>("State/Enemy/" + gameObject.name.Replace("(Clone)", ""));
    }
    void Start()
    {
        for (int i = 0; i < GameManager.Instance.WayPoints.Count; i++)
            wayPoints.Add(GameManager.Instance.WayPoints[i].position);

        rb = GetComponent<Rigidbody2D>();
        if (wayPoints[0].y < transform.position.y)
            FirstDirection = Vector2.down;
        else
            FirstDirection = Vector2.up;
        rb.velocity = FirstDirection * speed;
    }

    void Update()
    {
        if (curIndex < 2 && Vector2.Distance(rb.position, wayPoints[curIndex]) < 0.2f)
        {
            rb.velocity = curIndex == 0 ? Vector2.right * speed : FirstDirection * -speed;
            curIndex++;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Goal") && curIndex == 2)
        {
            Debug.Log("도착");
            Destroy(gameObject, 0.2f);
        }
    }
}
