using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EnemyNav : MonoBehaviour
{
    public EnemyState enemyState;
    private List<Vector2> wayPoints = new List<Vector2>();
    private Rigidbody2D rb;
    private int curIndex = 0;
    private Vector2 FirstDirection;
    private float speed => enemyState.Speed;
    private int currentHealth = 1;
    private int maxHealth = 1;
    [Header("UI")]
    public Slider healthSlider; // 할당되어 있지 않으면 런타임에 자동 생성

    void Awake()
    {
        enemyState = Resources.Load<EnemyState>("State/Enemy/" + gameObject.name.Replace("(Clone)", ""));
        if (enemyState != null)
        {
            maxHealth = Mathf.Max(1, enemyState.Health);
            currentHealth = maxHealth;
        }
    }
    void Start()
    {
        for (int i = 0; i < GameManager.Instance.wayPoints.Count; i++)
            wayPoints.Add(GameManager.Instance.wayPoints[i].position);
        rb = GetComponent<Rigidbody2D>();
        if (wayPoints[0].y < transform.position.y)
            FirstDirection = Vector2.down;
        else
            FirstDirection = Vector2.up;
        rb.velocity = FirstDirection * speed;

        UpdateHealthSlider();
    }

    void Update()
    {
        if (curIndex < 2 && Vector2.Distance(rb.position, wayPoints[curIndex]) < 0.2f)
        {
            rb.velocity = curIndex == 0 ? Vector2.right * speed : FirstDirection * -speed;
            curIndex++;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Goal") && curIndex == 2)
        {
            Debug.Log("도착");
            Destroy(gameObject, 0.2f);
        }
    }
    
    public void Damaged(int damage)
    {
        if ((currentHealth - damage) <= 0)
        {
            Debug.Log("적 제거");
            Destroy(gameObject);
        }
        else
        {
            currentHealth -= damage;
            UpdateHealthSlider();
        }
    }

    private void UpdateHealthSlider()
    {
        if (healthSlider == null) return;
        if (maxHealth <= 0) maxHealth = 1;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = 1f;
        healthSlider.value = Mathf.Clamp01((float)currentHealth / (float)maxHealth);
    }

    // 외부에서 호출하기 위한 보조 메서드
    public void TakeDamage(int damage)
    {
        Damaged(damage);
    }

    // ApplySlow(Vector2(slowRate, slowTime)) 형태로 호출 가능
    public void ApplySlow(Vector2 slowParams)
    {
        float slowRate = slowParams.x;
        float slowTime = slowParams.y;
        StartCoroutine(ApplySlowCoroutine(slowRate, slowTime));
    }

    private IEnumerator ApplySlowCoroutine(float slowRate, float slowTime)
    {
        if (rb == null) yield break;
        var origVel = rb.velocity.normalized * speed;;
        rb.velocity = origVel * (1f - Mathf.Clamp01(slowRate));
        yield return new WaitForSeconds(slowTime);
        if (rb != null)
            rb.velocity = origVel;
    }

    // StopForSeconds(float seconds) 호출 가능
    public void StopForSeconds(float seconds)
    {
        StartCoroutine(StopForSecondsCoroutine(seconds));
    }

    private IEnumerator StopForSecondsCoroutine(float seconds)
    {
        if (rb == null) yield break;
        var orig = rb.velocity;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(seconds);
        if (rb != null)
            rb.velocity = orig;
    }
}
