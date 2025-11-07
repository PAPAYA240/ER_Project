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
        S_Move mPacket = packet as S_Move;
        ServerSession serverSession = session as ServerSession;

        GameObject go = Managers.Object.FindById(mPacket.ObjectId);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == mPacket.ObjectId)
        {
            Managers.Object.MyPlayer.OnServerUpdate(mPacket);
        }
        else
        {
            BaseController bc = go.GetComponentInChildren<BaseController>();
            if (bc == null)
                return;
            GameObjectType objectType = ObjectManager.GetObjectTypeById(bc.Id);
            if (objectType == GameObjectType.Player)
            {
                PlayerController pc = go.GetComponentInChildren<PlayerController>();
                if (pc == null)
                    return;

                if (pc.State == CreatureState.Moving)
                {
                    pc.SyncPosFromServer(mPacket);
                }
            }

            bc.transform.position = mPacket.PosInfo.ToVector();
            bc.transform.rotation = mPacket.RotInfo;
            bc.PosInfo = mPacket.PosInfo;
            bc.RotInfo = mPacket.RotInfo;
        }     
    }

    public static void S_TargetChangeHandler(PacketSession session, IMessage packet)
    {
        S_TargetChange targetChangePacket = packet as S_TargetChange;
        ServerSession serverSession = session as ServerSession;

        Managers.Object.MyPlayer.View.RotateAttack(targetChangePacket.TargetId);
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

            //foreach(var v in changePacket.Damages)
            //{
            //    Managers.CombatText.SetCombatText(CombatTextManager.TextType.AdDamage, v.Damage, cc.transform.position);
            //}
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

        if (Managers.Object.MyPlayer != null)
        {
            if (Managers.Object.MyPlayer.Id == diePacket.ObjectId)
            {
                go.GetComponentInChildren<MyPlayerController>().UI.PlayerInterface.OnDead(diePacket.RespawnTime);
            }

            // 죽은 플레이어
            PlayerController pc = cc as PlayerController;
            if (pc == null)
                return;

            // 공격 플레이어
            GameObject attackerGo = Managers.Object.FindById(diePacket.AttackerId);
            if (attackerGo == null)
                return;

            PlayerController attPc = attackerGo.GetComponentInChildren<PlayerController>();
            if (attPc == null)
                return;

            Managers.Object.MyPlayer.UI.NotifyKill(attPc, pc); 
        }
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
        S_Interact interactPacket = packet as S_Interact;

        GameObject go = Managers.Object.FindById(interactPacket.ObjectId);
        if (go == null) return;

        CreatureController creature = go.GetComponentInChildren<CreatureController>();
        if (creature == null) return;

        GameObjectType objectType = ObjectManager.GetObjectTypeById(creature.Id);

        // 오브젝트 충돌
        if ((KeyCode)interactPacket.TargetKeyCode == KeyCode.F1)
        {
            if (objectType == GameObjectType.Player)
            {
                KeyCode mkey = (KeyCode)interactPacket.KeyCode; // Hitbox 키코드
                GameObject target = Managers.Object.FindById(interactPacket.TargetId); // 공격한 타겟

                MonsterController mc = target.GetComponentInChildren<MonsterController>();
                creature.OnObjectCollision(target, mkey);
            }
        }
        // Hitbox 충돌
        else
        {
            if (objectType == GameObjectType.Player)
            {
                KeyCode mkey = (KeyCode)interactPacket.KeyCode;
                KeyCode tKey = (KeyCode)interactPacket.TargetKeyCode;
                creature.OnHitboxCollision(mkey, tKey);
            }
        }
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

        mpc.View.VisibleObjectIds.Clear(); // 나중에 렌더링 하고나서 바로 Clear하는게 나을듯?
        mpc.View.VisibleObjectIds = visibleObjectsPkt.VisibleObjectIds.ToHashSet();
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

        cc.ChangeStat(levelUpPkt.StatGrowth);

        //아래는 레벨이 제대로 표시되게 하는 코드
        //마이 플레이어면 업데이트 하고 리턴
        MyPlayerController mpc = go.GetComponent<MyPlayerController>();
        if (null != mpc)
        {
            mpc.UI.PlayerInterface.OnLevelUp(levelUpPkt.LevelUpCnt);
            mpc.UpdateLevel();
            mpc.UI.PlayerInterface.UpdateStat();
            return;
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
       
    }

    public static void S_RespawnHandler(PacketSession session, IMessage packet)
    {
        S_Respawn respawnPacket = packet as S_Respawn;

        GameObject go = Managers.Object.FindById(respawnPacket.ObjectId);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == respawnPacket.ObjectId)
            Managers.Object.MyPlayer.OnServerUpdate(respawnPacket);
        else
        {
            PlayerController pc = go.GetComponentInChildren<PlayerController>();
            if (pc != null)
                pc.OnRespawn(respawnPacket);
        }
    }

    public static void S_SkillLevelUpHandler(PacketSession session, IMessage packet)
    {
        S_SkillLevelUp skillLevelUpPacket = packet as S_SkillLevelUp;

        KeyCode key = (KeyCode)skillLevelUpPacket.KeyCode;

        Managers.Object.MyPlayer.UI.PlayerInterface.SpecificSkillLevelUp(key);
        Managers.Object.MyPlayer.UI.UpdateSkillMaxCool();
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
        S_SkillConfirm confirmPacket = packet as S_SkillConfirm;

        GameObject go = Managers.Object.FindById(confirmPacket.ObjectId);
        if (go == null)
            return;

        if (Managers.Object.MyPlayer.Id == confirmPacket.ObjectId)
        {
            if(true == confirmPacket.CanUse)
                Managers.Object.MyPlayer.OnServerUpdate(confirmPacket);
        }

        // 스킬 시 이펙트 자신의 스킬 이펙트만 호출
        PlayerController player = go.GetComponent<PlayerController>();
        if (player != null)
        { 
            player.PlaySkillEffect((KeyCode)confirmPacket.SkillKey); 
        }
    }

    public static void S_SkillCollisionRequestHandler(PacketSession session, IMessage packet)
    {
        S_SkillCollisionRequest requestPacket = packet as S_SkillCollisionRequest;

        Managers.Object.MyPlayer.OnServerUpdate(requestPacket);
    }

    public static void S_SkillCostHandler(PacketSession session, IMessage packet)
    {
        S_SkillCost costPacket = packet as S_SkillCost;

        GameObject go = Managers.Object.FindById(costPacket.ObjectId);
        if (go == null)
            return;

        Managers.Object.MyPlayer.OnServerUpdate(costPacket);
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

    public static void S_ChangeItemStatHandler(PacketSession session, IMessage packet)
    {
        S_ChangeItemStat changeItemStatPacket = packet as S_ChangeItemStat;

        GameObject go = Managers.Object.FindById(changeItemStatPacket.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponent<PlayerController>();
        if (pc == null)
            return;

        pc.UpdateItemStat(changeItemStatPacket.ItemStat);

        if(pc is MyPlayerController mpc)
        {
            mpc.UI.PlayerInterface.UpdateStat();
            mpc.UI.PlayerInterface.UpdateSkillAccForPopup((int)changeItemStatPacket.ItemStat.SkillAcceleration);
            mpc.UI.UpdateSkillMaxCool();
        }
    }

    public static void S_ChangeEquipItemHandler(PacketSession session, IMessage packet)
    {
        S_ChangeEquipItem changeEquipPacket = packet as S_ChangeEquipItem;

        GameObject go = Managers.Object.FindById(changeEquipPacket.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponent<PlayerController>();
        if (pc == null)
            return;

        pc.EquipItem(changeEquipPacket.ItemId);
    }
    public static void S_ProjectileHandler(PacketSession session, IMessage packet)
    {
    }
    
    public static void S_EnvRequestHandler(PacketSession session, IMessage packet)
    {
        S_EnvRequest revPacket = packet as S_EnvRequest;

        GameObject go = Managers.Object.FindById(revPacket.ObjectId);
        if (go == null)
            return;

        EnvController ec = go.GetComponent<EnvController>();
        if (ec == null)
            return;
        ec.OnInteractionAuthorized();
    }
    
    public static void S_ChangeInventoryHandler(PacketSession session, IMessage packet)
    {
        S_ChangeInventory changeInventoryPacket = packet as S_ChangeInventory;

        MyPlayerController mpc = Managers.Object.MyPlayer;
        if (mpc == null) 
            return;

        mpc.ChangeInventory(changeInventoryPacket);
    }

    public static void S_StunHandler(PacketSession session, IMessage packet)
    {
        //S_Stun stunPacket = packet as S_Stun;
        //GameObject go = Managers.Object.FindById(stunPacket.ObjectId);
        //if (go == null)
        //    return;

        //CreatureController cc = go.GetComponentInChildren<CreatureController>();
        //if (cc != null)
        //{
        //    if (stunPacket.IsStun)
        //        cc.ApplyStun(stunPacket.Duration);
        //}
    }

    public static void S_CombatTextHandler(PacketSession session, IMessage packet)
    {
        S_CombatText textPacket = packet as S_CombatText;

        GameObject go = Managers.Object.FindById(textPacket.ObjectId);
        if (go == null)
            return;

        Managers.CombatText.SetCombatText(textPacket.Type, textPacket.Value, go.transform.position);
    }

    public static void S_ChangeKDAHandler(PacketSession session, IMessage packet)
    {
        S_ChangeKDA KDAPacket = packet as S_ChangeKDA;

        foreach(KDAInfo info in KDAPacket.KDAs)
        {
            GameObject go = Managers.Object.FindById(info.ObjectId);
            if (go != null)
            {
                PlayerController pc = go.GetComponentInChildren<PlayerController>();
                if (pc != null)
                {
                    pc.SetKDA(info.Kill, info.Death, info.Asist);
                    //Debug.Log($"{pc.Id} {pc.name} K: {pc.KillAmount} D: {pc.DeathAmount} A: {pc.AsistAmount}");
                }
            }               
        }
    }

    public static void S_SyncTimerHandler(PacketSession session, IMessage packet)
    {
        S_SyncTimer syncTimerPacket = packet as S_SyncTimer;


        float clientPacketReceiveTime = Time.realtimeSinceStartup; // 패킷을 받은 로컬 시간 (Unity)

        float oneWayLatencySeconds = GetCurrentEstimatedOneWayLatency(); 

        long compensatedServerCurrentTimeMs = syncTimerPacket.CurrentTimestamp + (long)(oneWayLatencySeconds * 1000);
        long compensatedPhaseServerEndTimeMs = syncTimerPacket.PhaseEndTime + (long)(oneWayLatencySeconds * 1000);

        // 서버가 생각하는 남은 시간 (밀리초)
        long estimatedServerRemainingDurationMs = compensatedPhaseServerEndTimeMs - compensatedServerCurrentTimeMs;

        // 클라이언트의 Time.realtimeSinceStartup을 기준으로 타이머가 끝날 최종 목표 시간
        float clientLocalTargetRealtimeSinceStartupEnd = clientPacketReceiveTime + (estimatedServerRemainingDurationMs / 1000f);


        if(Managers.Object.MyPlayer != null)
        {
            Managers.Object.MyPlayer.UI.SetTimer(syncTimerPacket.Phase, clientLocalTargetRealtimeSinceStartupEnd);
        }
    }

    public static void S_AddAbigailCoordHandler(PacketSession session, IMessage packet)
    {
        S_AddAbigailCoord addAbigailCoordPkt = packet as S_AddAbigailCoord;

        GameObject go = Managers.Object.FindById(addAbigailCoordPkt.ObjectId);
        if (go == null)
            return;

        AbigailCoord abigailCoord = go.GetComponentInChildren<AbigailCoord>();
        if (abigailCoord == null)
            return;

        abigailCoord.ActivateAbigailCoord(addAbigailCoordPkt.Duration, addAbigailCoordPkt.AttackerTeam);
    }

    public static void S_RemoveAbigailCoordHandler(PacketSession session, IMessage packet)
    {
        S_RemoveAbigailCoord addAbigailCoordPkt = packet as S_RemoveAbigailCoord;

        GameObject go = Managers.Object.FindById(addAbigailCoordPkt.ObjectId);
        if (go == null)
            return;

        AbigailCoord abigailCoord = go.GetComponentInChildren<AbigailCoord>();
        if (abigailCoord == null)
            return;

        abigailCoord.DeactivateAbigailCoord();
    }

    public static void S_OccupyBeaconHandler(PacketSession session, IMessage packet)
    {
        S_OccupyBeacon occupyBeaconPkt = packet as S_OccupyBeacon;


    }

    public static void S_ChangeBeaconTimeHandler(PacketSession session, IMessage packet)
    {
        S_ChangeBeaconTime changeBeaconTimePkt = packet as S_ChangeBeaconTime;


    }

    public static void S_ChangeScoreHandler(PacketSession session, IMessage packet)
    {
        S_ChangeScore changeScorePkt = packet as S_ChangeScore;


    }

    public static void S_GameOverHandler(PacketSession session, IMessage packet)
    {
        S_GameOver gameOverPkt = packet as S_GameOver;


    }

    public static void S_ChangeTransformHandler(PacketSession session, IMessage packet)
    {
        S_ChangeTransform changeTransformPkt = packet as S_ChangeTransform;

        GameObject go = Managers.Object.FindById(changeTransformPkt.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponentInChildren<PlayerController>();
        if (pc == null)
            return;

        pc.CellPos = changeTransformPkt.PosInfo.ToVector();
        pc.RotInfo = changeTransformPkt.RotInfo;
        pc.SyncPos(changeTransformPkt.IsWarp);
    }

    public static void S_CanStopSkillHandler(PacketSession session, IMessage packet) 
    {
        S_CanStopSkill canStopSkillPkt = packet as S_CanStopSkill;

        GameObject go = Managers.Object.FindById(canStopSkillPkt.ObjectId);
        if (go == null)
            return;

        MyPlayerController mpc = go.GetComponentInChildren<MyPlayerController>();
        if (mpc == null)
            return;

        mpc.CanStopSkill = canStopSkillPkt.CanStopSkill;
    }

    public static void S_MoveSpeedHandler(PacketSession session, IMessage packet)
    {
        S_MoveSpeed speedPacket = packet as S_MoveSpeed;

        GameObject go = Managers.Object.FindById(speedPacket.ObjectId);
        if (go == null)
            return;

        PlayerController pc = go.GetComponentInChildren<PlayerController>();
        if (pc == null)
            return;

        pc.Speed = speedPacket.MoveSpeed;
    }

    static float GetCurrentEstimatedOneWayLatency()
    {
        return 0.05f;
    }
}
