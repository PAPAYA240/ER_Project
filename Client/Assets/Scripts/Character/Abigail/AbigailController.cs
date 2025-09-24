using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using ServerCore;
using UnityEngine;

public class AbigailController : MyPlayerController
{
    // E
    float _warpRange = 6.2f;
    float _warpRadius = 1.2f;
    GameObject _skillTarget = null;
    Vector3 _warpPos = Vector3.zero;

    protected override void UpdateSkillKeyInput()
    {
        if (IsKeyInput == false && Input.GetKeyDown(KeyCode.Q))
        {
            SetSkillInput(KeyCode.Q);
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.W))
        {
            SetSkillInput(KeyCode.W);
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.E))
        {
            Vector3 mousePos = GetCursorPos();
            if (Vector3.Distance(mousePos, CellPos) > _warpRange)
                return;

            GameObject target = TryGetAttackableObject(_warpRadius);
            if (target == null)
                return;

            Vector3 pos = transform.position;
            pos.y = 0;
            if (Vector3.Distance(mousePos, pos) <= _warpRange)
            {
                _skillTarget = target;
                SetSkillInput(KeyCode.E);
                _warpPos = mousePos;
                CreatureController cc = _skillTarget.GetComponent<CreatureController>();
                SkillTargetId = cc.Id;
            }
        }
        else if (IsKeyInput == false && Input.GetKeyDown(KeyCode.R))
        {
            SetSkillInput(KeyCode.R);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {

        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            //Skill_F();
        }
    }

    protected override void UpdateSkill()
    {

    }

    #region Skill

    public override void OnSkillConfirmed(SkillInfo skillInfo)
    {
        base.OnSkillConfirmed(skillInfo);

        if((KeyCode)skillInfo.KeyCode == KeyCode.E)
        {
            transform.position = _warpPos;
            _agent.Warp(_warpPos);
            UpdateTransform(true);

            C_AttackSkillTarget atkSkillTargetPkt = new C_AttackSkillTarget();
            Managers.Network.Send(atkSkillTargetPkt);
        }
    }

    protected override void Skill_Q()
    {
        PlayAnimation("SKILL_Q", 0.1f);
    }

    protected override void Skill_W()
    {
        PlayAnimation("SKILL_W", 0.1f);
    }

    protected override void Skill_E()
    {
        PlayAnimation("SKILL_E", 0.1f);
    }

    protected override void Skill_R()
    {
        PlayAnimation("SKILL_R", 0.1f);
    }
    #endregion
}
