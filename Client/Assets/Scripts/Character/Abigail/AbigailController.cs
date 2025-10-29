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
            if (Vector2.Distance(new Vector2(mousePos.x, mousePos.z), new Vector2(CellPos.x, CellPos.z)) > _warpRange)
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

    public override void OnSkillConfirmed(S_Skill skillPacket)
    {
        base.OnSkillConfirmed(skillPacket);

        if((KeyCode)skillPacket.SkillInfo.KeyCode == KeyCode.W)
        {
            LookAtMouse();
        }
        else if((KeyCode)skillPacket.SkillInfo.KeyCode == KeyCode.E)
        {
            transform.position = _warpPos;
            _agent.Warp(_warpPos);
            UpdateTransform(true);
            
            CreatureController cc = _skillTarget.GetComponentInChildren<CreatureController>();

            C_TargetingSkill targetingSkillPkt = new C_TargetingSkill();
            targetingSkillPkt.ObjectId = Id;
            targetingSkillPkt.KeyCode = skillPacket.SkillInfo.KeyCode;
            targetingSkillPkt.TargetId = cc.Id;
            Managers.Network.Send(targetingSkillPkt);
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

    #region SkillMesh
    public override void CreateSkillMesh(KeyCode keyCode, float chargeRatio, Vector3 mousePos = new Vector3(), bool bProjectile = false)
    {
        base.CreateSkillMesh(keyCode, chargeRatio, mousePos, bProjectile);

        if(keyCode == KeyCode.Q)
            base.CreateSkillMesh(KeyCode.F1, 0);
    }
    #endregion
}
