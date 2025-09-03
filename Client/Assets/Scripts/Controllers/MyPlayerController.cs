using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class MyPlayerController : PlayerController
{
    bool _moveKeyPressed = false;
    Dictionary<string, CoolTime> _coolDownDict = new Dictionary<string, CoolTime>();
    class CoolTime
    {
        public bool    isCoolDown;
        public float   coolTime;
    }

    int _mask = (1 << (int)Define.Layer.Map);
    Vector3 _dstPos = Vector3.zero;

    protected override void Init()
    {
        base.Init();
        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);

        _object = Define.Object.MyPlayer;
        MakeCoolDownDict();
    }

    private void MakeCoolDownDict()
    {
        foreach(var skill in _skills)
        {
            _coolDownDict[skill.Key] = new CoolTime { isCoolDown  = false, coolTime = 0.0f };
        }
    }

    protected float GetCoolTime(string skillName)
    {
        return _coolDownDict[skillName].coolTime;
    }

    protected override void UpdateController()
    {
        switch (State)
        {
            case CreatureState.Idle:
                GetMouseInput();
                break;
            case CreatureState.Moving:
                GetMouseInput();
                break;
        }

        TempKeyInput();

        base.UpdateController();
    }

    protected override void UpdateIdle()
    {
        if (_moveKeyPressed)
        {
            State = CreatureState.Moving;
            return;
        }

        InputSkill();
    }

    protected override void UpdateSkill()
    {
        InputSkill();
    }

    void InputSkill()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ExecuteSkill("Rozzi_Q");
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {          
            ExecuteSkill("Rozzi_W");
        }

        // TEMP
        if(Input.GetKeyDown(KeyCode.D))
        {
            State = CreatureState.Dead;
        }
    }

    protected void ExecuteSkill(string skillName)
    {
        if (!_coolDownDict[skillName].isCoolDown)
        {
            SkillBase skill = FindSkill(skillName);
            
            // 쿨타임 체크
            StartCoroutine(CoInputCooltime(skillName, skill.SkillData.cooldown));

            // 다른 조건 체크하기

            // 스킬 실행
            skill.Execute();

            // 패킷 보내기
            SendSkillPacket(skillName);

            Debug.Log($"스킬 사용! : {skillName}");
        }
        else
        {
            Debug.Log($"스킬 쿨타임 적용 중! : {skillName} -> {GetCoolTime(skillName)} 초 남음");
        }
    }

    protected override void UpdateMoving()
    {
        Vector3 moveDir = _dstPos - transform.position;
        moveDir.y = 0.0f;

        // ���� ���� üũ
        float dist = moveDir.magnitude;
        if (dist < Speed * Time.deltaTime)
        {
            transform.position = _dstPos;
            State = CreatureState.Idle;
            _moveKeyPressed = false;
        }
        else
        {
            transform.position += moveDir.normalized * Speed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 20 * Time.deltaTime);
            State = CreatureState.Moving;
            CellPos = transform.position;
            RotInfo = transform.rotation;
            CheckUpdatedFlag();
        }
    }

    protected override void UpdateAnimation()
    {
        if (_animator == null)
            return;

        if (State == CreatureState.Idle)
        {
            //_animator.Play("WAIT");
            //_animator.SetTrigger();

            PlayAnimation("WAIT");
        }
        else if (State == CreatureState.Moving)
        {
            //_animator.Play("RUN");

            PlayAnimation("RUN");
        }
        else if (State == CreatureState.Skill)
        {

        }
        else if (State == CreatureState.Dead)
        {
            TriggerAnimation("tDeath");
        }
    }

    protected void PlayAnimation(string animName)
    {
        _animator.Play(animName);
        SendAnimPacket(animName, false);
    }

    protected void TriggerAnimation(string triggerName)
    {
        _animator.SetTrigger(triggerName);
        SendAnimPacket(triggerName, true);
    }

    // ��ų ��Ÿ��
    //Coroutine _coSkillCooltime;
    IEnumerator CoInputCooltime(string skillName, float time)
    {
        _coolDownDict[skillName].isCoolDown = true;

        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            _coolDownDict[skillName].coolTime = time - elapsed;
            yield return null;
        }

        _coolDownDict[skillName].isCoolDown = false;
        _coolDownDict[skillName].coolTime = 0.0f;
        //_coSkillCooltime = null;
    }

    [SerializeField]
    public Vector3 _offset = new Vector3(0, 10, -10);
    [SerializeField]
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        Vector3 targetPos = transform.position + _offset;
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetPos, smoothSpeed * Time.deltaTime);
        Camera.main.transform.LookAt(transform.position);
    }

    // ���콺 �Է�
    void GetMouseInput()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f, _mask);

        if (Input.GetMouseButton(1))
        {
            if (raycastHit)
            {
                _dstPos = hit.point;
                State = CreatureState.Moving;

                _moveKeyPressed = true;
            }
        }
    }

    void TempKeyInput()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f, _mask);

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_navMeshAgent == null)
                return;

            Vector3 targetPos = Managers.Map.CalcResultPos(transform.position, hit.point);
            _navMeshAgent.Warp(targetPos);
            _dstPos = targetPos;
            CellPos = transform.position;
            RotInfo = transform.rotation;
            CheckUpdatedFlag();
        }
    }

    protected override void CheckUpdatedFlag()
    {
        if (_updated)
        {
            C_Move movePacket = new C_Move();
            movePacket.PosInfo = PosInfo;
            movePacket.RotInfo = RotInfo;
            Managers.Network.Send(movePacket);
            _updated = false;
        }
    }

    private void SendSkillPacket(string skillName)
    {
        C_Skill skillPacket = new C_Skill() { Info = new SkillInfo() { Name = skillName } };
        Managers.Network.Send(skillPacket);
    }

    private void SendAnimPacket(string name, bool isTrigger = false)
    {
        if(!isTrigger)
        {
            int animHash = Animator.StringToHash(name);
            C_Anim animPacket = new C_Anim() { AnimInfo = new AnimInfo() { IsTrigger = false, Hash = animHash, Time = Time.time } };
            Managers.Network.Send(animPacket);
        }
        else
        {
            int triggerHash = Animator.StringToHash(name);
            C_Anim animPacket = new C_Anim() { AnimInfo = new AnimInfo() { IsTrigger = true, Hash = triggerHash } };
            Managers.Network.Send(animPacket);
        }        
    }
}
