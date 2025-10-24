using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using UnityEngine;
using static UI_PlayerInterface;

class PacketHandler
{
	public static void S_EnterGameHandler(PacketSession session, IMessage packet)
	{
		S_EnterGame enterGamePacket = packet as S_EnterGame;

        Managers.Object.Add(enterGamePacket.Player, myPlayer: true);
	}

    public static void S_LeaveGameHandler(PacketSession session, IMessage packet)
    {
        S_LeaveGame leaveGamePacket = packet as S_LeaveGame;
        Managers.Object.Clear();
    }
    public static void S_SpawnHandler(PacketSession session, IMessage packet)
    {
        S_Spawn spawnPacket = packet as S_Spawn;
        foreach (ObjectInfo obj in spawnPacket.Objects)
        {
            Managers.Object.Add(obj, myPlayer: false);
        }
    }
    public static void S_DespawnHandler(PacketSession session, IMessage packet)
    {
        S_Despawn despawnPacket = packet as S_Despawn;
        foreach (int id in despawnPacket.ObjectIds)
        {
            Managers.Object.Remove(id);
        }
    }
    public static void S_MoveHandler(PacketSession session, IMessage packet)
    {
        S_Move movePacket = packet as S_Move;
        ServerSession serverSession = session as ServerSession;

        GameObject go = Managers.Object.FindById(movePacket.ObjectId);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == movePacket.ObjectId)
        {
            Managers.Object.MyPlayer.OnServerUpdate(movePacket);
        }
        else
        {
            PlayerController pc = go.GetComponentInChildren<PlayerController>();
            if (pc == null)
                return;

            if (pc.State == CreatureState.Moving)
            {
                pc.SyncPosFromServer(movePacket);
            }
            else
            {
                pc.transform.position = movePacket.PosInfo.ToVector();
                pc.transform.rotation = movePacket.RotInfo;
                pc.PosInfo = movePacket.PosInfo;
                pc.RotInfo = movePacket.RotInfo;
            }
        }     
    }

    public static void S_SetMoveTargetHandler(PacketSession session, IMessage packet)
    {
        S_SetMoveTarget targetPacket = packet as S_SetMoveTarget;
        ServerSession serverSession = session as ServerSession;

        if (Managers.Object.MyPlayer.Id == targetPacket.Id)
        {
            Managers.Object.MyPlayer.OnServerUpdate(targetPacket);
        }
    }

    public static void S_StateHandler(PacketSession session, IMessage packet)
    {
        S_State skillPacket = packet as S_State;
        if (skillPacket == null)
            return;

        GameObject go = Managers.Object.FindById(skillPacket.ObjectId);
        if (go == null)
        {
            Debug.Log($"ID {skillPacket.ObjectId}를 가진 몬스터 오브젝트를 찾을 수 없습니다");
            return;
        }

        MonsterController mc = go.GetComponentInChildren<MonsterController>();
        if (mc != null)
            mc.OnRecvStatePacket(skillPacket);  
    }

    public static void S_SkillHandler(PacketSession session, IMessage packet)
    {
        S_Skill skillPacket = packet as S_Skill;

        GameObject go = Managers.Object.FindById(skillPacket.ObjectId);
        if (go == null)
            return;

        CreatureController cc = go.GetComponentInChildren<CreatureController>();
        if (cc != null)
        {
            GameObjectType objectType = ObjectManager.GetObjectTypeById(cc.Id);
            if (objectType == GameObjectType.Player)
            {
                cc.UseSkill(skillPacket);
                Debug.Log("스킬 패킷 받기");
            }
        }
    }

    public static void S_AnimHandler(PacketSession session, IMessage packet)
    {
        S_Anim animPacket = packet as S_Anim;

        GameObject go = Managers.Object.FindById(animPacket.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponent<PlayerController>();
        if (pc != null)
        {
            //if (pc.ObjectType == Define.Object.OtherPlayer)
                pc.PlayAnimFromServer(animPacket.AnimInfo);
        }
    }
    
    
    public static void S_ChangeHpHandler(PacketSession session, IMessage packet)
    {
        S_ChangeHp changePacket = packet as S_ChangeHp;

        GameObject go = Managers.Object.FindById(changePacket.ObjectId);
        if (go == null)
            return;

        CreatureController cc = go.GetComponent<CreatureController>();
        if (cc != null)
        {
            cc.Hp = changePacket.Hp;
            cc.Barrier = changePacket.Barrier;
        }
    }

    public static void S_DieHandler(PacketSession session, IMessage packet)
    {
        S_Die diePacket = packet as S_Die;

        GameObject go = Managers.Object.FindById(diePacket.ObjectId);
        if (go == null)
            return;

        CreatureController cc = go.GetComponent<CreatureController>();
        if (cc != null)
        {
            cc.Hp = 0;
            cc.OnDead();
        }

        //if (Managers.Object.MyPlayer != null && Managers.Object.MyPlayer.Id == diePacket.ObjectId)
        //{
        //    go.GetComponentInChildren<MyPlayerController>().PlayerInterface.OnDead(diePacket.RespawnTime);
        //}
    }

    public static void S_CharacterHandler(PacketSession session, IMessage packet)
    {
        S_Character charPacket = packet as S_Character;

        GameObject go = GameObject.Find("PickScene");
        if (go == null) return;

        PickScene pickScene = go.GetComponent<PickScene>();
        if (pickScene == null) return;

        pickScene.ChangePickImage(charPacket.CharType, charPacket.PickIdx);
    }
    public static void S_TraitHandler(PacketSession session, IMessage packet)
    {
        S_Trait traitPacket = packet as S_Trait;

        GameObject go = GameObject.Find("PickScene");
        if (go == null) return;

        PickScene pickScene = go.GetComponent<PickScene>();
        if (pickScene == null) return;

        pickScene.ChangeTraitImage(traitPacket.TraitType, traitPacket.PickIdx);
    }
    
    public static void S_InteractHandler(PacketSession session, IMessage packet)
    {
        
    }

    public static void S_WeaponHandler(PacketSession session, IMessage packet)
    {
        S_Weapon weaponPacket = packet as S_Weapon;

        GameObject go = GameObject.Find("PickScene");
        if (go == null) return;

        PickScene pickScene = go.GetComponent<PickScene>();
        if (pickScene == null) return;

        pickScene.ChangeWeaponImage(weaponPacket.WeaponType, weaponPacket.PickIdx);
    }

    public static void S_EnterPickHandler(PacketSession session, IMessage packet)
    {
        S_EnterPick enterPickPacket = packet as S_EnterPick;

        GameObject go = GameObject.Find("PickScene");
        if (go == null) return;

        PickScene pickScene = go.GetComponent<PickScene>();
        if (pickScene == null) return;

        pickScene.PickIdx = enterPickPacket.PickIdx;
        pickScene.NickName = enterPickPacket.UserName;
        pickScene.ChangeBar(enterPickPacket.PickIdx);
    }
    public static void S_SpawnPickHandler(PacketSession session, IMessage packet)
    {
        S_SpawnPick spawnPickPacket = packet as S_SpawnPick;

        GameObject go = GameObject.Find("PickScene");
        if (go == null) return;

        PickScene pickScene = go.GetComponent<PickScene>();
        if (pickScene == null) return;

        foreach(PickScenePlayerInfo pspi in spawnPickPacket.Players)
            pickScene.Spawn(pspi);
    }

    public static void S_LeavePickHandler(PacketSession session, IMessage packet)
    {
        S_LeavePick leavePickPacket = packet as S_LeavePick;

        GameObject go = GameObject.Find("Test");
        if (go == null) return;

        UI_SelectEvent selectEvent = go.GetComponent<UI_SelectEvent>();
        if (selectEvent == null) return;

        selectEvent.ChangePickImage(CharacterType.CharacterNone, leavePickPacket.PickIdx);
    }

    public static void S_VisibleObjectsHandler(PacketSession session, IMessage packet)
    {
        S_VisibleObjects visibleObjectsPkt = packet as S_VisibleObjects;

        GameObject go = Managers.Object.FindById(visibleObjectsPkt.ObjectId);
        if (go == null)
            return;

        MyPlayerController mpc = go.GetComponent<MyPlayerController>();
        if(mpc == null) 
            return;

        mpc.VisibleObjectIds.Clear(); // 나중에 렌더링 하고나서 바로 Clear하는게 나을듯?
        mpc.VisibleObjectIds = visibleObjectsPkt.VisibleObjectIds.ToHashSet();
        Managers.Object.SetObjectVisible();

        // TEMP
        PlayerViewController pvc = go.GetComponent<PlayerViewController>();
        if (pvc == null) return;
        pvc.VisibleObjectIds.Clear();
        pvc.VisibleObjectIds = visibleObjectsPkt.VisibleObjectIds.ToHashSet();
    }

    public static void S_LevelUpHandler(PacketSession session, IMessage packet)
    {
        S_LevelUp levelUpPkt = packet as S_LevelUp;

        GameObject go = Managers.Object.FindById(levelUpPkt.ObjectId);
        if (go == null)
            return;

        CreatureController cc = go.GetComponent<CreatureController>();
        if (cc == null)
            return;

        cc.Stat.Level += levelUpPkt.LevelUpCnt;

        //사실 이때 적용되야되는데?
        cc.ChangeStat(levelUpPkt.StatGrowth);

        //아래는 레벨이 제대로 표시되게 하는 코드
        //마이 플레이어면 업데이트 하고 리턴
        MyPlayerController mpc = go.GetComponent<MyPlayerController>();
        if (null != mpc)
        {
            //mpc.PlayerInterface.OnLevelUp(levelUpPkt.LevelUpCnt);
            //mpc.UpdateLevel();
            //return;
        }

        //다른 플레이어면 위에서 안걸리고 내려와서 여기 걸림. 몬스터도 레벨업 하나?
        PlayerController pc = go.GetComponent<PlayerController>();
        if(null !=  pc)
        {
            pc.SetNameTagLevel();
        }
    }

    public static void S_FxHandler(PacketSession session, IMessage packet)
    {
        S_Fx effectPacket = packet as S_Fx;

        GameObject go = Managers.Object.FindById(effectPacket.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponent<PlayerController>();
        if (pc != null)
        {
            if (pc.ObjectType == Define.Object.OtherPlayer)
                pc.PlayEffectFromServer(effectPacket.FxInfo);
        }
    }

    public static void S_RespawnHandler(PacketSession session, IMessage packet)
    {
        S_Respawn respawnPacket = packet as S_Respawn;

        GameObject go = Managers.Object.FindById(respawnPacket.ObjectId);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == respawnPacket.ObjectId)
        {
            Managers.Object.MyPlayer.OnServerUpdate(respawnPacket);
        }
    }

    public static void S_SkillLevelUpHandler(PacketSession session, IMessage packet)
    {
        S_SkillLevelUp skillLevelUpPacket = packet as S_SkillLevelUp;

        KeyCode key = (KeyCode)skillLevelUpPacket.KeyCode;

        Managers.Object.MyPlayer.PlayerInterface.SpecificSkillLevelUp(key);
    }

    public static void S_ChangeStatHandler(PacketSession session, IMessage packet)
    {
        S_ChangeStat statPacket = packet as S_ChangeStat;

        GameObject go = Managers.Object.FindById(statPacket.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponent<PlayerController>();
        if (pc == null)
            return;

        pc.Hp = statPacket.Hp;
        pc.Barrier = statPacket.Barrier;
        pc.Stamina = statPacket.Stamina;
    }

    public static void S_PlayerStateHandler(PacketSession session, IMessage packet)
    {
        S_PlayerState statePacket = packet as S_PlayerState;

        GameObject go = Managers.Object.FindById(statePacket.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponent<PlayerController>();
        if (pc == null)
            return;

        pc.ChangeState(statePacket);
    }

    public static void S_StopHandler(PacketSession session, IMessage packet)
    {
        S_Stop stopPacket = packet as S_Stop;

        GameObject go = Managers.Object.FindById(stopPacket.Id);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == stopPacket.Id)
        {
            Managers.Object.MyPlayer.OnServerUpdate(stopPacket);
        }
        else
        {
            PlayerController pc = go.GetComponentInChildren<PlayerController>();
            if (pc == null)
                return;

            pc.OnStop(stopPacket);
        }
    }

    public static void S_SkillConfirmHandler(PacketSession session, IMessage packet)
    {
        S_SkillConfirm followPacket = packet as S_SkillConfirm;

        GameObject go = Managers.Object.FindById(followPacket.ObjectId);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == followPacket.ObjectId)
        {
            Managers.Object.MyPlayer.OnServerUpdate(followPacket);
        }
    }

    public static void S_SkillMotionHandler(PacketSession session, IMessage packet)
    {
        S_SkillMotion motionPacket = packet as S_SkillMotion;

        GameObject go = Managers.Object.FindById(motionPacket.ObjectId);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == motionPacket.ObjectId)
        {
            Managers.Object.MyPlayer.OnServerUpdate(motionPacket);
        }
    }

    public static void S_MoveSyncHandler(PacketSession session, IMessage packet)
    {
        S_MoveSync syncPacket = packet as S_MoveSync;

        GameObject go = Managers.Object.FindById(syncPacket.ObjectId);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == syncPacket.ObjectId)
        {
            Managers.Object.MyPlayer.OnServerUpdate(syncPacket);
        }
    }
}
