using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// 스킬 사용이 가능한지 체크 (쿨타임, 마나 등등)
// 사용이 가능할 때 해당 스킬 함수 호출
// 클라에서 일단 스킬 시전 후 실제 판정 처리는 서버에서
// SkillData.json의 name과 Skill Class의 이름이 같아야함
public class SkillManager
{
    private Dictionary<string, SkillBase> _skills = new Dictionary<string, SkillBase>();

    public void Init()
    {
        var skillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(SkillBase)) && !t.IsAbstract);
        
        foreach(var type in skillTypes)
        {
            // SkillBase를 상속받은 클래스들을 탐색해 생성
            // 클래스의 이름으로 SkillDict에서 SkillData을 검색해 데이터를 채워줌
            SkillBase skill = (SkillBase)Activator.CreateInstance(type);
            skill.SkillData = Managers.Data.SkillDict[type.Name];
            _skills.Add(type.Name, skill);
        }
    }

    public void UseSkill(string skillName)
    {
        // 스킬 사용 가능 여부 체크
        if(!CanUseSkill(skillName))
        {
            float coolTime = GetSkillCoolTime(skillName);
            Debug.Log($"스킬 사용 불가 : {coolTime}");
            return;
        }

        SkillBase skill = FindSkill(skillName);

        // 사용 시 쿨타임, 마나 등 초기화
        SkillData skillData = skill.SkillData;
        Debug.Log($"스킬 사용 : {skillData.name}");
        skillData.lastUsedTime = Time.time;
        // TODO : 마나 초기화

        // 스킬 실행
        skill.Execute();

        // 서버 전송
    }

    public float GetSkillCoolTime(string skillName)
    {
        SkillData skillData = FindSkillData(skillName);

        return (FindSkillData(skillName).lastUsedTime + skillData.cooldown) - Time.time;
    }

    private bool CanUseSkill(string skillName)
    {
        SkillData skillData = FindSkillData(skillName);

        return Time.time >= skillData.lastUsedTime + skillData.cooldown; // TODO : && 마나;
    }

    private SkillBase FindSkill(string skillName)
    {
        SkillBase skillBase = null;
        if (!_skills.TryGetValue(skillName, out skillBase))
        {
            Debug.Log($"Skill을 찾을 수 없음 : {skillName}");
            return null;
        }

        return skillBase;
    }

    private SkillData FindSkillData(string skillName)
    {
        SkillBase skillBase = null;
        if(!_skills.TryGetValue(skillName, out skillBase))
        {
            Debug.Log($"Skill Data를 찾을 수 없음 : {skillName}");
            return null;
        }

        return skillBase.SkillData;
    }
}

