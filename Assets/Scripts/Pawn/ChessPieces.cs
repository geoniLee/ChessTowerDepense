using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessPieces : MonoBehaviour
{
    public PawnState pawnState;
    public float attackRange;
    private int enemyLayerMask;
    private float attackDelay = 100;
    Collider2D enemy = null;

    void Awake()
    {
        pawnState = Resources.Load<PawnState>("State/Player/" + gameObject.name.Replace("(Clone)", ""));
        attackRange = pawnState.AttackRange;
        attackDelay = pawnState.AttackDelay;
        enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    // Update is called once per frame
    void Update()
    {
        if (enemy == null)
            enemy = Physics2D.OverlapCircle(transform.position, attackRange, enemyLayerMask);
        else if (enemy != null)
        {
            attackDelay -= Time.deltaTime;
            if (attackDelay <= 0)
            {
                Debug.Log("공격");
                Destroy(enemy.gameObject);
                enemy = null;

                attackDelay = pawnState.AttackDelay;
            }
        }

    }

    
}
