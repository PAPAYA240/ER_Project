using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : CreatureController
{
    private System.Random _random = new System.Random();
    private S_Move _pendingMovePacket = null;

    public enum MonsterSkill
    {
        None = 0,
        Attack1 = 1,
        Attack2 = 2,
        Skill1 = 3,
        Skill2 = 4,
        Skill3 = 5
    }

    public MonsterSkill Skill { get;  set; } 

    protected override void Init()
	{
        Skill = MonsterSkill.Attack1;
        _object = Define.Object.Monster;
        _navMeshAgent = GetComponentInParent<NavMeshAgent>();
		base.Init();
    }

    protected override void UpdateController()
    {
        base.UpdateController();
    }

    // 보간에 필요한 변수들
    Vector3 _lastPos;
    Vector3 _currentPos;
    float _posRatio;

    Quaternion _lastRot;
    Quaternion _currentRot;
    float _rotRatio;

    // 서버에서 패킷을 받을 때 호출되는 함수
    public void OnRecvMovePacket(S_Move movePacket)
    {
        if (State == CreatureState.Skill || State == CreatureState.Idle)
        {
            _pendingMovePacket = movePacket;
            return;
        }

        _lastPos = transform.position;
        _currentPos = new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ);
        _navMeshAgent.SetDestination(new Vector3(movePacket.PosInfo.PosX, movePacket.PosInfo.PosY, movePacket.PosInfo.PosZ));
    }

    protected override void UpdateMoving()
    {
    }

    public override void OnDamaged()
	{
		//Managers.Object.Remove(Id);
		//Managers.Resource.Destroy(gameObject);
	}

    //bool isSkill = false;
    //public void SelectSkill() 
    //{
    //    if (isSkill)
    //        return;

    //    isSkill = true;
    //    Debug.Log($"SelectSkill : 스킬 쓰는 중");
    //    Array skillValues = System.Enum.GetValues(typeof(MonsterSkill));
    //    List<MonsterSkill> availableSkills = new List<MonsterSkill>();

    //    foreach (MonsterSkill skill in skillValues)
    //    {
    //        if (skill != MonsterSkill.None)
    //            availableSkills.Add(skill);
    //    }

    //    if (availableSkills.Count > 0)
    //    {
    //        int randomIndex = _random.Next(0, availableSkills.Count);
    //        MonsterSkill selectedSkill = availableSkills[randomIndex];

    //        Skill = selectedSkill;
    //    }
    //}

    public override void UseSkill(int skillId)
    {
        Skill = (MonsterSkill)skillId;
        State = CreatureState.Skill;
    }

    public bool isAnimEnd = false;

    public void OnSkillAnimationEnd()
    {
        isAnimEnd = true;
    }
}