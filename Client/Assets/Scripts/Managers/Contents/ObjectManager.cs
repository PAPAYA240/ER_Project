using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.PackageManager.UI;
#endif

public class ObjectManager
{
	public MyPlayerController MyPlayer { get; set; }
	private Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();

    public Define.Character Character { get; set; } = Define.Character.Rozzi;

    #region Type ID
    public static GameObjectType GetObjectTypeById(int id)
	{
		int type = (id >> 24) & 0x7F;
		return (GameObjectType)type;
	}

    public static GameObjectType GetObjectTypeById(GameObject go)
    {
        BaseController bs = go.GetComponent<BaseController>();
        return GetObjectTypeById(bs.Id);
    }
    #endregion

    #region Add
    public void Add(ObjectInfo info, bool myPlayer = false)
	{
		GameObjectType objectType = GetObjectTypeById(info.ObjectId);
        switch (objectType)
        {
            case GameObjectType.Player:
                AddPlayer(info, myPlayer);
                break;
            case GameObjectType.Monster:
                AddMonster(info);
                break;
            case GameObjectType.Projectile:
                AddProjectile(info);
                break;
            case GameObjectType.Environment:
                AddEnvironment(info);
                break;
        }
    }

    private void AddPlayer(ObjectInfo info, bool myPlayer)
    {
        if (myPlayer)
        {
            GameObject go = Managers.Resource.Instantiate($"Creature/My{info.Player.CharType}");
            go.name = info.Name;
            _objects.Add(info.ObjectId, go);

            MyPlayer = go.GetComponent<MyPlayerController>();
            MyPlayer.ObjInfo = info;
            MyPlayer.Id = info.ObjectId;
            MyPlayer.SyncPos();
            MyPlayer.Hp = info.StatInfo.MaxHp;
            MyPlayer.Stamina = info.StatInfo.MaxStamina;
            MyPlayer.ManualInit();
        }
        else
        {
            GameObject go = Managers.Resource.Instantiate($"Creature/{info.Player.CharType}");
            go.name = info.Name;
            _objects.Add(info.ObjectId, go);

            PlayerController pc = go.GetComponent<PlayerController>();
            pc.ObjInfo = info;
            pc.Id = info.ObjectId;
            pc.SyncPos();
            pc.ManualInit();
           
            Managers.Object.MyPlayer.GetComponentInChildren<UI_Minimap>().ActivatePlayerIcon(UI_MinimapCharIcon.IconType.TeamPlayer, pc);
        }
    }
    private void AddMonster(ObjectInfo info)
    {
        GameObject go = Managers.Resource.Instantiate($"Creature/Monster/{info.Monster.MonsterType}");
        go.name = info.Name;
        _objects.Add(info.ObjectId, go);

        MonsterController mc = go.GetComponentInChildren<MonsterController>();
        mc.ObjInfo = info;
        mc.Id = info.ObjectId;
        mc.PosInfo = info.PosInfo;
        mc.Stat = info.StatInfo;
        mc.Hp = info.StatInfo.MaxHp;
        mc._monsterType = info.Monster.MonsterType;
    }
    private void AddProjectile(ObjectInfo info)
    {
        //GameObject go = Managers.Resource.Instantiate("Creature/Arrow");
        //go.name = "Arrow";
        //_objects.Add(info.ObjectId, go);

        //ArrowController ac = go.GetComponent<ArrowController>();
        //ac.PosInfo = info.PosInfo;
        //ac.Stat = info.StatInfo;
        //ac.SyncPos();
    }
    private void AddEnvironment(ObjectInfo info)
    {
        GameObject go = Managers.Resource.Instantiate($"Env/{info.Env.EnvType}");
        if (go == null) return;

        go.name = info.Name;
        _objects.Add(info.ObjectId, go);

        EnvController ec = go.GetComponent<EnvController>();
        ec.ObjInfo = info;
        ec.Id = info.ObjectId;
        ec.PosInfo = info.PosInfo;
        ec.Stat = info.StatInfo;
        if (Enum.TryParse(info.Name, out EnvType envEnum))
            ec._envType = envEnum;
        ec.SyncPos();
    }
    #endregion

    #region Utils
    public void SetObjectVisible()
    {
        return;
        if (MyPlayer == null)
            return;

        HashSet<int> hash = MyPlayer.VisibleObjectIds;

        foreach (var keyValue in _objects)
        {
            int key = keyValue.Key;
            if (MyPlayer.ObjInfo.ObjectId == key)
                continue;

            GameObject go = keyValue.Value;

            bool isVisible = false;

            //Vector3 playerPos = MyPlayer.transform.position;
            //Vector3 targetPos = go.transform.position;

            //NavMeshHit hit;

            //if (NavMesh.SamplePosition(playerPos, out hit, 1, NavMesh.AllAreas))
            //    playerPos = hit.position;

            //if (NavMesh.SamplePosition(targetPos, out hit, 1, NavMesh.AllAreas))
            //    targetPos = hit.position;

            //playerPos.y = 0.5f;
            //targetPos.y = 0.5f;

            //Vector3 dir = targetPos - playerPos;

            if (hash.Contains(key) /*&& !NavMesh.Raycast(playerPos, targetPos, out hit, NavMesh.AllAreas)*/)
                isVisible = true; /*장애물없고 시야 범위 내에 있으면*/

            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.enabled = isVisible;
            }

            foreach (var r in go.GetComponentsInChildren<Canvas>())
            {
                r.enabled = isVisible;
            }
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
    #endregion
}
