using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using JetBrains.Annotations;

public class ObjectManager
{
    public MyPlayerController MyPlayer { get; set; } = null;
	private Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
    public Define.Character Character { get; set; } = Define.Character.Rozzi;

    Queue<Action> _pendingActions = new Queue<Action>();
    bool _myPlayerReady = false;

    private readonly Dictionary<ProjectileType, Queue<Projectile>> _projectilePool = new Dictionary<ProjectileType, Queue<Projectile>>();

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
        if (Managers.Object.FindById(info.ObjectId) != null)
            return;

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
            MyPlayer.ManualInit();
            MyPlayer.PosInfo = info.PosInfo;
            MyPlayer.SyncPos(true);
            MyPlayer.Hp = info.StatInfo.MaxHp;
            MyPlayer.Stamina = info.StatInfo.MaxStamina;
            MyPlayer.NickName = info.Player.Nickname;
            MyPlayer.UI.PlayerHUD.AddPlayerBoardToBattleBoard(MyPlayer);
            if (Managers.Scene.CurrentScene is GameScene scene)
            {
                scene.AddPlayer(go, MyPlayer);
            }

            while (_pendingActions.Count > 0)
                _pendingActions.Dequeue().Invoke();
        }
        else
        {
            if (!_myPlayerReady)
            {
                _pendingActions.Enqueue(() => AddPlayer(info, myPlayer));
                return;
            }

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
            pc.NickName = info.Player.Nickname;

            UI_Minimap ui_minimap = MyPlayer.GetComponentInChildren<UI_Minimap>();

            if (Managers.Info.Team != pc.ObjInfo.Player.Team)
            {
                if (ui_minimap != null)
                {
                    ui_minimap.ActivatePlayerIcon(UI_MinimapCharIcon.IconType.EnemyPlayer, pc);
                }
            }
            else
                ui_minimap.ActivatePlayerIcon(UI_MinimapCharIcon.IconType.TeamPlayer, pc);

            MyPlayer.SetxRayFromPlayer(go);
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

        Transform rotationTarget = mc.transform.parent != null ? mc.transform.parent : mc.transform;
        mc.PosInfo = info.PosInfo;
        rotationTarget.position = mc.PosInfo.ToVector();

        mc.RotInfo = info.RotInfo;
        rotationTarget.rotation = mc.RotInfo;

        mc.Stat = info.StatInfo;
        mc.Hp = info.StatInfo.MaxHp;
        mc.Type = info.Monster.MonsterType;
        mc.MonsterTeam = info.Monster.Team;

        if (mc.Type == MonsterType.Omega && Managers.Object.MyPlayer != null )
        {
            Managers.Object.MyPlayer.UI.PlayerHUD.SetMinimapOmegaExpected(false);
            Managers.Object.MyPlayer.UI.PlayerHUD.SetMinimapOmegaGo(true);
        }
        else if (mc.Type == MonsterType.Gamma && Managers.Object.MyPlayer != null)
        {
            Managers.Object.MyPlayer.UI.PlayerHUD.SetMinimapGammaGo(true);
        }
    }
    private void AddProjectile(ObjectInfo info)
    {
        GameObject go = Managers.Object.FindById(info.ObjectId);
        if (go == null)
        {
            ProjectileType type = info.Projectile.ProjectileType;
           // 1) 풀에서 꺼내기
           Projectile pc = GetOrCreateProjectile(info.Projectile.ProjectileType);
           go = pc.gameObject;
           go.name = "Projectile_" + info.ObjectId;

           // 2) 기본 정보 세팅
           pc.PosInfo = info.PosInfo;
           pc.Stat = info.StatInfo;
           pc.Type = info.Projectile.ProjectileType;
           pc.Owner = Managers.Object.FindById(info.Projectile.OwnerId);
           pc.Id = info.ObjectId;

           // 3) 딕셔너리에 등록
           _objects.Add(info.ObjectId, go);

           // 4) 위치 동기화
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
        if (ec == null)
            return;

        ec.ObjInfo = info;
        ec.Id = info.ObjectId;
        ec.PosInfo = info.PosInfo;
        ec.Stat = info.StatInfo;
        ec.ScaleInfo = info.ScaleInfo;
        if (System.Enum.TryParse(info.Name, out EnvType envEnum))
            ec.Type = envEnum;
        ec.SyncPos();
    }

    public void AddWard(ObjectInfo info, int teamIndex)
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
        wc.TeamIndex = teamIndex;
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

    public void MyPlayerReady()
    {
        _myPlayerReady = true;
    }

    public void ResiterVisibleObjects(GameObject go, HashSet<GameObject> outObjects, float visionRange = 8.5f)
    {
        BaseController bc = go.GetComponentInChildren<BaseController>();
        if (bc == null) return;

        Vector3 playerPos = bc.PosInfo.ToVector();
        playerPos.y = 0.5f;

        LayerMask blockingLayers = LayerMask.GetMask("VisionWall");

        foreach (var keyValue in _objects)
        {
            if (bc.Id == keyValue.Key)
                continue;

            GameObject target = keyValue.Value;
            if (target == null)
                continue;

            Vector3 targetPos = target.transform.position;
            targetPos.y = 0.5f;
            float distance = Vector3.Distance(playerPos, targetPos);
            if (distance > visionRange) continue;

            Vector3 direction = (targetPos - playerPos).normalized;

            if (!Physics.Raycast(playerPos, direction, distance, blockingLayers))
            {
                outObjects.Add(target);
            }
        }
    }

    public void SetVisibleObjects(HashSet<GameObject> objects)
    {
        if (MyPlayer == null || MyPlayer.View == null)
            return;

        HashSet<int> hash = MyPlayer.View.VisibleObjectIds;
        if (hash == null)
            return;

        HashSet<int> wardHash = MyPlayer.View.WardIds;
        if (wardHash == null)
            return;

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

            CreatureController controller = go.GetComponent<CreatureController>();
            if (controller != null)
            {
                if (controller.IsHide)
                    continue;
            }

            bool isVisible = false;

            if (hash.Contains(key) || wardHash.Contains(key) || objects.Contains(FindById(key)))
                isVisible = true; /* 서버에서 넘어온 해시셋에 있거나 클라에서 등록한 해시셋에 있으면 */

            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.gameObject.name == "VisionCircle")
                    continue;
                r.enabled = isVisible;
            }

            foreach (var r in go.GetComponentsInChildren<Canvas>())
            {
                if(controller != null && controller.State == CreatureState.Dead)
                    r.enabled = false;
                else
                    r.enabled = isVisible;
            }

            if(go.GetComponent<PlayerController>() != null && Managers.Object.MyPlayer != null)
            {
                Managers.Object.MyPlayer.UI.PlayerHUD.SetMinimapCharImgEnable(key, isVisible);
            }

            if(go.name == "Ward")
            {
                WardController wc = go.GetComponentInChildren<WardController>();
                if (wc != null)
                    wc.SetWardLifeBarActive(isVisible);
                wc.SetVisible(isVisible);
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

        Projectile proj = go.GetComponent<Projectile_Rozzi_NormalAttack>();
        if (proj != null) 
            ReturnProjectileToPool(proj);
        else        
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

    #region Projectile Pool
    private Projectile GetOrCreateProjectile(ProjectileType type)
    {
        Queue<Projectile> queue;
        if (!_projectilePool.TryGetValue(type, out queue))
        {
            queue = new Queue<Projectile>();
            _projectilePool[type] = queue;
        }

        Projectile proj = null;

        if (queue.Count > 0)
        {
            proj = queue.Dequeue();
        }
        else
        {
            // 새로 생성
            GameObject go = Managers.Resource.Instantiate($"Creature/Weapon/{type}");
            if(go== null)
                return null;
            proj = go.GetComponent<Projectile>();
            if (proj == null)
                return null;

            Transform parent = GetOrCreateParent("@ Projectile Pool");
            proj.transform.SetParent(parent);
        }

        proj.gameObject.SetActive(true);

        if (proj is Projectile_Rozzi_NormalAttack normal)
            normal.ResetForPool();

        return proj;
    }

    private void ReturnProjectileToPool(Projectile proj)
    {
        proj.gameObject.SetActive(false);

        Queue<Projectile> queue;
        if (!_projectilePool.TryGetValue(proj.Type, out queue))
        {
            queue = new Queue<Projectile>();
            _projectilePool[proj.Type] = queue;
        }

        queue.Enqueue(proj);
    }
    #endregion
}
