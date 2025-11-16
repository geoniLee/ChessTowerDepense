using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileSpawner : MonoBehaviour
{
    public Tilemap tilemap;
    public GameObject prefab;

    // 제외 영역 정의 (좌 1, 우 1, 위 5줄 제외 → 내부 8×4)
    public int leftEx = 1;
    public int rightEx = 1;
    public int topEx = 5;

    // 내부 유효 영역 캐시
    private int xMin, xMax, yMin, yMax;

    // 이미 프리팹이 생성(점유)된 셀들
    private HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();

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

        // 점유 기록
        occupied.Add(target);

        Debug.Log($"[TileSpawner] Spawned at {target}. Remain: {candidates.Count - 1}");
    }

    /// 합성 또는 이동 시: 출발지점 A는 해제, 도착지점 B는 새 점유
    public void MoveOccupancy(Vector3Int fromCell, Vector3Int toCell)
    {
        // 출발지점 해제
        if (occupied.Contains(fromCell))
        {
            occupied.Remove(fromCell);
            Debug.Log($"[TileSpawner] {fromCell} 점유 해제됨.");
        }

        // 도착지점 점유
        if (!occupied.Contains(toCell))
        {
            occupied.Add(toCell);
            Debug.Log($"[TileSpawner] {toCell} 새로 점유됨.");
        }
        else
        {
            Debug.LogWarning($"[TileSpawner] {toCell} 은 이미 점유된 셀입니다!");
        }
    }

}
