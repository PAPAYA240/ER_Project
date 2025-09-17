using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	PacketManager()
	{
		Register();
	}

	Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv = new Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>>();
	Dictionary<ushort, Action<PacketSession, IMessage>> _handler = new Dictionary<ushort, Action<PacketSession, IMessage>>();
		
	public Action<PacketSession, IMessage, ushort> CustomHandler { get; set; }

	public void Register()
	{		
		_onRecv.Add((ushort)MsgId.CEnterGame, MakePacket<C_EnterGame>);
		_handler.Add((ushort)MsgId.CEnterGame, PacketHandler.C_EnterGameHandler);		
		_onRecv.Add((ushort)MsgId.CMove, MakePacket<C_Move>);
		_handler.Add((ushort)MsgId.CMove, PacketHandler.C_MoveHandler);		
		_onRecv.Add((ushort)MsgId.CSkill, MakePacket<C_Skill>);
		_handler.Add((ushort)MsgId.CSkill, PacketHandler.C_SkillHandler);		
		_onRecv.Add((ushort)MsgId.CAnim, MakePacket<C_Anim>);
		_handler.Add((ushort)MsgId.CAnim, PacketHandler.C_AnimHandler);		
		_onRecv.Add((ushort)MsgId.CCharacter, MakePacket<C_Character>);
		_handler.Add((ushort)MsgId.CCharacter, PacketHandler.C_CharacterHandler);		
		_onRecv.Add((ushort)MsgId.CSkillEnd, MakePacket<C_SkillEnd>);
		_handler.Add((ushort)MsgId.CSkillEnd, PacketHandler.C_SkillEndHandler);		
		_onRecv.Add((ushort)MsgId.CPing, MakePacket<C_Ping>);
		_handler.Add((ushort)MsgId.CPing, PacketHandler.C_PingHandler);		
		_onRecv.Add((ushort)MsgId.CReady, MakePacket<C_Ready>);
		_handler.Add((ushort)MsgId.CReady, PacketHandler.C_ReadyHandler);		
		_onRecv.Add((ushort)MsgId.CTrait, MakePacket<C_Trait>);
		_handler.Add((ushort)MsgId.CTrait, PacketHandler.C_TraitHandler);		
		_onRecv.Add((ushort)MsgId.CWeapon, MakePacket<C_Weapon>);
		_handler.Add((ushort)MsgId.CWeapon, PacketHandler.C_WeaponHandler);
	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Action<PacketSession, ArraySegment<byte>, ushort> action = null;
		if (_onRecv.TryGetValue(id, out action))
			action.Invoke(session, buffer, id);
	}

	void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
	{
		T pkt = new T();
		pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

		if(CustomHandler != null)
		{
			CustomHandler.Invoke(session, pkt, id);
		}
		else
		{
			Action<PacketSession, IMessage> action = null;
			if (_handler.TryGetValue(id, out action))
				action.Invoke(session, pkt);
		}
	}

	public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
	{
		Action<PacketSession, IMessage> action = null;
		if (_handler.TryGetValue(id, out action))
			return action;
		return null;
	}
}