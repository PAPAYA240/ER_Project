using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class MapManager
{
	NavMeshSurface navMeshSurface = null;

	public Vector3Int CalcResultPos(Vector3Int destPos) // 이동스킬 위치 결과
	{
		if (CanGo(destPos))
			return destPos;



		return Vector3Int.zero;
	}

	bool CanGo(Vector3Int destPos) // 스킬 or 점멸로 이동하는 경우 사용
	{
        NavMeshHit hit;
        bool isOnNavMesh = NavMesh.SamplePosition(destPos, out hit, 1.0f, NavMesh.AllAreas);
        if (isOnNavMesh) 
			return true;
		else
			return false;
	}

	public void LoadMap(string mapName)
	{
		DestroyMap();

		string mapFullName = "Map_" + mapName;
		GameObject go = Managers.Resource.Instantiate($"Map/{mapFullName}");
		go.name = "Map";

		navMeshSurface = go.GetComponent<NavMeshSurface>();
	}

	public void DestroyMap()
	{
		GameObject map = GameObject.Find("Map");
		if (map != null)
		{
			GameObject.Destroy(map);
			navMeshSurface = null;
        }
	}
}
