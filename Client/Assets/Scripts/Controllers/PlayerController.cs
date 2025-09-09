using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Windows;
using static Define;

public class PlayerController : CreatureController
{
    protected Coroutine _coSkill;
    protected bool _rangedSkill = false;

    protected Dictionary<string, SkillBase> _skills = new Dictionary<string, SkillBase>();

    float length;

    protected override void Init()
    {
        base.Init();
        MakeSkillDict();
        ObjectType = Define.Object.OtherPlayer;
    }

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    protected virtual void CheckUpdatedFlag()
    {

    }

    public override void OnDamaged()
    {
        Debug.Log("Player HIT !");
    }

    #region Util
    protected string GetCharacterName()
    {
        return Enum.GetName(typeof(CharacterType), ObjInfo.CharType);
    }
    #endregion


    #region Skill
    public override void UseSkill(string keyCode)
    {
        SkillBase skill = FindSkill(keyCode);
        skill.Execute();

        _coSkill = StartCoroutine("CoStartSkill");
        Debug.Log("스킬 코루틴 시작");
    }

    IEnumerator CoStartSkill()
    {
        // 대기 시간
        _rangedSkill = false;
        State = CreatureState.Skill;
        yield return new WaitForSeconds(0.1f);
        AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        float length = 0;
        if (clipInfos.Length > 0)
        {
            length = clipInfos[0].clip.length / _animator.speed;
            Debug.Log($"Clip Name: {clipInfos[0].clip.name}, Length: {length}");
        }
        yield return new WaitForSeconds(length - 0.1f);
        State = CreatureState.Idle;
        _coSkill = null;
        Debug.Log("스킬 코루틴 종료");

        // TODO : TEMP
        CheckUpdatedFlag();
    }

    protected void MakeSkillDict()
    {
        var skillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(SkillBase)) && !t.IsAbstract);
        Dictionary<KeyCode, Data.SkillData> skills = DataManager.SkillDict[ObjInfo.CharType];

        foreach (var type in skillTypes)
        {
            // SkillBase를 상속받은 클래스들을 탐색해 생성
            // 클래스의 이름으로 SkillDict에서 SkillData을 검색해 데이터를 채워줌

            // 필요한 것 캐릭터 이름 스트링과 이 스킬이 어떤 번호를 갖는지.
            string className = type.Name;
            int idx = className.IndexOf('_');
            string charName = idx >= 0 ? className.Substring(0, idx) : className;
            if (charName != GetCharacterName())
                continue;

            string keyCode = className.Substring(idx + 1);
            if(!Enum.TryParse<KeyCode>(keyCode, out var result))
                Debug.Log($"KeyCode를 찾을 수 없음 : {keyCode}");

            SkillBase skill = (SkillBase)Activator.CreateInstance(type);

            skill.SkillData = skills[result];
            skill._player = this;
            skill._animator = this._animator;
            _skills.Add(type.Name, skill);
        }
    }

    public void PlayAnimFromServer(AnimInfo animInfo)
    {
        _animator.CrossFadeInFixedTime(animInfo.Name, animInfo.Ratio);
    }

    protected SkillBase FindSkill(KeyCode keyCode)
    {
        SkillBase skillBase = null;

        string skillName = GetCharacterName() + '_' + keyCode.ToString();
        if (!_skills.TryGetValue(skillName, out skillBase))
        {
            Debug.Log($"Skill을 찾을 수 없음 : {keyCode}");
            return null;
        }

        return skillBase;
    }

    protected SkillBase FindSkill(string keyCode)
    {
        SkillBase skillBase = null;

        string skillName = GetCharacterName() + '_' + keyCode;
        if (!_skills.TryGetValue(skillName, out skillBase))
        {
            Debug.Log($"Skill을 찾을 수 없음 : {keyCode}");
            return null;
        }

        return skillBase;
    }
    #endregion
}
