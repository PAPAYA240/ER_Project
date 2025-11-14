using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;

public class Projectile_Rozzi_R : Projectile
{
    private readonly float _maxDistance = 5.5f;
    private int _tStartTick, _tEndTick;

    private float _duration = 3f;

    public override void Init()
    {
        if (Owner == null)
            return;

        _tStartTick = TimeUtil.LastTick;
        _tEndTick = unchecked(_tStartTick + (int)MathF.Round(_duration * 1000f));

        // Owner의 현재 위치를 복사
        Info.PosInfo = new PositionInfo
        {
            PosX = Owner.PosInfo.PosX,
            PosY = Owner.PosInfo.PosY,
            PosZ = Owner.PosInfo.PosZ
        };
        Info.RotInfo = Owner.RotInfo;
    }

    public override void Update()
    {
        if (Owner == null)  // TEMP: Owner가 죽어도 남아있기?
            return;

        if (Deactivation())
        {
            Room.LeaveGame(Id);
            return;
        }

        UpdatePosition();
    }

    protected override bool Deactivation()
    {
        int now = TimeUtil.LastTick;

        if (TimeUtil.IsPastOrNow(now, _tEndTick))
            return true;

        return false;
    }

    private void UpdatePosition()
    {

    }
}

