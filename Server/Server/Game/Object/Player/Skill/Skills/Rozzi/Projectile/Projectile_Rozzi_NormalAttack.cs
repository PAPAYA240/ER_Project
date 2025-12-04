using Google.Protobuf;
using Google.Protobuf.Protocol;
using Lucene.Net.Store;
using Server.Data;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using static Server.Data.DataUtils;
using ValueType = Server.Game.ValueType;

public class Projectile_Rozzi_NormalAttack : Projectile
{
    private KeyCode _keyCode = KeyCode.F3;
    
    public void SendRozziNormalAttackPacket(Player owner, int targetId, bool isLWeapon, float speed)
    {
        SendAddPacket(owner, targetId, isLWeapon, speed);

        SendFxPacket(owner, isLWeapon);
    }

    public override void Init()
    {
        if (Owner == null)
            return;

        _endTime = Environment.TickCount64 + 1000;

        // Owner의 현재 위치를 복사
        Info.PosInfo = new PositionInfo
        {
            PosX = Owner.PosInfo.PosX,
            PosY = Owner.PosInfo.PosY,
            PosZ = Owner.PosInfo.PosZ
        };
        Info.RotInfo = new RotationInfo
        {
            Qx = Owner.RotInfo.Qx,
            Qy = Owner.RotInfo.Qy,
            Qz = Owner.RotInfo.Qz,
            Qw = Owner.RotInfo.Qw
        };
    }

    public override void Update()
    {
        if (Owner == null)
            return;

        if (Deactivation())
        {
            Room.Push(Room.LeaveGame, Id);
            return;
        }
    }

    private void SendAddPacket(Player owner, int targetId, bool isLWeapon, float speed)
    {
        S_RozziNormalAttack packet = new S_RozziNormalAttack()
        {
            ObjectId = Id,
            TargetId = targetId,
            IsLWeapon = isLWeapon,
            Speed = speed
        };

        owner.Room.Push(owner.Room.Broadcast, packet);
    }

    private void SendFxPacket(Player owner, bool isLWeapon)
    {
        owner.SendSkillEffect(new Vector2(Position.X, Position.Z), keyCode: _keyCode, sendLookatMousePacket: false,
                targetPos: default, targetRot: default,
                type: "Select", name: (isLWeapon ? "FX_BI_Rozzi_NormalAttack_Shot_L" : "FX_BI_Rozzi_NormalAttack_Shot_R"),
                useTargetTransform: true, targetId: owner.Id);
    }
}

