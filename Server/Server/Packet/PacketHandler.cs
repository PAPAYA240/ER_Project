using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;
using Server.Game.Object.Monster;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

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
            clientSession.MyPlayer.Info.CharType = clientSession.MyCharacter;
            clientSession.MyPlayer.MakeDict();

            StatInfo stat = null;
            DataManager.StatDict.TryGetValue(clientSession.MyCharacter, out stat);
            clientSession.MyPlayer.Stat.MergeFrom(stat);
            clientSession.MyPlayer.Hp = clientSession.MyPlayer.Stat.MaxHp;
            clientSession.MyPlayer.Stamina = clientSession.MyPlayer.Stat.MaxStamina;
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
        room.Push(room.AddDummyPlayers, clientSession, enterGamePkt.DummyPlayers.ToList());
    }

    public static void C_MoveHandler(PacketSession session, IMessage packet)
	{
		C_Move movePacket = packet as C_Move;
		ClientSession clientSession = session as ClientSession;

        //Console.WriteLine($"C_Move ({movePacket.PosInfo.PosX}, {movePacket.PosInfo.PosY}, {movePacket.PosInfo.PosZ})");

		Player player = clientSession.MyPlayer;
        if (player == null)
			return;

		GameRoom room = player.Room;
		if (room == null)
			return;

		room.Push(room.HandleMove, player, movePacket);
	}

	public static void C_SkillHandler(PacketSession session, IMessage packet)
	{
        C_Skill skillPacket = packet as C_Skill;
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

		room.Push(room.HandleSkill, player, skillPacket);
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

        room.Push(room.HandleVF, player, skillPacket);
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
        room.Broadcast(s_charPacket, c_charPacket.PickIdx);

        //GameRoom room = RoomManager.Instance.Find(2) as GameRoom;
        //if (room == null)
        //    return;


        // PickRoom 클래스 만들고 거기서 호출해야할듯
        //room.Push(pickPacket);
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
        room.Broadcast(s_traitPacket, c_traitPacket.PickIdx);
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
        room.Broadcast(s_weaponPacket, c_weaponPacket.PickIdx);
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
            //패킷을 보낸다.? 푸쉬한다?
            room.Push(room.SkillLevelUp, player.Id, skillInfoChangePacket.KeyCode);
        }
    }

    public static void C_PlayerStateHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_PlayerState statePacket = packet as C_PlayerState;

        // 검증 필요하면 추가하기..
        Player player = clientSession.MyPlayer;
        player.State = statePacket.State;
    }

    public static void C_AttackSkillTargetHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_AttackSkillTarget atkSkillTargetPkt = packet as C_AttackSkillTarget;
        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = player.Room;
        if (room == null)
            return;

        room.Push(room.HandleAttackSkillTarget, player, atkSkillTargetPkt);
    }
}
