using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server;
using Server.Game;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

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
}
