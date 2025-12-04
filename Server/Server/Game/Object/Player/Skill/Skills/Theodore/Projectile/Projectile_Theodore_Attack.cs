using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Numerics;
using static Server.Data.DataUtils;

public class Projectile_Theodore_Attack : Projectile
{
    private KeyCode _keyCode = KeyCode.F2;

    public void SendTheodoreNormalAttackPacket(Player owner, int targetId, float speed)
    {
        SendAddPacket(owner, targetId, speed);

        SendFxPacket(owner);
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

    private void SendAddPacket(Player owner, int targetId, float speed)
    {
        S_TheodoreAttack packet = new S_TheodoreAttack()
        {
            ObjectId = Id,
            TargetId = targetId,
            Speed = speed
        };

        owner.Room.Push(owner.Room.Broadcast, packet);
    }

    private void SendFxPacket(Player owner)
    {
        if (ProjectileType == ProjectileType.ProjectileTheodoreNormalAttack)
        {
            owner.SendSkillEffect(new Vector2(Position.X, Position.Z), keyCode: _keyCode, sendLookatMousePacket: false,
            targetPos: default, targetRot: default,
            type: "Caster", name: "FX_NormalAttack");
        }
    }
}
