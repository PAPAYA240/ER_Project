using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace Server.Game
{
    public class Player : GameObject
    {
        public ClientSession Session { get; set; }

        protected Dictionary<string, Skill> _skills = new Dictionary<string, Skill>();
        Dictionary<string, CoolTime> _coolDownDict = new Dictionary<string, CoolTime>();
        class CoolTime
        {
            public bool isCoolDown;     // 쿨타임이 돌고 있는지 (false : 사용 가능)
            public float coolTime;      // 남은 쿨타임
        }

        public Player()
        {
            ObjectType = GameObjectType.Player;            
        }

        public void MakeDict()
        {
            MakeSkillDict();
            MakeCoolDownDict();
        }

        public bool CanUseSkill(C_Skill skillPacket)
        {
            // 쿨타임 체크
            if (!CheckCoolTime(skillPacket))
                return false;

            // 스테미나 체크


            // 체크 끝나면 데이터 변경
            // ex : 쿨타임 돌리기 시작 등
            _ = CoInputCooltime(skillPacket.SkillInfo.KeyCode, FindSkill(skillPacket.SkillInfo.KeyCode).SkillData.cooldown);
            //StartCoroutine(CoInputCooltime(key, skill.SkillData.cooldown));

            return true;
        }

        public override void OnDamaged(GameObject attacker, float damage)
        {
            base.OnDamaged(attacker, damage);
        }

        public override void OnDead(GameObject attacker)
        {
            //base.OnDead(attacker);
        }

        private bool CheckCoolTime(C_Skill skillPacket)
        {
            if (!_coolDownDict[skillPacket.SkillInfo.KeyCode].isCoolDown)
                return true;

            return false;
        }

        private async Task CoInputCooltime(string key, float time)
        {
            _coolDownDict[key].isCoolDown = true;

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < time)
            {
                _coolDownDict[key].coolTime = (float)(time - sw.Elapsed.TotalSeconds);
                await Task.Delay(100); // 0.1초마다 남은 쿨타임 갱신
            }

            _coolDownDict[key].isCoolDown = false;
            _coolDownDict[key].coolTime = 0.0f;
        }

        private Skill FindSkill(string keyCode)
        {
            return _skills[keyCode];
        }

        private void MakeSkillDict()
        {
            // 본인 캐릭터의 스킬 정보만 추출
            string myCharName = Enum.GetName(typeof(CharacterType), Info.CharType);
            foreach (var skillData in DataManager.SkillDict)
            {
                string skillName = skillData.Key;
                int idx = skillName.IndexOf('_');
                string charName = idx >= 0 ? skillName.Substring(0, idx) : skillName;
                if (myCharName != charName)
                    continue;

                Skill skill = new Skill();
                skill.SkillData = skillData.Value;

                string key = skillData.Key.Substring(skillData.Key.Length - 1);
                _skills.Add(key, skill);
            }
        }

        private void MakeCoolDownDict()
        {
            foreach (var skill in _skills)
            {
                string keyCode = skill.Key;
                _coolDownDict[keyCode] = new CoolTime { isCoolDown = false, coolTime = 0.0f };
            }
        }
    }
}
