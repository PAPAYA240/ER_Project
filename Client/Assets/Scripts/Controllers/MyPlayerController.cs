using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class MyPlayerController : PlayerController
{
    bool _moveKeyPressed = false;

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

        //UI
        GameObject go = Managers.Resource.Instantiate("UI/Scene/PlayerHUD");
        go.transform.SetParent(gameObject.transform);
        _playerInterface = go.GetComponentInChildren<UI_PlayerInterface>();
        _playerInterface.CharacterCode = CharTypeToCharCode(ObjInfo.CharType);
        _playerInterface.CharacterName = Enum.GetName(typeof(CharacterType), ObjInfo.CharType);
        _playerInterface.WeaponCode = CharTypeToWeaponCode(ObjInfo.CharType);
        _playerInterface.Init();
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, FindSkill(KeyCode.Q).SkillData.cooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.WSkill, FindSkill(KeyCode.W).SkillData.cooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.ESkill, FindSkill(KeyCode.E).SkillData.cooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, FindSkill(KeyCode.R).SkillData.cooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.DSkill, _coolDownDict[KeyCode.D].coolTime);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.FSkill, _coolDownDict[KeyCode.F].coolTime);
    }

    protected override void UpdateAnimation()
    {
        if (_animator == null)
            return;

        if (State == CreatureState.Idle)
        {
            _animator.CrossFadeInFixedTime("WAIT", 0.1f);
        }
        else if (State == CreatureState.Moving)
        {
            _animator.CrossFadeInFixedTime("RUN", 0.1f);
        }
        else if (State == CreatureState.Skill)
        {

        }
        else
        {

        }
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

        base.UpdateController();
    }

    protected override void UpdateIdle()
    {
        // 이동 상태로 갈지 확인
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

    Coroutine _coSkillCooltime;
    IEnumerator ConInputCooltime(float time)
    {
        yield return new WaitForSeconds(time);
        _coSkillCooltime = null;
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

    // 키보드 입력
    protected virtual void UpdateKeyInput()
    {
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.R))
        {
            State = CreatureState.Skill;
        }
        else if (Input.GetKey(KeyCode.D))
        {

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

    protected void PlayAnimation(string animName, float ratio)
    {
        _animator.CrossFadeInFixedTime(animName, ratio);
        SendAnimPacket(animName, Time.time);
    }

    private void SendAnimPacket(string name, float ratio)
    {
        C_Anim animPacket = new C_Anim() { AnimInfo = new AnimInfo() { Name = name, Ratio = ratio } };
        Managers.Network.Send(animPacket);
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

    private string CharTypeToCharCode(CharacterType type)
    {
        string result = "";

        switch (type)
        {
            case CharacterType.Rozzi:
                result = "021";
                break;
            case CharacterType.Yuki:
                result = "011";
                break;
            case CharacterType.Hyunwoo:
                result = "007";
                break;
            case CharacterType.Abigail:
                result = "067";
                break;
            case CharacterType.Theodore:
                result = "062";
                break;
        }

        return result;
    }

    private string CharTypeToWeaponCode(CharacterType type)
    {
        string result = "";

        switch (type)
        {
            case CharacterType.Rozzi:
                result = "051";
                break;
            case CharacterType.Yuki:
                result = "021";
                break;
            case CharacterType.Hyunwoo:
                result = "081";
                break;
            case CharacterType.Abigail:
                result = "031";
                break;
            case CharacterType.Theodore:
                result = "071";
                break;
        }

        return result;
    }

    private void SetMaxCoolDownUI(UI_PlayerInterface.GameObjects skillEnum, float value)
    {
        _playerInterface.SetSkillMaxCool(skillEnum, value);
    }


}
