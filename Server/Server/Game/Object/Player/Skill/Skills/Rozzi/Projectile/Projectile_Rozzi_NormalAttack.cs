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
    
    public void SendRozziNormalAttackPacket(Player owner, int targetId, int projectileId, bool isLWeapon, float speed)
    {
        SendAddPacket(owner, targetId, isLWeapon, speed);

        SendFxPacket(owner, projectileId);
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
        Console.WriteLine($"@ Send Add Packet : {Id}");
    }

    private void SendFxPacket(Player owner, int projectileId)
    {
        //owner.SendSkillEffect(new Vector2(Position.X, Position.Z), keyCode: _keyCode, sendLookatMousePacket: true,
        //        targetPos: default, targetRot: default,
        //        type: "Select", "Projectile_FX_BI_Rozzi_NormalAttack",
        //        useTargetTransform: true, targetId: projectileId);

        owner.SendSkillEffect(new Vector2(Position.X, Position.Z), keyCode: _keyCode, sendLookatMousePacket: true,
                targetPos: default, targetRot: default,
                type: "Select", "FX_BI_Rozzi_NormalAttack_Shot",
                useTargetTransform: true, targetId: projectileId);
    }
}

