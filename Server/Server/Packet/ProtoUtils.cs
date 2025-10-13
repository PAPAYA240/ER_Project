using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;

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

    public partial class ItemStat
    {
        public static ItemStat operator +(ItemStat a, ItemStat b)
        {
            // 새로운 ItemStat 객체를 생성하여 두 객체의 값을 합산
            return new ItemStat
            {
                AttackDamage = a.AttackDamage + b.AttackDamage,
                AttackSpeed = a.AttackSpeed + b.AttackSpeed,
                CriticalRatio = a.CriticalRatio + b.CriticalRatio,
                CriticalDamage = a.CriticalDamage + b.CriticalDamage,
                AttackRange = a.AttackRange + b.AttackRange,
                FixedSkillAmplification = a.FixedSkillAmplification + b.FixedSkillAmplification,
                PercentageSkillAmplification = a.PercentageSkillAmplification + b.PercentageSkillAmplification,
                SkillAcceleration = a.SkillAcceleration + b.SkillAcceleration,
                FixedDefensePenetration = a.FixedDefensePenetration + b.FixedDefensePenetration,
                PercentageDefensePenetration = a.PercentageDefensePenetration + b.PercentageDefensePenetration,
                FixedSpeed = a.FixedSpeed + b.FixedSpeed,
                PercentageSpeed = a.PercentageSpeed + b.PercentageSpeed,
                MaxHp = a.MaxHp + b.MaxHp,
                HpRegen = a.HpRegen + b.HpRegen,
                MaxStamina = a.MaxStamina + b.MaxStamina,
                StaminaRegen = a.StaminaRegen + b.StaminaRegen,
                Defense = a.Defense + b.Defense,
                LifeSteal = a.LifeSteal + b.LifeSteal,
                Omnivamp = a.Omnivamp + b.Omnivamp,
                HealingPower = a.HealingPower + b.HealingPower,
                SlowResistance = a.SlowResistance + b.SlowResistance,
                CCResistance = a.CCResistance + b.CCResistance,
                AdaptiveStat = a.AdaptiveStat + b.AdaptiveStat,
                Vision = a.Vision + b.Vision,
                AttackDamagePerLevel = a.AttackDamagePerLevel + b.AttackDamagePerLevel,
                SkillAmplificationPerLevel = a.SkillAmplificationPerLevel + b.SkillAmplificationPerLevel,
                MaxHpPerLevel = a.MaxHpPerLevel + b.MaxHpPerLevel,
            };
        }
    }

    public sealed partial class SkillHitbox
    {
        public void SetDefaultsIfEmpty()
        {
            if (Fps == 0)
                Fps = 30;
        }
    }
}