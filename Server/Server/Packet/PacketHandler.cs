using System;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Data;
using Server.Game;
using Server.Game.Object.Monster;
using ServerCore;

class PacketHandler
{
    public static void C_EnterGameHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;

        Player player = clientSession.MyPlayer;
        if (player == null)
            return;

        GameRoom room = RoomManager.Instance.Find(1);
        if (room == null)
            return;

        room.Push(room.EnterGame, player);
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

    public static void C_SkillEndHandler(PacketSession session, IMessage packet)
    {
        C_SkillEnd skillEndPacket = packet as C_SkillEnd;
        if (skillEndPacket == null)
            return;
        
        GameRoom room = RoomManager.Instance.Find(1);
        if (room == null)
            return;

        room.Push(() =>
        {
            if (room.TryGetMonster(skillEndPacket.ObjectInfo.ObjectId, out Monster monster))
            {
                C_SkillEnd broadcastPacket = new C_SkillEnd();
                broadcastPacket.ObjectInfo = skillEndPacket.ObjectInfo;
            }
        });
    }
  
    public static void C_CharacterHandler(PacketSession session, IMessage packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Character charPacket = packet as C_Character;
        clientSession.MyCharacter = charPacket.CharType;

        GameRoom room = RoomManager.Instance.Find(1);
        if (room == null)
            return;

        clientSession.MyPlayer = ObjectManager.Instance.Add<Player>();
        {
            clientSession.MyPlayer.Info.Name = $"Player_{clientSession.MyPlayer.Info.ObjectId}";
            clientSession.MyPlayer.Info.PosInfo.State = CreatureState.Idle;
            clientSession.MyPlayer.Info.PosInfo.PosX = 0;
            clientSession.MyPlayer.Info.PosInfo.PosY = 0;
            clientSession.MyPlayer.Info.CharType = clientSession.MyCharacter;

            StatInfo stat = null;
            DataManager.StatDict.TryGetValue(1, out stat);
            clientSession.MyPlayer.Stat.MergeFrom(stat);

            clientSession.MyPlayer.Session = clientSession;
        }
        // PickRoom 클래스 만들고 거기서 호출해야할듯
        //room.Push(pickPacket);
    }
}
