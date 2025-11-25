using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using UnityEngine;

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
            case GameObjectType.Ward:
                AddWard(info);
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
            MyPlayer.ManualInit();
            MyPlayer.PosInfo = info.PosInfo;
            MyPlayer.SyncPos(true);
            MyPlayer.Hp = info.StatInfo.MaxHp;
            MyPlayer.Stamina = info.StatInfo.MaxStamina;
            MyPlayer.UI.PlayerHUD.AddPlayerBoardToBattleBoard(MyPlayer);
            if (Managers.Scene.CurrentScene is GameScene scene)
            {
                scene.AddPlayer(go, MyPlayer);
            }
        }
        else
        {
            GameObject go = Managers.Resource.Instantiate($"Creature/{info.Player.CharType}");
            go.name = info.Name;
            _objects.Add(info.ObjectId, go);

            PlayerController pc = go.GetComponent<PlayerController>();

            pc.ObjInfo = info;
            pc.Id = info.ObjectId;
            pc.ManualInit();
            pc.PosInfo = info.PosInfo;
            pc.SyncPos(true);
            pc.SyncPosFromServer(info.PosInfo, info.RotInfo);

            if (MyPlayer.ObjInfo.Player.Team != pc.ObjInfo.Player.Team)
            {
                go.gameObject.AddComponent<HighlightEffect>();
                Managers.Object.MyPlayer.GetComponentInChildren<UI_Minimap>().ActivatePlayerIcon(UI_MinimapCharIcon.IconType.EnemyPlayer, pc);
            }
            else
                Managers.Object.MyPlayer.GetComponentInChildren<UI_Minimap>().ActivatePlayerIcon(UI_MinimapCharIcon.IconType.TeamPlayer, pc);

            Managers.Object.MyPlayer.SetxRayFromPlayer(go);
            MyPlayer.UI.PlayerHUD.AddPlayerBoardToBattleBoard(pc);
            if (Managers.Scene.CurrentScene is GameScene scene)
            {
                scene.AddPlayer(go, pc);
            }
        }
    }
    private void AddMonster(ObjectInfo info)
    {
        GameObject go = Managers.Resource.Instantiate($"Creature/Monster/{info.Monster.MonsterType}");
        go.name = info.Name;
        _objects.Add(info.ObjectId, go);
        go.transform.position = new Vector3(info.PosInfo.PosX, info.PosInfo.PosY, info.PosInfo.PosZ);

        MonsterController mc = go.GetComponentInChildren<MonsterController>();
        mc.ObjInfo = info;
        mc.Id = info.ObjectId;
        mc.PosInfo = info.PosInfo;
        mc.RotInfo = info.RotInfo;
        mc.Stat = info.StatInfo;
        mc.Hp = info.StatInfo.MaxHp;
        mc.Type = info.Monster.MonsterType;
    }
    private void AddProjectile(ObjectInfo info)
    {
        GameObject go = Managers.Object.FindById(info.ObjectId);
        if (go == null)
        {
            go = Managers.Resource.Instantiate($"Creature/Weapon/{info.Projectile.ProjectileType}");
            go.name = "Projectile_" + info.ObjectId;

            Projectile pc = go.GetComponent<Projectile>();
            pc.PosInfo = info.PosInfo;
            pc.Stat = info.StatInfo;
            pc.Type = info.Projectile.ProjectileType;
            pc.Owner = Managers.Object.FindById(info.Projectile.OwnerId);

            Transform parent = GetOrCreateParent("@ Projectiles");
            pc.transform.SetParent(parent);

            _objects.Add(info.ObjectId, go);
            pc.SyncPos();
        }
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

        if (System.Enum.TryParse(info.Name, out EnvType envEnum))
            ec._envType = envEnum;
        ec.SyncPos();
    }

    private void AddWard(ObjectInfo info)
    {
        GameObject go = Managers.Resource.Instantiate("Creature/Ward");
        if (go == null) return;

        go.name = "Ward";
        _objects.Add(info.ObjectId, go);
        go.transform.position = new Vector3(info.PosInfo.PosX, info.PosInfo.PosY, info.PosInfo.PosZ);

        //Debug.Log(" Add Ward!");

        WardController wc = go.GetComponent<WardController>();
        wc.ObjInfo = info;
        wc.Id = info.ObjectId;
        wc.PosInfo = info.PosInfo;
        wc.Stat = info.StatInfo;
        wc.SyncPos();

        //EnvController ec = go.GetComponent<EnvController>();
        //ec.ObjInfo = info;
        //ec.Id = info.ObjectId;
        //ec.PosInfo = info.PosInfo;
        //ec.Stat = info.StatInfo;

        //if (System.Enum.TryParse(info.Name, out EnvType envEnum))
        //    ec._envType = envEnum;
        //ec.SyncPos();
    }
    #endregion

    #region Utils


    public void ResiterVisibleObjects(GameObject go, HashSet<GameObject> outObjects)
    {
        foreach (var keyValue in _objects)
        {
            int key = keyValue.Key;
            PlayerController pc = go.GetComponentInChildren<PlayerController>();

            if (pc == null || pc.Id == key)
                continue;

            GameObject target = keyValue.Value;
            if (target == null)
                continue;

            float visionRange = 8.5f;

            Vector3 playerPos = go.transform.position;
            Vector3 targetPos = target.transform.position;

            UnityEngine.AI.NavMeshHit hit;

            if (UnityEngine.AI.NavMesh.SamplePosition(playerPos, out hit, 1, UnityEngine.AI.NavMesh.AllAreas))
                playerPos = hit.position;

            if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 1, UnityEngine.AI.NavMesh.AllAreas))
                targetPos = hit.position;

            playerPos.y = 0.5f;
            targetPos.y = 0.5f;

            // Vector3 dir = targetPos - playerPos;

            if (Vector3.Distance(playerPos, targetPos) < visionRange && !UnityEngine.AI.NavMesh.Raycast(playerPos, targetPos, out hit, UnityEngine.AI.NavMesh.AllAreas))
            {
                outObjects.Add(target); /*장애물없고 시야 범위 내에 있으면*/
            }
        }
    }

    public void SetVisibleObjects(HashSet<GameObject> objects)
    {
        if (MyPlayer == null || MyPlayer.View == null)
            return;

        HashSet<int> hash = MyPlayer.View.VisibleObjectIds;

        foreach (var keyValue in _objects)
        {
            int key = keyValue.Key;
            if (MyPlayer.ObjInfo.ObjectId == key)
                continue;

            GameObject go = keyValue.Value;
            if (go == null)
                continue;

            if (go.GetComponent<EnvController>() != null)
                continue;

            bool isVisible = false;

            if (hash.Contains(key) || objects.Contains(FindById(key)))
                isVisible = true; /* 서버에서 넘어온 해시셋에 있거나 클라에서 등록한 해시셋에 있으면 */

            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name == "VisionCircle")
                    continue;
                r.enabled = isVisible;
            }

            foreach (var r in go.GetComponentsInChildren<Canvas>())
            {
                r.enabled = isVisible;
            }
        }
    }

    private Transform GetOrCreateParent(string name)
    {
        Transform root = GameObject.Find(name)?.transform;

        if (root == null)
        {
            GameObject newRoot = new GameObject(name);
            root = newRoot.transform;
        }

        return root;
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
