using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Hyunwoo : Player
    {
        public int TSkillCurrnetCount = 0;
        public List<int> TSkillMaxCount = new List<int>{ 99, 10, 9, 8 };
        private float _basicRecoveryAmount = 0.03f;
        private float _addtionalRecoveryAmount = 0.04f;


        public void AddTSkillCount()
        {
            lock (this)
            {
                TSkillCurrnetCount = Math.Min(TSkillCurrnetCount + 1, TSkillMaxCount[_skills[Data.DataUtils.KeyCode.T].CurLevel]);
            }
        }

        public void ActivateTSKill()
        {
            lock (this)
            {
                // 체력 회복 매커니즘?
                TSkillCurrnetCount = 0;

                int curTLevel = _skills[Data.DataUtils.KeyCode.T].CurLevel;
                if (curTLevel > 0)
                {
                    float hpRecoveryRatio = _basicRecoveryAmount + _addtionalRecoveryAmount * curTLevel;
                    float recoveryHp = MaxHp * hpRecoveryRatio;
                    Hp += recoveryHp;
                    _isUpdatedStat = true;

                    Skill.Reduce(Data.DataUtils.KeyCode.W, 2f);

                    S_CombatText packet = new S_CombatText();
                    packet.ObjectId = Id;
                    packet.Type = CombatTextType.HpRecovery;
                    packet.Value = recoveryHp;

                    Room.Broadcast(packet);

                    //Console.WriteLine("현우 패시브 발동!");
                }
            }
        }

        public bool CheckTActivate()
        {
            if(TSkillCurrnetCount == TSkillMaxCount[_skills[Data.DataUtils.KeyCode.T].CurLevel])
                return true;
            return false;
        }
    }
}
