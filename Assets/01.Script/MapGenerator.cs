using System;
using System.Collections;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public int width;
    public int height;

    public string seed;
    public bool useRandomSeed;
    
    [Range(0, 100)] public int randomFillPercent;
    [Range(1, 10)] public int repeatCount = 5;
    public float delayTime = 1f;
    
    int[,] map;
    Coroutine smoothCoroutine;

    private void Start()
    {
        RegenerateMap();
    }

    // 배열 초기화 함수
    public void GenerateMap()
    {
        map = new int[width, height];
    }
    
    [ContextMenu("Generate Map")]
    public void RegenerateMap()
    {
        // 기존 코루틴이 실행 중이면 중지
        if (smoothCoroutine != null)
        {
            StopCoroutine(smoothCoroutine);
        }

        GenerateMap();
        RandomFillMap();

        smoothCoroutine = StartCoroutine(SmoothMapCoroutine());
    }
    
    // 맵을 나타내는 map 배열을 Seed 값에 따라 Random하게 채우는 함수
    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.deltaTime.ToString();
        }

        System.Random psuedoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (psuedoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;    
                }
            }
        }
    }
    
    // 1초마다 SmoothMap을 한 번씩 실행
    IEnumerator SmoothMapCoroutine()
    { 
        // 처음 랜덤 맵을 1초 동안 확인
        yield return new WaitForSeconds(delayTime); 
        for (int i = 0; i < 5; i++) 
        { 
            SmoothMap();
            yield return new WaitForSeconds(delayTime); 
        } 
        
        smoothCoroutine = null;
    }

    // 규칙을 적용하여 맵을 부드럽게 만드는 부분
    // 배열을 순회하며 3X3에 인접한 8개 셀이 5개 이상이면 벽이면, 해당 셀도 벽으로
    // 4개 이하면 해당 셀은 빈 공간으로
    void SmoothMap()
    {
        int[,] newMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallTiles = GetSurroundingWallCount(x, y);

                if (neighborWallTiles > 4)
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

    // 인접한 셀 중 벽의 개수를 세는 함수 (맵의 테두리에 위치한 셀은 무조건 벽으로)
    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if ((neighborX >= 0 && neighborX < width) && (neighborY >= 0 && neighborY < height))
                {
                    if (neighborX != gridX || neighborY != gridY)
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
    
    // 벽/빈 공간을 Gizmo Cube로 그리는 함수
    void OnDrawGizmos()
    {
        if (map != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.color = (map[x,y] == 1) ? Color.black : Color.white;
                    Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }
        }
    }
}
