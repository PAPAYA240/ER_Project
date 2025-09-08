using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Define;

public class PlayerController : CreatureController
{
	protected Coroutine _coSkill;
    protected bool _rangedSkill = false;

    protected Dictionary<string, SkillBase> _skills = new Dictionary<string, SkillBase>();

    //Fog
    //private GameObject _fogOfWarVisionGo;
    private FogOfWarVision _fogOfWarVision;

    protected override void Init()
	{
		base.Init();
		MakeSkillDict();
        ObjectType = Define.Object.OtherPlayer;

        //Fog
        //_fogOfWarVisionGo = new GameObject("FogOfWar");
        //_fogOfWarVisionGo.transform.SetParent(gameObject.transform);
        //_fogOfWarVision = _fogOfWarVisionGo.GetOrAddComponent<FogOfWarVision>();
        //_fogOfWarVisionGo.layer = LayerMask.NameToLayer("Fog");

        _fogOfWarVision = gameObject.GetOrAddComponent<FogOfWarVision>();
        gameObject.layer = LayerMask.NameToLayer("Fog");
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

        if (_coSkill != null)
            StopCoroutine(_coSkill);
        _coSkill = StartCoroutine("CoStartSkill");
        Debug.Log("스킬 코루틴 시작");
    }

    IEnumerator CoStartSkill()
    {
        // 대기 시간
        _rangedSkill = false;
        State = CreatureState.Skill;
        float length = _animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);
        State = CreatureState.Idle;
        _coSkill = null;

        // TODO : TEMP
        _animator.SetTrigger("tIdle");
        CheckUpdatedFlag();
    }

    protected void MakeSkillDict()
    {
        var skillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(SkillBase)) && !t.IsAbstract);

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

            string KeyCode = className.Substring(idx + 1);
            int skillIdx = 0;
            if(KeyCode.Equals("T"))
            {
                skillIdx = 0;
            }
            else if(KeyCode.Equals("Q"))
            {
                skillIdx = 1;
            }
            else if(KeyCode.Equals("W"))
            {
                skillIdx = 2;
            }
            else if(KeyCode.Equals("E"))
            {
                skillIdx = 3;
            }
            else if(KeyCode.Equals("R"))
            {
                skillIdx = 4;
            }
            else
            {
                Debug.Log("Skill Index Error");
            }

            SkillBase skill = (SkillBase)Activator.CreateInstance(type);
            skill.SkillData = Managers.Data.GameData[charName].skills[skillIdx];
            skill._player = this;
            skill._animator = this._animator;
            _skills.Add(type.Name, skill);
        }
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

    #region Animation
    protected override void UpdateAnimation()
    {
        if (_animator == null)
            return;

        if (State == CreatureState.Idle)
        {
        }
        else if (State == CreatureState.Moving)
        {
        }
        else if (State == CreatureState.Skill)
        {
        }
        else
        {
        }
    }

    // 서버로부터 애니메이션 정보를 받아와 다른 플레이어의 애니메이션 재생용
    public virtual void PlayAnimation(AnimInfo animInfo)
    {
        switch (animInfo.Type)
        {
            case AnimType.Play:
                _animator.Play(animInfo.Hash, 0, Time.time - animInfo.Value);
                break;
            case AnimType.Trigger:
                _animator.SetTrigger(animInfo.Hash);
                break;
            case AnimType.Bool:
                _animator.SetBool(animInfo.Hash, animInfo.Value != 0f);
                break;
            case AnimType.Float:
                _animator.SetFloat(animInfo.Hash, animInfo.Value);
                break;
        }
    }
    #endregion
}
