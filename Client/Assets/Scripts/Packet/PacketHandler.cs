using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MonsterController;

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
            return;

        CreatureController cc = go.GetComponentInChildren<CreatureController>();
        if (cc == null)
            return;

        cc.PosInfo = movePacket.PosInfo;
        cc.RotInfo = movePacket.RotInfo;

        if (cc.ObjectType == Define.Object.OtherPlayer)
        {
            cc.SyncPos();
        }
        else if (cc.ObjectType == Define.Object.Monster)
        {
            MonsterController mc = go.GetComponentInChildren<MonsterController>();
            if (mc == null)
                return;
            //mc.OnRecvMovePacket(movePacket);
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
                cc.UseSkill((KeyCode)skillPacket.SkillInfo.KeyCode);
            }
            else if (cc.ObjectType == Define.Object.Monster)
            {
                MonsterController mc = go.GetComponentInChildren<MonsterController>();
                //mc.OnRecvSkillPacket(skillPacket);
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
            if (pc.ObjectType == Define.Object.OtherPlayer)
                pc.PlayAnimation(animPacket.AnimInfo);
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
    }

    public static void S_CharacterHandler(PacketSession session, IMessage packet)
    {
        S_Character charPacket = packet as S_Character;

        GameObject go = GameObject.Find("Test");
        if (go == null) return;

        UI_SelectEvent selectEvent = go.GetComponent<UI_SelectEvent>();
        if (selectEvent == null) return;

        selectEvent.ChangePickImage(charPacket.CharType, charPacket.PickIdx);
    }

    public static void S_EnterPickHandler(PacketSession session, IMessage packet)
    {
        S_EnterPick enterPickPacket = packet as S_EnterPick;

        GameObject go = GameObject.Find("Test");
        if(go == null) return;

        UI_SelectEvent selectEvent = go.GetComponent<UI_SelectEvent>();
        if (selectEvent == null) return;

        selectEvent.SetPickIdx(enterPickPacket.PickIdx);
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
}
