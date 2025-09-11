using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;

public class ObjectManager
{
	public MyPlayerController MyPlayer { get; set; }
	Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();

    public Define.Character Character { get; set; } = Define.Character.Rozzi;

    public static GameObjectType GetObjectTypeById(int id)
	{
		int type = (id >> 24) & 0x7F;
		return (GameObjectType)type;
	}

	public void Add(ObjectInfo info, bool myPlayer = false)
	{
		GameObjectType objectType = GetObjectTypeById(info.ObjectId);

        if (objectType == GameObjectType.Player)
        {
            if (myPlayer)
            {
                GameObject go = Managers.Resource.Instantiate($"Creature/My{info.CharType}");
                go.name = info.Name;
                _objects.Add(info.ObjectId, go);

                MyPlayer = go.GetComponent<MyPlayerController>();
                MyPlayer.Id = info.ObjectId;
                MyPlayer.PosInfo = info.PosInfo;
                MyPlayer.Stat = info.StatInfo;
                MyPlayer.SyncPos();
                MyPlayer.ObjInfo = info;
                MyPlayer.ManualInit();
            }
            else
            {
                GameObject go = Managers.Resource.Instantiate($"Creature/{info.CharType}");
                go.name = info.Name;
                _objects.Add(info.ObjectId, go);

                PlayerController pc = go.GetComponent<PlayerController>();
                pc.Id = info.ObjectId;
                pc.PosInfo = info.PosInfo;
                pc.Stat = info.StatInfo;
                pc.SyncPos();
                pc.ObjInfo = info;
                Managers.Object.MyPlayer.GetComponentInChildren<UI_Minimap>().ActivatePlayerIcon(UI_MinimapCharIcon.IconType.TeamPlayer, pc);
            }
        }
        else if (objectType == GameObjectType.Monster)
        {
            GameObject go = Managers.Resource.Instantiate($"Creature/Monster/{info.MonsterType}");
            go.name = info.Name;
            _objects.Add(info.ObjectId, go);

            MonsterController mc = go.GetComponentInChildren<MonsterController>();
            mc.ObjInfo = info;
            mc.Id = info.ObjectId;
            mc.PosInfo = info.PosInfo;
            mc.Stat = info.StatInfo;
            mc._monsterType = info.MonsterType;
            mc.SyncPos();
        }

        else if (objectType == GameObjectType.Projectile)
        {
            //GameObject go = Managers.Resource.Instantiate("Creature/Arrow");
            //go.name = "Arrow";
            //_objects.Add(info.ObjectId, go);

            //ArrowController ac = go.GetComponent<ArrowController>();
            //ac.PosInfo = info.PosInfo;
            //ac.Stat = info.StatInfo;
            //ac.SyncPos();
        }
    }

	public void Remove(int id)
	{
		GameObject go = FindById(id);
		if (go == null)
			return;

		_objects.Remove(id);
		Managers.Resource.Destroy(go);
	}

	public GameObject FindById(int id)
	{
		GameObject go = null;
		_objects.TryGetValue(id, out go);
		return go;
	}

	public GameObject Find(Func<GameObject, bool> condition)
	{
		foreach (GameObject obj in _objects.Values)
		{
			if (condition.Invoke(obj))
				return obj;
		}

		return null;
	}

	public void Clear()
	{
        foreach (GameObject obj in _objects.Values)
			Managers.Resource.Destroy(obj);
        _objects.Clear();
		MyPlayer = null;
	}
}
