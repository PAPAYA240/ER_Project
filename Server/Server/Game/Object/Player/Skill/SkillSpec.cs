using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public sealed class SkillSpec
{
    public string Id;
    public string AnimName;
    public float Windup = 0f;
    public float Backswing = 0f;

    public MoveSpec Move = new MoveSpec();
    public HitboxSpec Hitbox = new HitboxSpec();
    public CollisionSpec Collision = new CollisionSpec();

    // JSON 태그/효과가 있다면 여기에 보조 메서드 추가
    public bool HasEffect(string tag) => false; // TODO: 필요하면 구현
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

public sealed class SkillContext
{
    public Vector2 MousePos;            // XZ
    public int TargetId;                // 필요 시
    public KeyCode Key;
}