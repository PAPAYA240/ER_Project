using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Google.Protobuf.Protocol
{
    public sealed partial class PositionInfo
    {
        public float Distance(PositionInfo other)
        {
            float dx = PosX - other.PosX;
            float dy = PosX - other.PosX;
            float dz = PosX - other.PosX;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // Vector3 -> PositionInfo (암시적 변환)
        public static implicit operator PositionInfo(Vector3 v)
            => new PositionInfo { PosX = v.X, PosY = v.Y, PosZ = v.Z };

        // PositionInfo -> Vector3 (암시적 변환)
        public static implicit operator Vector3(PositionInfo p)
            => new Vector3(p.PosX, p.PosY, p.PosZ);
    }

    public sealed partial class StatInfo
    {
        public void MultiplyForGrowth(int levelUpCnt)
        {
            Attack *= levelUpCnt;
            Defense *= levelUpCnt;
            //Hp *= levelUpCnt;
            MaxHp *= levelUpCnt;
            HpRegen *= levelUpCnt;
            //Stamina *= levelUpCnt;
            MaxStamina *= levelUpCnt;
            StaminaRegen *= levelUpCnt;
            AttackSpeed *= levelUpCnt;
        }

        public void AddStat(StatInfo other)
        {
            Attack += other.Attack;
            Defense += other.Defense;
            Hp += other.MaxHp;
            MaxHp += other.MaxHp;
            HpRegen += other.HpRegen;
            Stamina *= other.MaxStamina;
            MaxStamina += other.MaxStamina;
            StaminaRegen += other.StaminaRegen;
            AttackSpeed += other.AttackSpeed;
        }
    }


    public sealed partial class RotationInfo
    {
        public static Vector3 operator *(RotationInfo q, Vector3 v)
        {
            Vector3 u = new Vector3(q.Qx, q.Qy, q.Qz);
            float s = q.Qw;

            return 2.0f * Vector3.Dot(u, v) * u +
                   (s * s - Vector3.Dot(u, u)) * v +
                   2.0f * s * Vector3.Cross(u, v);
        }

        public Vector3 Forward() => this * new Vector3(0, 0, 1);
        public Vector3 Right() => this * new Vector3(1, 0, 0);
        public Vector3 Back() => -Forward();
        public Vector3 Left() => -Right();
    }
}