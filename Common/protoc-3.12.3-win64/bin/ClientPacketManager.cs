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
		_onRecv.Add((ushort)MsgId.SEnterGame, MakePacket<S_EnterGame>);
		_handler.Add((ushort)MsgId.SEnterGame, PacketHandler.S_EnterGameHandler);		
		_onRecv.Add((ushort)MsgId.SLeaveGame, MakePacket<S_LeaveGame>);
		_handler.Add((ushort)MsgId.SLeaveGame, PacketHandler.S_LeaveGameHandler);		
		_onRecv.Add((ushort)MsgId.SSpawn, MakePacket<S_Spawn>);
		_handler.Add((ushort)MsgId.SSpawn, PacketHandler.S_SpawnHandler);		
		_onRecv.Add((ushort)MsgId.SDespawn, MakePacket<S_Despawn>);
		_handler.Add((ushort)MsgId.SDespawn, PacketHandler.S_DespawnHandler);		
		_onRecv.Add((ushort)MsgId.SMove, MakePacket<S_Move>);
		_handler.Add((ushort)MsgId.SMove, PacketHandler.S_MoveHandler);		
		_onRecv.Add((ushort)MsgId.SSkill, MakePacket<S_Skill>);
		_handler.Add((ushort)MsgId.SSkill, PacketHandler.S_SkillHandler);		
		_onRecv.Add((ushort)MsgId.SChangeHp, MakePacket<S_ChangeHp>);
		_handler.Add((ushort)MsgId.SChangeHp, PacketHandler.S_ChangeHpHandler);		
		_onRecv.Add((ushort)MsgId.SDie, MakePacket<S_Die>);
		_handler.Add((ushort)MsgId.SDie, PacketHandler.S_DieHandler);		
		_onRecv.Add((ushort)MsgId.SAnim, MakePacket<S_Anim>);
		_handler.Add((ushort)MsgId.SAnim, PacketHandler.S_AnimHandler);		
		_onRecv.Add((ushort)MsgId.SState, MakePacket<S_State>);
		_handler.Add((ushort)MsgId.SState, PacketHandler.S_StateHandler);		
		_onRecv.Add((ushort)MsgId.SCharacter, MakePacket<S_Character>);
		_handler.Add((ushort)MsgId.SCharacter, PacketHandler.S_CharacterHandler);		
		_onRecv.Add((ushort)MsgId.SEnterPick, MakePacket<S_EnterPick>);
		_handler.Add((ushort)MsgId.SEnterPick, PacketHandler.S_EnterPickHandler);		
		_onRecv.Add((ushort)MsgId.SSpawnPick, MakePacket<S_SpawnPick>);
		_handler.Add((ushort)MsgId.SSpawnPick, PacketHandler.S_SpawnPickHandler);		
		_onRecv.Add((ushort)MsgId.SLeavePick, MakePacket<S_LeavePick>);
		_handler.Add((ushort)MsgId.SLeavePick, PacketHandler.S_LeavePickHandler);		
		_onRecv.Add((ushort)MsgId.SVisibleObjects, MakePacket<S_VisibleObjects>);
		_handler.Add((ushort)MsgId.SVisibleObjects, PacketHandler.S_VisibleObjectsHandler);		
		_onRecv.Add((ushort)MsgId.SLevelUp, MakePacket<S_LevelUp>);
		_handler.Add((ushort)MsgId.SLevelUp, PacketHandler.S_LevelUpHandler);		
		_onRecv.Add((ushort)MsgId.SFx, MakePacket<S_Fx>);
		_handler.Add((ushort)MsgId.SFx, PacketHandler.S_FxHandler);		
		_onRecv.Add((ushort)MsgId.STrait, MakePacket<S_Trait>);
		_handler.Add((ushort)MsgId.STrait, PacketHandler.S_TraitHandler);		
		_onRecv.Add((ushort)MsgId.SWeapon, MakePacket<S_Weapon>);
		_handler.Add((ushort)MsgId.SWeapon, PacketHandler.S_WeaponHandler);		
		_onRecv.Add((ushort)MsgId.SInteract, MakePacket<S_Interact>);
		_handler.Add((ushort)MsgId.SInteract, PacketHandler.S_InteractHandler);		
		_onRecv.Add((ushort)MsgId.SRespawn, MakePacket<S_Respawn>);
		_handler.Add((ushort)MsgId.SRespawn, PacketHandler.S_RespawnHandler);		
		_onRecv.Add((ushort)MsgId.SSkillLevelUp, MakePacket<S_SkillLevelUp>);
		_handler.Add((ushort)MsgId.SSkillLevelUp, PacketHandler.S_SkillLevelUpHandler);		
		_onRecv.Add((ushort)MsgId.SPlayerState, MakePacket<S_PlayerState>);
		_handler.Add((ushort)MsgId.SPlayerState, PacketHandler.S_PlayerStateHandler);		
		_onRecv.Add((ushort)MsgId.SChangeStat, MakePacket<S_ChangeStat>);
		_handler.Add((ushort)MsgId.SChangeStat, PacketHandler.S_ChangeStatHandler);		
		_onRecv.Add((ushort)MsgId.SCanStopSkill, MakePacket<S_CanStopSkill>);
		_handler.Add((ushort)MsgId.SCanStopSkill, PacketHandler.S_CanStopSkillHandler);		
		_onRecv.Add((ushort)MsgId.SChangeItemStat, MakePacket<S_ChangeItemStat>);
		_handler.Add((ushort)MsgId.SChangeItemStat, PacketHandler.S_ChangeItemStatHandler);		
		_onRecv.Add((ushort)MsgId.SChangeEquipItem, MakePacket<S_ChangeEquipItem>);
		_handler.Add((ushort)MsgId.SChangeEquipItem, PacketHandler.S_ChangeEquipItemHandler);		
		_onRecv.Add((ushort)MsgId.SChangeInventory, MakePacket<S_ChangeInventory>);
		_handler.Add((ushort)MsgId.SChangeInventory, PacketHandler.S_ChangeInventoryHandler);		
		_onRecv.Add((ushort)MsgId.SCombatText, MakePacket<S_CombatText>);
		_handler.Add((ushort)MsgId.SCombatText, PacketHandler.S_CombatTextHandler);		
		_onRecv.Add((ushort)MsgId.SChangeKDA, MakePacket<S_ChangeKDA>);
		_handler.Add((ushort)MsgId.SChangeKDA, PacketHandler.S_ChangeKDAHandler);		
		_onRecv.Add((ushort)MsgId.SSyncTimer, MakePacket<S_SyncTimer>);
		_handler.Add((ushort)MsgId.SSyncTimer, PacketHandler.S_SyncTimerHandler);		
		_onRecv.Add((ushort)MsgId.SEnvRequest, MakePacket<S_EnvRequest>);
		_handler.Add((ushort)MsgId.SEnvRequest, PacketHandler.S_EnvRequestHandler);		
		_onRecv.Add((ushort)MsgId.SAddAbigailCoord, MakePacket<S_AddAbigailCoord>);
		_handler.Add((ushort)MsgId.SAddAbigailCoord, PacketHandler.S_AddAbigailCoordHandler);		
		_onRecv.Add((ushort)MsgId.SRemoveAbigailCoord, MakePacket<S_RemoveAbigailCoord>);
		_handler.Add((ushort)MsgId.SRemoveAbigailCoord, PacketHandler.S_RemoveAbigailCoordHandler);		
		_onRecv.Add((ushort)MsgId.SMoveSync, MakePacket<S_MoveSync>);
		_handler.Add((ushort)MsgId.SMoveSync, PacketHandler.S_MoveSyncHandler);		
		_onRecv.Add((ushort)MsgId.SStop, MakePacket<S_Stop>);
		_handler.Add((ushort)MsgId.SStop, PacketHandler.S_StopHandler);		
		_onRecv.Add((ushort)MsgId.SSetMoveTarget, MakePacket<S_SetMoveTarget>);
		_handler.Add((ushort)MsgId.SSetMoveTarget, PacketHandler.S_SetMoveTargetHandler);		
		_onRecv.Add((ushort)MsgId.SSkillConfirm, MakePacket<S_SkillConfirm>);
		_handler.Add((ushort)MsgId.SSkillConfirm, PacketHandler.S_SkillConfirmHandler);		
		_onRecv.Add((ushort)MsgId.SSkillMotion, MakePacket<S_SkillMotion>);
		_handler.Add((ushort)MsgId.SSkillMotion, PacketHandler.S_SkillMotionHandler);		
		_onRecv.Add((ushort)MsgId.SOccupyBeacon, MakePacket<S_OccupyBeacon>);
		_handler.Add((ushort)MsgId.SOccupyBeacon, PacketHandler.S_OccupyBeaconHandler);		
		_onRecv.Add((ushort)MsgId.SChangeBeaconTime, MakePacket<S_ChangeBeaconTime>);
		_handler.Add((ushort)MsgId.SChangeBeaconTime, PacketHandler.S_ChangeBeaconTimeHandler);		
		_onRecv.Add((ushort)MsgId.SChangeScore, MakePacket<S_ChangeScore>);
		_handler.Add((ushort)MsgId.SChangeScore, PacketHandler.S_ChangeScoreHandler);		
		_onRecv.Add((ushort)MsgId.SGameOver, MakePacket<S_GameOver>);
		_handler.Add((ushort)MsgId.SGameOver, PacketHandler.S_GameOverHandler);		
		_onRecv.Add((ushort)MsgId.SChangeTransform, MakePacket<S_ChangeTransform>);
		_handler.Add((ushort)MsgId.SChangeTransform, PacketHandler.S_ChangeTransformHandler);		
		_onRecv.Add((ushort)MsgId.STargetChange, MakePacket<S_TargetChange>);
		_handler.Add((ushort)MsgId.STargetChange, PacketHandler.S_TargetChangeHandler);		
		_onRecv.Add((ushort)MsgId.SSnare, MakePacket<S_Snare>);
		_handler.Add((ushort)MsgId.SSnare, PacketHandler.S_SnareHandler);		
		_onRecv.Add((ushort)MsgId.SSkillCost, MakePacket<S_SkillCost>);
		_handler.Add((ushort)MsgId.SSkillCost, PacketHandler.S_SkillCostHandler);		
		_onRecv.Add((ushort)MsgId.SSkillCollisionRequest, MakePacket<S_SkillCollisionRequest>);
		_handler.Add((ushort)MsgId.SSkillCollisionRequest, PacketHandler.S_SkillCollisionRequestHandler);		
		_onRecv.Add((ushort)MsgId.SRotateToPos, MakePacket<S_RotateToPos>);
		_handler.Add((ushort)MsgId.SRotateToPos, PacketHandler.S_RotateToPosHandler);		
		_onRecv.Add((ushort)MsgId.SAddYukiPyosik, MakePacket<S_AddYukiPyosik>);
		_handler.Add((ushort)MsgId.SAddYukiPyosik, PacketHandler.S_AddYukiPyosikHandler);
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