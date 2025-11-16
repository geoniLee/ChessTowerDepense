using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileSpawner : MonoBehaviour
{
    public Tilemap tilemap;
    public GameObject prefab; // 초기 생성용 (등급 0)
    
    // 등급별 기물 프리팹 배열 (0: 기본, 1: 1단계, 2: 2단계, ...)
    public GameObject[] gradePrefabs;

    // 제외 영역 정의 (좌 1, 우 1, 위 5줄 제외 → 내부 8×4)
    public int leftEx = 1;
    public int rightEx = 1;
    public int topEx = 5;

    // 내부 유효 영역 캐시
    private int xMin, xMax, yMin, yMax;

    // 이미 프리팹이 생성(점유)된 셀들
    private HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();
    
    // 셀 위치 → 생성된 기물 오브젝트 매핑
    private Dictionary<Vector3Int, GameObject> cellToObject = new Dictionary<Vector3Int, GameObject>();

    // 타일맵 경계 설정
    void Start()
    {
        tilemap.CompressBounds();

        var b = tilemap.cellBounds;
        xMin = b.xMin + leftEx;         // 포함
        xMax = b.xMin + 10 - rightEx;   // 미포함 상한
        yMin = b.yMin;                  // 포함
        yMax = b.yMin + 9 - topEx;      // 미포함 상한

        Debug.Log($"[TileSpawner] Ready. Area X:{xMin}~{xMax - 1}, Y:{yMin}~{yMax - 1}");
    }

    #region 기물 생성
    /// 버튼 클릭 시: 8×4 영역 내에서 '아직 점유되지 않은' 랜덤 셀 1곳에 프리팹 생성
    public void SpawnRandom()
    {
        var candidates = new List<Vector3Int>();

        for (int y = yMin; y < yMax; y++)
        {
            for (int x = xMin; x < xMax; x++)
            {
                var cell = new Vector3Int(x, y, 0);

                // 타일이 존재하고, 아직 점유되지 않은 셀만 후보에 추가
                if (tilemap.HasTile(cell) && !occupied.Contains(cell))
                    candidates.Add(cell);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[TileSpawner] 생성 가능한 빈 칸이 없습니다. (모두 점유됨)");
            return;
        }

        var target = candidates[Random.Range(0, candidates.Count)];
        var localPos = tilemap.GetCellCenterLocal(target);

        var go = Instantiate(prefab, tilemap.transform);
        go.transform.localPosition = localPos + new Vector3(0, 0, -0.1f);

        // DraggablePiece 컴포넌트 추가 (없으면)
        if (go.GetComponent<DraggablePiece>() == null)
            go.AddComponent<DraggablePiece>();

        // 점유 기록 및 매핑
        occupied.Add(target);
        cellToObject[target] = go;

        Debug.Log($"[TileSpawner] Spawned at {target}. Remain: {candidates.Count - 1}");
    }
    #endregion

    #region 기물 합성
    
    /// 특정 셀의 기물 오브젝트 가져오기
    public GameObject GetPieceAt(Vector3Int cell)
    {
        return cellToObject.TryGetValue(cell, out var obj) ? obj : null;
    }

    /// 월드 좌표 → 셀 좌표 변환
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return tilemap.WorldToCell(worldPos);
    }

    /// 셀이 유효한 영역 내에 있는지 확인
    public bool IsValidCell(Vector3Int cell)
    {
        return cell.x >= xMin && cell.x < xMax && cell.y >= yMin && cell.y < yMax;
    }

    /// 셀이 점유되어 있는지 확인
    public bool IsOccupied(Vector3Int cell)
    {
        return occupied.Contains(cell);
    }

    /// 기물 이동 처리 (단순 이동)
    public void MovePiece(GameObject piece, Vector3Int fromCell, Vector3Int toCell)
    {
        if (!IsValidCell(toCell))
        {
            Debug.LogWarning($"[TileSpawner] {toCell}은 유효하지 않은 셀입니다.");
            return;
        }

        if (IsOccupied(toCell))
        {
            Debug.LogWarning($"[TileSpawner] {toCell}은 이미 점유된 셀입니다.");
            return;
        }

        // 셀 매핑 업데이트
        cellToObject.Remove(fromCell);
        cellToObject[toCell] = piece;

        // 점유 상태 업데이트
        occupied.Remove(fromCell);
        occupied.Add(toCell);

        // 기물 위치 이동
        var localPos = tilemap.GetCellCenterLocal(toCell);
        piece.transform.localPosition = localPos + new Vector3(0, 0, -0.1f);

        Debug.Log($"[TileSpawner] 기물 이동: {fromCell} → {toCell}");
    }

    /// 기물 합성 처리 (같은 등급이면 상위 프리팹으로 업그레이드)
    public bool TryMergePieces(GameObject draggedPiece, Vector3Int fromCell, Vector3Int toCell)
    {
        var targetPiece = GetPieceAt(toCell);
        if (targetPiece == null)
        {
            // 빈 셀이면 단순 이동
            MovePiece(draggedPiece, fromCell, toCell);
            return false;
        }

        // 두 기물의 등급 확인
        var draggedCP = draggedPiece.GetComponent<ChessPieces>();
        var targetCP = targetPiece.GetComponent<ChessPieces>();

        if (draggedCP == null || targetCP == null)
        {
            Debug.LogWarning("[TileSpawner] ChessPieces 컴포넌트가 없습니다.");
            return false;
        }

        // 등급이 같은지 확인
        if (draggedCP.grade != targetCP.grade)
        {
            Debug.Log($"[TileSpawner] 등급이 다릅니다. ({draggedCP.grade} ≠ {targetCP.grade}) 합성 불가.");
            return false;
        }

        int currentGrade = targetCP.grade;
        int nextGrade = currentGrade + 1;

        // 상위 프리팹이 있는지 확인
        if (gradePrefabs == null || nextGrade >= gradePrefabs.Length)
        {
            Debug.LogWarning($"[TileSpawner] 등급 {nextGrade} 프리팹이 없습니다. 최대 등급입니다.");
            return false;
        }

        Debug.Log($"[TileSpawner] 합성 성공: 등급 {currentGrade} → {nextGrade}");
        
        // 기존 기물들 제거
        cellToObject.Remove(fromCell);
        occupied.Remove(fromCell);
        Destroy(draggedPiece);
        
        cellToObject.Remove(toCell);
        Destroy(targetPiece);

        // 상위 등급 프리팹 생성
        var localPos = tilemap.GetCellCenterLocal(toCell);
        var upgradedPiece = Instantiate(gradePrefabs[nextGrade], tilemap.transform);
        upgradedPiece.transform.localPosition = localPos + new Vector3(0, 0, -0.1f);

        // 등급 설정
        var upgradedCP = upgradedPiece.GetComponent<ChessPieces>();
        if (upgradedCP != null)
        {
            upgradedCP.grade = nextGrade;
        }

        // DraggablePiece 컴포넌트 추가
        if (upgradedPiece.GetComponent<DraggablePiece>() == null)
            upgradedPiece.AddComponent<DraggablePiece>();

        // 새 기물 매핑
        cellToObject[toCell] = upgradedPiece;
        occupied.Add(toCell);

        Debug.Log($"[TileSpawner] 등급 {nextGrade} 기물 생성 완료");
        
        return true;
    }
    #endregion
}
