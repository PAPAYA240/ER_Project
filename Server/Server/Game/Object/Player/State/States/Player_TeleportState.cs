using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework.Interfaces;
using Server.Data;
using Server.Game;
using static Server.Game.GameObject;


public class Player_TeleportState : IPlayerState
{
    readonly string _animName = "OPERATE";
    private float _duration = 3.0f;
    private double _startTime;
    private PositionInfo _ioPos = default;

    public Player_TeleportState(PositionInfo ioPos)
    {
        _ioPos = ioPos;
    }

    public void Enter(Player player)
    {
        if(_ioPos == default)
        {
            player.ChangeState(new Player_IdleState());
            return;
        }

        player.SendAnimPacket(_animName, 0.1f);
        player.SendStopPacket(StopReason.StopMoveOnly);

        //_duration = DataManager.AnimLengthInfoDict[player.Info.Player.CharType][_animName].Length;
        _startTime = TimeUtil.UtcSec();

        LookAtObject(player);
    }

    public void Execute(Player player)
    {
        bool animFinished = _startTime + _duration <= TimeUtil.UtcSec();

        if (animFinished)
        {
            player.PosInfo.MergeFrom(player.Room.Teleport.GetTeleportPoint(player).ToPositionInfo());
            player.SendChangeTransformPacket(true);
            player.ChangeState(new Player_IdleState());

            StatusEffect se = new StatusEffect();
            se.type = "Untargetable";
            se.duration = 1f;
            player.AddStatusEffect(se);
        }
    }

    public void Exit(Player player)
    {
        player.SendDeployingPacket();
    }

    private void LookAtObject(Player player)
    {
        Vector2 ioPos = new Vector2(_ioPos.PosX, _ioPos.PosZ);
        Vector2 myPos = new Vector2(player.PosInfo.PosX, player.PosInfo.PosZ);
        Vector2 dir = ioPos - myPos;

        if (dir.LengthSquared() < 0.0001f)
            return;

        float angle = (float)Math.Atan2(dir.X, dir.Y);
        Quaternion rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

        player.RotInfo = new RotationInfo
        {
            Qx = rot.X,
            Qy = rot.Y,
            Qz = rot.Z,
            Qw = rot.W
        };
        player.SendChangeTransformPacket();
    }
}

