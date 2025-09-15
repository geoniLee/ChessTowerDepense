using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyNav : MonoBehaviour
{
    public List<Vector2> wayPoints = new List<Vector2>();
    Rigidbody2D rb;
    float speed = 2f;
    private int curIndex = 0;
    Vector2 target;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = (wayPoints[curIndex] - (Vector2)transform.position).normalized * speed;

        target = wayPoints[curIndex];
    }

    // Update is called once per frame
    void Update()
    {
        if (curIndex >= wayPoints.Count) return;

        if (Vector2.Distance(transform.position, target) < 0.05f)
        {
            curIndex++;
            if (curIndex < wayPoints.Count){
                target = wayPoints[curIndex];
                rb.velocity = (target - (Vector2)transform.position).normalized * speed;
            }
            else
                Destroy(gameObject);
        }
    }
}
