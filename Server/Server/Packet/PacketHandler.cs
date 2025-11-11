using System;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;
using ServerCore;
using System.Collections.Generic;
using System.Numerics;
using static Server.Data.DataUtils;
using System.Diagnostics;

class PacketHandler
{
    public static void C_EnterGameHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        clientSession.MyPlayer = ObjectManager.Instance.Add<Player>();
        {
            clientSession.MyPlayer.Info.Name = $"Player_{clientSession.MyPlayer.Info.ObjectId}";
            clientSession.MyPlayer.Info.PosInfo.State = CreatureState.Idle;
            clientSession.MyPlayer.Info.PosInfo.PosX = 0;
            clientSession.MyPlayer.Info.PosInfo.PosY = 0;
            clientSession.MyPlayer.Info.Player = new PlayerInfo();
            clientSession.MyPlayer.Info.Player.CharType = clientSession.MyCharacter;

            StatInfo stat = null;
            DataManager.StatDict.TryGetValue(clientSession.MyCharacter, out stat);
            clientSession.MyPlayer.Stat.MergeFrom(stat);
            clientSession.MyPlayer.Hp = clientSession.MyPlayer.MaxHp;
            clientSession.MyPlayer.Stamina = clientSession.MyPlayer.MaxStamina;
            clientSession.MyPlayer.Session = clientSession;
        }

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = RoomManager.Instance.Find(2) as GameRoom;
        if (room == null)
            return;
            
        clientSession.CurRoom = room.RoomId;
        room.Push(room.EnterGame, player);

        C_EnterGame enterGamePkt = packet as C_EnterGame;
    }

    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        // TEMP
    }

    public static void C_MoveSyncHandler(PacketSession session, IMessage packet)
    {
        C_MoveSync movePacket = packet as C_MoveSync;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleMoveSync, player, movePacket);
    }

    public static void C_SkillHandler(PacketSession session, IMessage packet)
	{
  //      C_Skill skillPacket = packet as C_Skill;
  //      ClientSession clientSession = session as ClientSession;

  //      Player player = clientSession.MyPlayer;
  //      if (player == null)
  //          return;

  //      GameRoom room = player.Room;
  //      if (room == null)
  //          return;

		//room.Push(room.HandleSkill, player, skillPacket);
    }

    public static void C_AnimHandler(PacketSession session, IMessage packet)
    {
        C_Anim animPacket = packet as C_Anim;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleAnim, player, animPacket);
    }
    public static void C_FxHandler(PacketSession session, IMessage effectPacket)
    {
        C_Fx skillPacket = effectPacket as C_Fx;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        //room.Push(room.HandleVF, player, skillPacket);
    }
    public static void C_CharacterHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Character c_charPacket = packet as C_Character;
        clientSession.MyCharacter = c_charPacket.CharType;

        PickRoom room = RoomManager.Instance.Find(1) as PickRoom;
        if(room == null) 
            return;

        S_Character s_charPacket = new S_Character();
        s_charPacket.CharType = c_charPacket.CharType;
        s_charPacket.PickIdx = c_charPacket.PickIdx;
        room.Push(room.Broadcast, s_charPacket, c_charPacket.PickIdx);
    }
    public static void C_TraitHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Trait c_traitPacket = packet as C_Trait;
        clientSession.TraitType = c_traitPacket.TraitType;

        PickRoom room = RoomManager.Instance.Find(1) as PickRoom;
        if(room == null) 
            return;

        S_Trait s_traitPacket = new S_Trait();
        s_traitPacket.TraitType = c_traitPacket.TraitType;
        s_traitPacket.PickIdx = c_traitPacket.PickIdx;
        room.Push(room.Broadcast, s_traitPacket, c_traitPacket.PickIdx);
    }
    public static void C_InteractHandler(PacketSession session, IMessage packet)
    {
    }
    public static void C_WeaponHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Weapon c_weaponPacket = packet as C_Weapon;
        clientSession.WeaponType = c_weaponPacket.WeaponType;

        PickRoom room = RoomManager.Instance.Find(1) as PickRoom;
        if(room == null) 
            return;

        S_Weapon s_weaponPacket = new S_Weapon();
        s_weaponPacket.WeaponType = c_weaponPacket.WeaponType;
        s_weaponPacket.PickIdx = c_weaponPacket.PickIdx;
        room.Push(room.Broadcast, s_weaponPacket, c_weaponPacket.PickIdx);
    }

    public static void C_PingHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        clientSession.LastPing = DateTime.Now;
    }

    public static void C_ReadyHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        PickRoom room = RoomManager.Instance.Find(1) as PickRoom;
        if (room == null)
            return;

        if (room.isRoomFull())
            return;

        PickPlayer pp = new PickPlayer();
        pp.Session = clientSession;
        room.Push(room.EnterPick, pp);
    }

    public static void C_SkillLevelUpHandler(PacketSession session, IMessage packet)
    {
        C_SkillLevelUp skillInfoChangePacket = packet as C_SkillLevelUp;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        //스킬 레벨업이 성공하면 
        if (player.SkillLevelUp((KeyCode)skillInfoChangePacket.KeyCode))
        {
            room.Push(room.SkillLevelUp, player.Id, skillInfoChangePacket.KeyCode);
        }
    }

    public static void C_AttackHandler(PacketSession session, IMessage packet)
    {
        var client = (ClientSession)session;
        var player = client?.MyPlayer;
        if (player?.Room == null)
            return;
        var req = (C_Attack)packet;

        player.Room.Push(player.Room.HandleAttack, player, req);
    }

    public static void C_SetMoveTargetHandler(PacketSession session, IMessage packet)
    {
        var client = (ClientSession)session;
        var player = client?.MyPlayer;
        if (player?.Room == null)
            return;
        var req = (C_SetMoveTarget)packet;

        player.Room.Push(player.Room.HandleSetMoveTarget, player, req);
    }

    public static void C_StopHandler(PacketSession session, IMessage packet)
    {
        var client = (ClientSession)session;
        var player = client?.MyPlayer;
        if (player?.Room == null)
            return;
        var req = (C_Stop)packet;

        player.Room.Push(player.Room.HandleStop, player, req);
    }

    public static void C_SkillInputHandler(PacketSession session, IMessage packet)
    {
        C_SkillInput skillPacket = packet as C_SkillInput;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleSkill, player, skillPacket);
    }

    public static void C_SkillPrepareHandler(PacketSession session, IMessage packet)
    {
        C_SkillPrepare skillPacket = packet as C_SkillPrepare;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandlerPrepareSkill, player, skillPacket);
    }
    public static void C_SkillCancelHandler(PacketSession session, IMessage packet)
    {
        C_SkillCancel skillPacket = packet as C_SkillCancel;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandlerChargeCancelSkill, player, skillPacket);
    }



    public static void C_EnvRequestHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_EnvRequest envPacket = packet as C_EnvRequest;

        if (!DataManager.EnvDict.TryGetValue(envPacket.EnvType, out EnvInfo envData))
            return;

        Player player = clientSession.MyPlayer;
        GameRoom room = player?.Room;

        if (room == null)
            return;
        // 보상
        room.GetEnvManager?.GiveRewardToPlayer(envPacket.ObjectId, envPacket.EnvType);
     
        S_EnvRequest sendPacket = new S_EnvRequest()
        {
            ObjectId = envPacket.ObjectId,
            EnvType = envPacket.EnvType,
        };
        room.Push(room.Broadcast, sendPacket);
    }

    public static void C_TestDamageHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_TestDamage damagePacket = packet as C_TestDamage;

        // 검증 필요하면 추가하기..
        Player player = clientSession.MyPlayer;
        player.OnDamaged(player, 500);
    }

    public static void C_SkillCollisionProposeHandler(PacketSession session, IMessage packet)
    {
        C_SkillCollisionPropose skillPacket = packet as C_SkillCollisionPropose;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleSkillCollision, player, skillPacket);
    }

    public static void C_RestHandler(PacketSession session, IMessage packet)
    {
        var client = (ClientSession)session;
        var player = client?.MyPlayer;
        if (player?.Room == null)
            return;
        var req = (C_Rest)packet;

        player.Room.Push(player.Room.HandleRest, player, req);
    }

    // temp 임시 코드 나중에 수정
    public static void C_DeathHandler(PacketSession session, IMessage packet)
    {
        var client = (ClientSession)session;
        var player = client?.MyPlayer;
        if (player?.Room == null)
            return;
        var req = (C_Death)packet;

        player.Room.Push(player.Room.HandleDeath, player, req);
    }

    public static void C_KeyInputForTestHandler(PacketSession session, IMessage packet)
    {
        var client = (ClientSession)session;
        var player = client?.MyPlayer;
        if (player?.Room == null)
            return;
        var req = (C_KeyInputForTest)packet;

        player.Room.Push(player.Room.HandleKeyInputForTest, player, req);
    }

    // Receive and save the charging ratio from the client.
    public static void C_ChargingSkillHandler(PacketSession session, IMessage packet)
    {
        C_ChargingSkill chargePacket = packet as C_ChargingSkill;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleChargingSkill, player, chargePacket);
    }

    public static void C_OperateHandler(PacketSession session, IMessage packet)
    {
        C_Operate operatePkt = packet as C_Operate;
        ClientSession clientSession = session as ClientSession;
        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        if (!Enum.TryParse<Server.Game.Beacon>(operatePkt.BeaconName, true, out Server.Game.Beacon beacon))
            return;

        room.Push(player.Room.HandleOperate, player, beacon, operatePkt.PosX, operatePkt.PosZ);
    }
}
