using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public class SkillSpec
{
    public SkillNeed needs;
    public SkillLimits limits;
}

public class SkillNeed
{
    public bool endBlocked;
    public bool endPass;
    public bool behindBlocked;
    public bool candidateTargetId;
}

public class SkillLimits
{
    public float baseMaxDist;
    public float extraMaxBehind;
    public float speed;
}

[Serializable]
public class SkillVariants
{
    // 없는 건 null 허용
    public SkillSpec cast;
    public SkillSpec followup;

    public bool IsEmpty => cast == null && followup == null;
}

public sealed class MoveSpec
{
    public float Distance = 0f;         // 대쉬 등
    public float Speed = 0f;            // 채널형 이동 등
}

public sealed class HitboxSpec
{
    public float Radius = 0.5f;         // 구/캡슐 반경
                                        // shape/size 확장 가능
}

public sealed class CollisionSpec
{
    public bool StopOnWall = true;
    public bool SlideOnWall = false;    
    public float Skin = 0.05f;          // 벽 앞에서 살짝 띄우기
}