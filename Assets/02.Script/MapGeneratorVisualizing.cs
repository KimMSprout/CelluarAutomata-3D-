using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGeneratorVisualizing : MonoBehaviour
{
    public int width;
    public int height;

    public string seed;
    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent;

    // 셀룰러 오토마타 진행 과정 시각화 여부
    public bool isVisualize = true;

    // 시각화 속도
    public float visualizeDelay = 0.05f;

    int[,] map;

    // 현재 규칙을 적용하고 있는 중심 셀
    Vector2Int currentCell = new Vector2Int(-1, -1);

    // 현재 검사 중인 인접 셀
    Vector2Int currentNeighbor = new Vector2Int(-1, -1);

    // 이미 검사가 끝난 인접 셀
    List<Vector2Int> checkedNeighbors = new List<Vector2Int>();

    Coroutine smoothCoroutine;

    private void Start()
    {
        RegenerateMap();
    }

    // Inspector의 점 세 개 메뉴에서 실행 가능
    [ContextMenu("Generate Map")]
    public void RegenerateMap()
    {
        if (smoothCoroutine != null)
        {
            StopCoroutine(smoothCoroutine);
        }

        GenerateMap();
        RandomFillMap();

        currentCell = new Vector2Int(-1, -1);
        currentNeighbor = new Vector2Int(-1, -1);

        checkedNeighbors.Clear();

        smoothCoroutine = StartCoroutine(SmoothMapCoroutine());
    }

    // 배열 초기화
    void GenerateMap()
    {
        map = new int[width, height];
    }

    // 랜덤하게 초기 맵 생성
    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = DateTime.Now.Ticks.ToString();
        }

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 맵의 테두리는 벽으로 설정
                if (x == 0 || x == width - 1 ||
                    y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] =
                        pseudoRandom.Next(0, 100) < randomFillPercent
                        ? 1
                        : 0;
                }
            }
        }
    }

    // 셀룰러 오토마타 반복
    IEnumerator SmoothMapCoroutine()
    {
        // 처음 생성된 랜덤 맵 확인
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < 5; i++)
        {
            if (isVisualize)
            {
                yield return StartCoroutine(SmoothMapVisualized());
            }
            else
            {
                SmoothMap();
            }

            Debug.Log($"SmoothMap {i + 1}회 적용");

            // 한 세대의 결과를 잠시 확인
            yield return new WaitForSeconds(1f);
        }

        currentCell = new Vector2Int(-1, -1);
        currentNeighbor = new Vector2Int(-1, -1);

        checkedNeighbors.Clear();

        smoothCoroutine = null;
    }

    // 시각화하지 않을 때 사용하는 SmoothMap
    void SmoothMap()
    {
        int[,] newMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallTiles = GetSurroundingWallCount(x, y);

                if (x == 0 || x == width - 1 ||
                    y == 0 || y == height - 1)
                {
                    newMap[x, y] = 1;
                }
                else if (neighborWallTiles > 4)
                {
                    newMap[x, y] = 1;
                }
                else
                {
                    newMap[x, y] = 0;
                }
            }
        }

        map = newMap;
    }

    // 시각화용 SmoothMap
    IEnumerator SmoothMapVisualized()
    {
        int[,] newMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 새로운 중심 셀 검사 시작
                currentCell = new Vector2Int(x, y);
                currentNeighbor = new Vector2Int(-1, -1);

                // 이전 셀에서 검사했던 인접 셀 초기화
                checkedNeighbors.Clear();

                // 현재 중심 셀을 초록색으로 잠시 표시
                yield return new WaitForSeconds(visualizeDelay);

                int wallCount = 0;

                // 주변 3 x 3 순회
                for (int neighborX = x - 1;
                     neighborX <= x + 1;
                     neighborX++)
                {
                    for (int neighborY = y - 1;
                         neighborY <= y + 1;
                         neighborY++)
                    {
                        // 자기 자신 제외
                        if (neighborX == x && neighborY == y)
                        {
                            continue;
                        }

                        // 맵 내부 셀인 경우
                        if (neighborX >= 0 &&
                            neighborX < width &&
                            neighborY >= 0 &&
                            neighborY < height)
                        {
                            Vector2Int neighborPos =
                                new Vector2Int(neighborX, neighborY);

                            // 현재 검사 중인 셀 → 빨간색
                            currentNeighbor = neighborPos;

                            yield return new WaitForSeconds(visualizeDelay);

                            // 해당 셀이 벽이면 카운트
                            wallCount += map[neighborX, neighborY];

                            // 검사가 끝난 셀을 checkedNeighbors에 추가
                            checkedNeighbors.Add(neighborPos);

                            // 현재 검사 중인 셀 해제 → 이제 주황색으로 표시됨
                            currentNeighbor = new Vector2Int(-1, -1);

                            yield return new WaitForSeconds(visualizeDelay);
                        }
                        else
                        {
                            // 맵 밖은 벽으로 취급
                            wallCount++;
                        }
                    }
                }

                // 계산 결과를 새로운 맵에 저장
                if (x == 0 || x == width - 1 ||
                    y == 0 || y == height - 1)
                {
                    newMap[x, y] = 1;
                }
                else if (wallCount > 4)
                {
                    newMap[x, y] = 1;
                }
                else
                {
                    newMap[x, y] = 0;
                }

                // 인접 셀 검사 완료 후 잠시 결과 확인
                yield return new WaitForSeconds(visualizeDelay);

                // 다음 중심 셀로 넘어가기 전에 초기화
                currentCell = new Vector2Int(-1, -1);
                currentNeighbor = new Vector2Int(-1, -1);
                checkedNeighbors.Clear();
            }
        }

        // 한 세대 계산이 모두 끝난 후 맵 교체
        map = newMap;
    }

    // 일반 실행에서 사용하는 주변 벽 개수 검사
    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;

        for (int neighborX = gridX - 1;
             neighborX <= gridX + 1;
             neighborX++)
        {
            for (int neighborY = gridY - 1;
                 neighborY <= gridY + 1;
                 neighborY++)
            {
                if (neighborX >= 0 &&
                    neighborX < width &&
                    neighborY >= 0 &&
                    neighborY < height)
                {
                    if (neighborX != gridX ||
                        neighborY != gridY)
                    {
                        wallCount += map[neighborX, neighborY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    // Gizmo로 맵 표시
    void OnDrawGizmos()
    {
        if (map == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int position = new Vector2Int(x, y);

                // 기본 색상
                Gizmos.color =
                    map[x, y] == 1
                    ? Color.black
                    : Color.white;

                if (isVisualize)
                {
                    // 이미 검사가 끝난 인접 셀 → 주황색
                    if (checkedNeighbors.Contains(position))
                    {
                        Gizmos.color = new Color(1f, 0.5f, 0f);
                    }

                    // 현재 중심 셀 → 초록색
                    if (currentCell == position)
                    {
                        Gizmos.color = Color.green;
                    }

                    // 현재 검사 중인 인접 셀 → 빨간색
                    if (currentNeighbor == position)
                    {
                        Gizmos.color = Color.red;
                    }
                }

                Vector3 pos = new Vector3(
                    -width / 2f + x + 0.5f,
                    0,
                    -height / 2f + y + 0.5f
                );

                Gizmos.DrawCube(pos, Vector3.one);
            }
        }
    }
}
