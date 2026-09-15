using System;
using System.Collections;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public int width;
    public int height;

    public string seed;
    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent;

    [Range(1, 10)]
    public int repeatCount = 5;

    public float delayTime = 1f;

    // 셀로 사용할 Prefab
    public GameObject _cellPrefab;

    int[,] map;

    // 생성된 셀 GameObject를 저장
    GameObject[,] cells;

    Coroutine smoothCoroutine;
    
    public Camera mainCamera;
    public float cameraPadding = 1.1f;

    [Range(0.5f, 3f)]
    public float cameraZoom = 1.3f;
    
    private void Start()
    {
        RegenerateMap();
    }

    // 배열 초기화
    void GenerateMap()
    {
        map = new int[width, height];
    }

    [ContextMenu("Generate Map")]
    public void RegenerateMap()
    {
        // 기존 코루틴 중지
        if (smoothCoroutine != null)
        {
            StopCoroutine(smoothCoroutine);
        }

        // 기존에 생성된 셀 삭제
        ClearMap();

        // 맵 데이터 생성
        GenerateMap();

        // 랜덤하게 맵 채우기
        RandomFillMap();
        
        SetupCamera();
        
        // Prefab 생성
        CreateTiles();

        // 처음 생성된 맵 색상 적용
        UpdateTiles();

        // 셀룰러 오토마타 시작
        smoothCoroutine = StartCoroutine(SmoothMapCoroutine());
    }

    // Seed에 따라 랜덤하게 맵 생성
    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = DateTime.Now.Ticks.ToString();
        }

        System.Random pseudoRandom =
            new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 테두리는 벽
                if (x == 0 ||
                    x == width - 1 ||
                    y == 0 ||
                    y == height - 1)
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

    // Prefab을 생성하는 함수
    void CreateTiles()
    {
        cells = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(
                    -width / 2f + x + 0.5f,
                    0,
                    -height / 2f + y + 0.5f
                );

                GameObject cell = Instantiate(
                    _cellPrefab,
                    position,
                    Quaternion.identity,
                    transform
                );

                cells[x, y] = cell;
            }
        }
    }

    // map 배열을 기준으로 Prefab 색상 변경
    void UpdateTiles()
    {
        if (cells == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Renderer renderer =
                    cells[x, y].GetComponent<Renderer>();

                if (renderer == null)
                    continue;

                if (map[x, y] == 1)
                {
                    // 벽
                    renderer.material.color = Color.black;
                }
                else
                {
                    // 빈 공간
                    renderer.material.color = Color.white;
                }
            }
        }
    }

    // 셀룰러 오토마타 반복
    IEnumerator SmoothMapCoroutine()
    {
        // 처음 랜덤 상태를 잠시 확인
        yield return new WaitForSeconds(delayTime);

        for (int i = 0; i < repeatCount; i++)
        {
            SmoothMap();

            // 변경된 map에 맞게 Prefab 색상 갱신
            UpdateTiles();

            Debug.Log($"SmoothMap {i + 1}회 적용");

            yield return new WaitForSeconds(delayTime);
        }

        smoothCoroutine = null;
    }

    // 셀룰러 오토마타 규칙 적용
    void SmoothMap()
    {
        int[,] newMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallTiles =
                    GetSurroundingWallCount(x, y);

                // 테두리를 항상 벽으로 유지
                if (x == 0 ||
                    x == width - 1 ||
                    y == 0 ||
                    y == height - 1)
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

    // 주변 8칸의 벽 개수 계산
    int GetSurroundingWallCount(
        int gridX,
        int gridY
    )
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
                // 맵 내부
                if (neighborX >= 0 &&
                    neighborX < width &&
                    neighborY >= 0 &&
                    neighborY < height)
                {
                    // 자기 자신 제외
                    if (neighborX != gridX ||
                        neighborY != gridY)
                    {
                        wallCount +=
                            map[neighborX, neighborY];
                    }
                }
                else
                {
                    // 맵 바깥은 벽으로 취급
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    // 기존에 생성된 Prefab 삭제
    void ClearMap()
    {
        if (cells == null)
            return;

        for (int x = 0; x < cells.GetLength(0); x++)
        {
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                if (cells[x, y] != null)
                {
                    Destroy(cells[x, y]);
                }
            }
        }

        cells = null;
    }
    
    void SetupCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
            return;

        float mapSize = Mathf.Max(width, height);

        // 맵 중앙 위쪽에 카메라 배치
        mainCamera.transform.position = new Vector3(
            0f,
            mapSize * cameraPadding,
            0f
        );

        // 아래쪽을 바라보도록 회전
        mainCamera.transform.rotation =
            Quaternion.Euler(90f, 0f, 0f);
        
        mainCamera.orthographic = true;

        float aspect = mainCamera.aspect;

        float verticalSize = height / 2f;
        float horizontalSize = width / (2f * aspect);

        float baseSize =
            Mathf.Max(verticalSize, horizontalSize)
            * cameraPadding;

        // 1.3이면 기본 화면보다 약 1.3배 확대
        mainCamera.orthographicSize =
            baseSize / cameraZoom;
    }
}