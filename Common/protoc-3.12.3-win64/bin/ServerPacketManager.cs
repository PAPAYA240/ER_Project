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
		_onRecv.Add((ushort)MsgId.CFx, MakePacket<C_Fx>);
		_handler.Add((ushort)MsgId.CFx, PacketHandler.C_FxHandler);		
		_onRecv.Add((ushort)MsgId.CPing, MakePacket<C_Ping>);
		_handler.Add((ushort)MsgId.CPing, PacketHandler.C_PingHandler);		
		_onRecv.Add((ushort)MsgId.CReady, MakePacket<C_Ready>);
		_handler.Add((ushort)MsgId.CReady, PacketHandler.C_ReadyHandler);		
		_onRecv.Add((ushort)MsgId.CTrait, MakePacket<C_Trait>);
		_handler.Add((ushort)MsgId.CTrait, PacketHandler.C_TraitHandler);		
		_onRecv.Add((ushort)MsgId.CWeapon, MakePacket<C_Weapon>);
		_handler.Add((ushort)MsgId.CWeapon, PacketHandler.C_WeaponHandler);		
		_onRecv.Add((ushort)MsgId.CInteract, MakePacket<C_Interact>);
		_handler.Add((ushort)MsgId.CInteract, PacketHandler.C_InteractHandler);		
		_onRecv.Add((ushort)MsgId.CSkillLevelUp, MakePacket<C_SkillLevelUp>);
		_handler.Add((ushort)MsgId.CSkillLevelUp, PacketHandler.C_SkillLevelUpHandler);		
		_onRecv.Add((ushort)MsgId.CTargetingSkill, MakePacket<C_TargetingSkill>);
		_handler.Add((ushort)MsgId.CTargetingSkill, PacketHandler.C_TargetingSkillHandler);		
		_onRecv.Add((ushort)MsgId.CTestDamage, MakePacket<C_TestDamage>);
		_handler.Add((ushort)MsgId.CTestDamage, PacketHandler.C_TestDamageHandler);		
		_onRecv.Add((ushort)MsgId.CEnvRequest, MakePacket<C_EnvRequest>);
		_handler.Add((ushort)MsgId.CEnvRequest, PacketHandler.C_EnvRequestHandler);		
		_onRecv.Add((ushort)MsgId.CMoveSync, MakePacket<C_MoveSync>);
		_handler.Add((ushort)MsgId.CMoveSync, PacketHandler.C_MoveSyncHandler);		
		_onRecv.Add((ushort)MsgId.CAttack, MakePacket<C_Attack>);
		_handler.Add((ushort)MsgId.CAttack, PacketHandler.C_AttackHandler);		
		_onRecv.Add((ushort)MsgId.CSetMoveTarget, MakePacket<C_SetMoveTarget>);
		_handler.Add((ushort)MsgId.CSetMoveTarget, PacketHandler.C_SetMoveTargetHandler);		
		_onRecv.Add((ushort)MsgId.CStop, MakePacket<C_Stop>);
		_handler.Add((ushort)MsgId.CStop, PacketHandler.C_StopHandler);		
		_onRecv.Add((ushort)MsgId.CSkillInput, MakePacket<C_SkillInput>);
		_handler.Add((ushort)MsgId.CSkillInput, PacketHandler.C_SkillInputHandler);		
		_onRecv.Add((ushort)MsgId.CSkillCollisionPropose, MakePacket<C_SkillCollisionPropose>);
		_handler.Add((ushort)MsgId.CSkillCollisionPropose, PacketHandler.C_SkillCollisionProposeHandler);		
		_onRecv.Add((ushort)MsgId.CRest, MakePacket<C_Rest>);
		_handler.Add((ushort)MsgId.CRest, PacketHandler.C_RestHandler);		
		_onRecv.Add((ushort)MsgId.CDeath, MakePacket<C_Death>);
		_handler.Add((ushort)MsgId.CDeath, PacketHandler.C_DeathHandler);		
		_onRecv.Add((ushort)MsgId.CSkillPrepare, MakePacket<C_SkillPrepare>);
		_handler.Add((ushort)MsgId.CSkillPrepare, PacketHandler.C_SkillPrepareHandler);		
		_onRecv.Add((ushort)MsgId.CSkillCancel, MakePacket<C_SkillCancel>);
		_handler.Add((ushort)MsgId.CSkillCancel, PacketHandler.C_SkillCancelHandler);		
		_onRecv.Add((ushort)MsgId.CSkillExecute, MakePacket<C_SkillExecute>);
		_handler.Add((ushort)MsgId.CSkillExecute, PacketHandler.C_SkillExecuteHandler);
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