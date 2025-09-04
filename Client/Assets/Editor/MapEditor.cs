using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class MapData
{
    public int width;
    public int depth;
    public bool[] walkable;
    public float[] height;
}

public class MapEditor
{
    [MenuItem("Tools/Export Map Data")]
    public static void Export()
    {
        // 맵의 크기와 그리드 간격 설정
        int width = 200;
        int depth = 200;
        float gridSize = 2.0f;

        MapData mapData = new MapData
        {
            width = width,
            depth = depth,
            walkable = new bool[width * depth],
            height = new float[width * depth]
        };

        // 맵의 모든 그리드를 순회하며 정보 수집
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 cellCenter = new Vector3(x * gridSize, 100, z * gridSize); // 아주 높은 곳에서
                RaycastHit hit;

                // 아래로 레이캐스트를 쏴서 지형 정보를 확인
                if (Physics.Raycast(cellCenter, Vector3.down, out hit, 200.0f))
                {
                    // TODO: 'Ground' 레이어에만 걸리도록 LayerMask 설정 필요
                    mapData.walkable[z * width + x] = true; // 레이캐스트에 걸리면 걸을 수 있는 땅
                    mapData.height[z * width + x] = hit.point.y; // 걸리는 지점의 높이(Y) 저장
                }
                else
                {
                    mapData.walkable[z * width + x] = false; // 아무것도 없으면 걸을 수 없음
                    mapData.height[z * width + x] = 0;
                }
            }
        }

        // 수집된 데이터를 JSON으로 변환하여 파일로 저장
        string json = JsonUtility.ToJson(mapData, true);
        string path = "Assets/Resources/MapData.json"; // 원하는 경로에 저장
        File.WriteAllText(path, json);

        Debug.Log($"Map data exported to {path}");
        AssetDatabase.Refresh();
    }

#if UNITY_EDITOR

    // % (Ctrl), # (Shift), & (Alt)

    //[MenuItem("Tools/GenerateMap %#g")]
    //private static void GenerateMap()
    //{
    //	GenerateByPath("Assets/Resources/Map");
    //       GenerateByPath("../Common/MapData");
    //}

    //private static void GenerateByPath(string pathPrefix)
    //{
    //       GameObject[] gameObjects = Resources.LoadAll<GameObject>("Prefabs/Map");

    //       foreach (GameObject go in gameObjects)
    //       {
    //           Tilemap tmBase = Util.FindChild<Tilemap>(go, "Tilemap_Base", true);
    //           Tilemap tm = Util.FindChild<Tilemap>(go, "Tilemap_Collision", true);

    //           using (var writer = File.CreateText($"{pathPrefix}/{go.name}.txt"))
    //           {
    //               writer.WriteLine(tmBase.cellBounds.xMin);
    //               writer.WriteLine(tmBase.cellBounds.xMax);
    //               writer.WriteLine(tmBase.cellBounds.yMin);
    //               writer.WriteLine(tmBase.cellBounds.yMax);

    //               for (int y = tmBase.cellBounds.yMax; y >= tmBase.cellBounds.yMin; y--)
    //               {
    //                   for (int x = tmBase.cellBounds.xMin; x <= tmBase.cellBounds.xMax; x++)
    //                   {
    //                       TileBase tile = tm.GetTile(new Vector3Int(x, y, 0));
    //                       if (tile != null)
    //                           writer.Write("1");
    //                       else
    //                           writer.Write("0");
    //                   }
    //                   writer.WriteLine();
    //               }
    //           }
    //       }
    //   }

#endif

}
