using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class MyPlayerController : PlayerController
{
    bool _moveKeyPressed = false;
    Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
    class CoolTime
    {
        public bool isCoolDown;
        public float coolTime;
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
            PlayAnimation("WAIT", 0.1f);
        }
        else if (State == CreatureState.Moving)
        {
            PlayAnimation("RUN", 0.1f);
        }
        else if (State == CreatureState.Skill)
        {
            ExecuteSkill(KeyCode.Q);
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

        UpdateKeyInput();

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
        SendAnimPacket(animName, ratio);
    }

#region Packet
    private void SendSkillPacket(KeyCode key)
    {
        string skillName = Enum.GetName(typeof(Character), Managers.Object.Character) + '_' + key.ToString();
        C_Skill skillPacket = new C_Skill()
        {
            ObjectInfo = ObjInfo,
            SkillInfo = new SkillInfo() { KeyCode = key.ToString(), Name = skillName }
        };
        Managers.Network.Send(skillPacket);
    }

    private void SendAnimPacket(string name, float ratio)
    {
        C_Anim animPacket = new C_Anim() { AnimInfo = new AnimInfo() { Name = name, Ratio = ratio } };
        Managers.Network.Send(animPacket);
    }
#endregion

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

    protected void ExecuteSkill(KeyCode key)
    {
        if (!_coolDownDict[key].isCoolDown)
        {
            SkillBase skill = FindSkill(key);

            // 쿨타임 체크
            StartCoroutine(CoInputCooltime(key, skill.MaxCooldown));

            // 다른 조건 체크하기

            // 스킬 실행
            skill.Execute();

            // 패킷 보내기
            SendSkillPacket(key);

            // 스킬 실행 UI, TODO 스킬 사용할 수 있는 검증이 다 끝난 곳으로 옮겨야함
            _playerInterface.UseSkill(KeyToUIEnum(key));

            Debug.Log($"스킬 사용! : {key}");
        }
        else
        {
            Debug.Log($"스킬 쿨타임 적용 중! : {key} -> {GetCoolTime(key)} 초 남음");
        }
    }

#region UI
    private UI_PlayerInterface.GameObjects KeyToUIEnum(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Q:
                return UI_PlayerInterface.GameObjects.QSkill;
            case KeyCode.W:
                return UI_PlayerInterface.GameObjects.WSkill;
            case KeyCode.E:
                return UI_PlayerInterface.GameObjects.ESkill;
            case KeyCode.R:
                return UI_PlayerInterface.GameObjects.RSkill;
            case KeyCode.D:
                return UI_PlayerInterface.GameObjects.DSkill;
            case KeyCode.F:
                return UI_PlayerInterface.GameObjects.FSkill;
        }

        return UI_PlayerInterface.GameObjects.TSkill;
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

    private void UpdateSkillMaxCool()
    {
        // TODO 현재 스킬레벨에 따른 쿨타임과 아이템으로 인한 스킬 가속을 적용하여 UI에 반영
        // 일단 스킬 가속에 대한 계산이 어떻게 되는지 알아야하고, 스킬들이 레벨마다 어떤 쿨타임을 가질지 데이터(Json)를 만들어줘야함.

        //temp 나중에 스탯에서 가져오든가 해야될듯
        SkillBase QSkill = FindSkill(KeyCode.Q);
        SkillBase WSkill = FindSkill(KeyCode.W);
        SkillBase ESkill = FindSkill(KeyCode.E);
        SkillBase RSkill = FindSkill(KeyCode.R);

        float skillAcc = 0.0f;
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, CalculateMaxCool(QSkill.CurLevelCooldown, skillAcc));
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.WSkill, CalculateMaxCool(WSkill.CurLevelCooldown, skillAcc));
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.ESkill, CalculateMaxCool(ESkill.CurLevelCooldown, skillAcc));
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, CalculateMaxCool(RSkill.CurLevelCooldown, skillAcc));
    }


    private float CalculateMaxCool(float cooldown, float skillAcc)
    {
        // 최종 쿨타임 = 기본 쿨타임 × (100 / (100 + 스킬가속))
        return cooldown * (100f / (100f + skillAcc));
    }
    //private void OnCharSkillLevelUp(SkillEnum skill)
    //{
    //    //For QWERT
    //    skills[GetCharacterName() + "" + skill.ToString()].CurLevel += 1;

    //    float skillAcc = 0.0f;
    //    //float skillAcc = Stat.GetSkillAcc();

    //    switch (skill)
    //    {
    //        case SkillEnum.Q:
    //            SkillBase QSkill = FindSkill(KeyCode.Q);
    //            SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, CalculateMaxCool(QSkill.CurLevelCooldown, skillAcc));
    //            break;
    //        case SkillEnum.W:
    //            SkillBase WSkill = FindSkill(KeyCode.W);
    //            SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.WSkill, CalculateMaxCool(WSkill.CurLevelCooldown, skillAcc));
    //            break;
    //        case SkillEnum.E:
    //            SkillBase ESkill = FindSkill(KeyCode.E);
    //            SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.ESkill, CalculateMaxCool(ESkill.CurLevelCooldown, skillAcc));
    //            break;
    //        case SkillEnum.R:
    //            SkillBase RSkill = FindSkill(KeyCode.R);
    //            SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, CalculateMaxCool(RSkill.CurLevelCooldown, skillAcc));
    //            break;
    //    }

    //}
#endregion

}
