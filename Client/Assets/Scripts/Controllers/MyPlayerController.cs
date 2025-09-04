using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class MyPlayerController : PlayerController
{
    bool _moveKeyPressed = false;
    Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
    class CoolTime
    {
        public bool    isCoolDown;
        public float   coolTime;
    }

    int _mask = (1 << (int)Define.Layer.Map);
    Vector3 _dstPos = Vector3.zero;

    //UI
    //UI_PlayerHUD _playerHUD = null;
    UI_PlayerInterface _playerInterface = null;
    protected override void Init()
    {
        base.Init();
        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);

        ObjectType = Define.Object.MyPlayer;
        MakeCoolDownDict();

        //UI
        GameObject go = Managers.Resource.Instantiate("UI/Scene/PlayerHUD");
        go.transform.SetParent(gameObject.transform);
        _playerInterface = go.GetComponentInChildren<UI_PlayerInterface>();
    }

    // 매 틱 Update에서 호출됨
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

        UpdateKeyInput();

        base.UpdateController();
    }

    protected virtual void UpdateKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
        }
    }

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
        else if (State == CreatureState.Dead)
        {
        }
    }

    protected override void UpdateIdle()
    {
        if (_moveKeyPressed)
        {
            State = CreatureState.Moving;
            return;
        }       
    }

    protected override void UpdateMoving()
    {
        Vector3 moveDir = _dstPos - transform.position;
        moveDir.y = 0.0f;

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

    protected override void UpdateSkill()
    {
    }

    protected override void UpdateDead()
    {
    }

    // Camera
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

    #region Skill
    protected void ExecuteSkill(KeyCode key)
    {
        if (!_coolDownDict[key].isCoolDown)
        {
            SkillBase skill = FindSkill(key);

            // 쿨타임 체크
            StartCoroutine(CoInputCooltime(key, skill.SkillData.cooldown));

            // 다른 조건 체크하기

            // 스킬 실행
            skill.Execute();

            // 패킷 보내기
            SendSkillPacket(key);

            Debug.Log($"스킬 사용! : {key}");
        }
        else
        {
            Debug.Log($"스킬 쿨타임 적용 중! : {key} -> {GetCoolTime(key)} 초 남음");
        }
    }

    IEnumerator CoInputCooltime(KeyCode key, float time)
    {
        _coolDownDict[key].isCoolDown = true;

        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            _coolDownDict[key].coolTime = time - elapsed;
            yield return null;
        }

        _coolDownDict[key].isCoolDown = false;
        _coolDownDict[key].coolTime = 0.0f;
    }

    private void MakeCoolDownDict()
    {
        foreach (var skill in _skills)
        {
            string str = skill.Key.Substring(skill.Key.Length - 1);
            KeyCode key = (KeyCode)Enum.Parse(typeof(KeyCode), str);
            _coolDownDict[key] = new CoolTime { isCoolDown = false, coolTime = 0.0f };
        }
    }

    protected float GetCoolTime(KeyCode key)
    {
        return _coolDownDict[key].coolTime;
    }        
    #endregion

    #region Animation
    protected void PlayAnimation(string animName)
    {
        _animator.Play(animName);
        SendAnimPacket(animName, AnimType.Play, Time.time);
    }

    protected void TriggerAnimation(string triggerName)
    {
        _animator.SetTrigger(triggerName);
        SendAnimPacket(triggerName, AnimType.Trigger, 0f);
    }

    protected void SetBoolAnimation(string boolName, bool value)
    {
        _animator.SetBool(boolName, value);
        SendAnimPacket(boolName, AnimType.Bool, value == true ? 1f : 0f);
    }

    protected void SetFloatAnimation(string floatName, float value)
    {
        _animator.SetFloat(floatName, value);
        SendAnimPacket(floatName, AnimType.Float, value);
    }
    #endregion

    #region Packet
    private void SendSkillPacket(KeyCode key)
    {
        C_Skill skillPacket = new C_Skill() { Info = new SkillInfo() { KeyCode = (int)key } };
        Managers.Network.Send(skillPacket);
    }

    private void SendAnimPacket(string name, AnimType type, float value)
    {
        int hash = Animator.StringToHash(name);
        C_Anim animPacket = new C_Anim() { AnimInfo = new AnimInfo() { Hash = hash, Type = type, Value = value } };
        Managers.Network.Send(animPacket);       
    }
    #endregion
}
