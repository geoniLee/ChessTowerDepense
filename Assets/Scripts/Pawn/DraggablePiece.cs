using UnityEngine;

/// <summary>
/// 기물을 드래그하여 이동/합성할 수 있게 하는 컴포넌트
/// PC 마우스 드래그 = 모바일 터치 드래그 (직접 입력 처리)
/// </summary>
public class DraggablePiece : MonoBehaviour
{
    private TileSpawner spawner;
    private Vector3 originalPosition;
    private Vector3Int originalCell;
    private bool isDragging = false;
    
    // 드래그 중 시각 효과
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private int originalSortingOrder;

    void Start()
    {
        // TileSpawner 참조 찾기
        spawner = FindObjectOfType<TileSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[DraggablePiece] TileSpawner를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalSortingOrder = spriteRenderer.sortingOrder;
        }

        // 디버깅: 컴포넌트 상태 확인
        var collider = GetComponent<Collider2D>();
        Debug.Log($"[DraggablePiece] {gameObject.name} 초기화 완료 - Collider:{collider != null}, Spawner:{spawner != null}");
    }

    void OnMouseDown()
    {
        if (spawner == null) return;

        Debug.Log($"[DraggablePiece] 클릭 감지됨: {gameObject.name}");
        
        isDragging = true;
        originalPosition = transform.position;
        originalCell = spawner.WorldToCell(originalPosition);

        // 드래그 중 시각 효과: 반투명 + 맨 앞으로
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.7f);
            spriteRenderer.sortingOrder = 100;
        }

        Debug.Log($"[DraggablePiece] 드래그 시작: {gameObject.name} at {originalCell}");
    }

    void OnMouseDrag()
    {
        if (!isDragging || spawner == null) return;

        // 마우스 위치를 월드 좌표로 변환
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z; // 카메라와의 거리
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = transform.position.z; // 원래 Z 좌표 유지
        
        transform.position = worldPos;
    }

    void OnMouseUp()
    {
        if (!isDragging || spawner == null) return;
        
        isDragging = false;

        // 시각 효과 원래대로
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.sortingOrder = originalSortingOrder;
        }

        // 드롭한 위치의 셀 계산
        Vector3Int dropCell = spawner.WorldToCell(transform.position);
        
        Debug.Log($"[DraggablePiece] 드래그 종료: {originalCell} → {dropCell}");

        // 유효한 셀인지 확인
        if (!spawner.IsValidCell(dropCell))
        {
            Debug.LogWarning($"[DraggablePiece] 유효하지 않은 셀 {dropCell}. 원위치로 복귀.");
            transform.position = originalPosition;
            return;
        }

        // 같은 셀이면 무시
        if (dropCell == originalCell)
        {
            Debug.Log("[DraggablePiece] 같은 위치에 드롭. 원위치 복귀.");
            transform.position = originalPosition;
            return;
        }

        // 드롭한 셀에 기물이 있는지 확인
        if (spawner.IsOccupied(dropCell))
        {
            // 합성 시도
            bool merged = spawner.TryMergePieces(gameObject, originalCell, dropCell);
            
            if (!merged)
            {
                // 합성 실패 시 원위치
                Debug.Log("[DraggablePiece] 합성 불가. 원위치 복귀.");
                transform.position = originalPosition;
            }
            // 합성 성공 시 이 오브젝트는 Destroy됨
        }
        else
        {
            // 빈 셀이면 이동
            spawner.MovePiece(gameObject, originalCell, dropCell);
        }
    }
}
