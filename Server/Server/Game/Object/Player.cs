using Google.Protobuf.Protocol;
using Server.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public class Player : GameObject
    {
        public ClientSession Session { get; set; }

        protected Dictionary<KeyCode, Skill> _skills = new Dictionary<KeyCode, Skill>();  // key : KeyCode
        Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
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
            KeyCode keyCode = (KeyCode)skillPacket.SkillInfo.KeyCode;

            // 쿨타임 체크
            if (!CheckCoolTime(keyCode))
                return false;

            // 스테미나 체크


            // 체크 끝나면 데이터 변경
            // ex : 쿨타임 돌리기 시작 등
            _ = CoInputCooltime(keyCode, FindSkill(keyCode).CurLevelCooldown);

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

        private bool CheckCoolTime(KeyCode key)
        {
            if (!_coolDownDict[key].isCoolDown)
                return true;

            return false;
        }

        private async Task CoInputCooltime(KeyCode key, float time)
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

        private Skill FindSkill(KeyCode key)
        {
            return _skills[key];
        }

        private void MakeSkillDict()
        {
            // 본인 캐릭터의 스킬 정보만 추출
            Dictionary<KeyCode, SkillData> skills = DataManager.SkillDict[Info.CharType];
            foreach (var skillData in skills)
            {
                Skill skill = new Skill();
                skill.SkillData = skillData.Value;

                _skills.Add(skillData.Key, skill);
            }
        }

        private void MakeCoolDownDict()
        {
            foreach (var skill in _skills)
            {
                _coolDownDict[skill.Key] = new CoolTime { isCoolDown = false, coolTime = 0.0f };
            }
        }
    }
}
