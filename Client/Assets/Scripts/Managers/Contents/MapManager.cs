using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEngine;

public class MapManager
{
	//public NavMeshSurface navMeshSurface;

	public bool CanGo(Vector3Int cellPos)
	{
		return true;
	}

	public void LoadMap(string mapName)
	{
		DestroyMap();

		string mapFullName = "Map_" + mapName;
		GameObject go = Managers.Resource.Instantiate($"Map/{mapFullName}");
		go.name = "Map";

		//navMeshSurface.BuildNavMesh();
	}

	public void DestroyMap()
	{
		GameObject map = GameObject.Find("Map");
		if (map != null)
		{
			GameObject.Destroy(map);
		}
	}
}
