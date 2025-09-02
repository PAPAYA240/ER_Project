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

	public Vector3 CalcResultPos(Vector3 srcPos, Vector3 destPos) // 이동스킬 위치 결과
	{
		NavMeshHit nmHit;
		if (CanGo(destPos, out nmHit))
			return nmHit.position;

        if (NavMesh.Raycast(srcPos, destPos, out NavMeshHit hit, NavMesh.AllAreas))
        {
            // hit.position = 벽 앞에서 막힌 지점
            return hit.position;
        }
		else
			return destPos;
	}

	bool CanGo(Vector3 destPos, out NavMeshHit nmHit) // 스킬 or 점멸로 이동하는 경우 사용
	{
        bool isOnNavMesh = NavMesh.SamplePosition(destPos, out nmHit, 0.5f, NavMesh.AllAreas);
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
