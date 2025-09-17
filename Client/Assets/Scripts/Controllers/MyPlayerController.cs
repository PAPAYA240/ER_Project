using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;
using static Define;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static UI_PlayerInterface;
using static UI_SkillBase;
using static UnityEngine.GraphicsBuffer;

public class MyPlayerController : PlayerController
{
    #region Variable
    protected bool _moveKeyPressed = false;

    protected bool _isUseSkill = false;

    bool _isAttackLoop = false;
    int _attackIndex = 0;
    Coroutine _attackRoutine;

    protected KeyCode _keyCode = KeyCode.None;

    protected Dictionary<string, SkillBase> _skills = new Dictionary<string, SkillBase>();

    Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
    class CoolTime
    {
        public bool isCoolDown;
        public float coolTime;
    }

    protected int _mask = (1 << (int)Define.Layer.Map);
    protected Vector3 _dstPos = Vector3.zero;

    public float AttackSpeed
    {
        get
        {
            float baseSpeed = Stat.AttackSpeed + MyWeapon.AttackSpeed;
            float multiplier = 1 + WeaponMasteryAS + ItemAttackSpeed;
            return baseSpeed * multiplier;
        }
    }

    //UI
    //UI_PlayerHUD _playerHUD = null;
    protected UI_PlayerInterface _playerInterface = null;

    public HashSet<int> VisibleObjectIds { get; set; } = new HashSet<int>();
    public WeaponInfo MyWeapon { get; set; } = new WeaponInfo();

    public float WeaponMasteryAS { get; set; }
    public float ItemAttackSpeed { get; set; } = 0;

    // State : Moving
    protected bool _isTargetOn;
    protected GameObject _targetMonster;

    // State : Rest
    protected bool _isResting = false;
    protected Coroutine _coRest;

    // TEMP
    protected float _attackRange;
    #endregion

    #region Init
    void Start()
    {
    }

    public void ManualInit()
    {
        Init();
    }

    protected override void Init()
    {
        base.Init();

        layerName = _animator.GetLayerName(0);
        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);

        ObjectType = Define.Object.MyPlayer;
        MakeSkillDict();
        MakeCoolDownDict();

        // NavMesh Agent
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed;
        _agent.acceleration = 999;
        _agent.angularSpeed = 720;
        _agent.stoppingDistance = 0.1f;

        _attackRange = 3.0f;

        //UI
        GameObject go = Managers.Resource.Instantiate("UI/Scene/PlayerHUD");
        go.transform.SetParent(gameObject.transform);
        _playerInterface = go.GetComponentInChildren<UI_PlayerInterface>();
        _playerInterface.CharacterCode = CharTypeToCharCode(ObjInfo.CharType);
        _playerInterface.CharacterName = Enum.GetName(typeof(CharacterType), ObjInfo.CharType);
        _playerInterface.WeaponCode = CharTypeToWeaponCode(ObjInfo.CharType);
        _playerInterface.Init();
        _playerInterface.OnCharSkillLevelUpAction += OnCharSkillLevelUp;

        
        UI_Minimap minimap = GetComponentInChildren<UI_Minimap>();
        minimap.ActivatePlayerIcon(UI_MinimapCharIcon.IconType.MyPlayer, this);

        //레벨업이벤트를 여기서 바인딩 해주고 싶어
        //쉬운 방법 겟 오브젝트를 퍼블릭으로 연다.
        // 바인드 해주는 함수를 하나 만든다. 근데 하나가 아닐지도 모름.
        //_playerInterface.Get

        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, FindSkill(KeyCode.Q).MaxCooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, FindSkill(KeyCode.Q).SkillData.levels[0].cooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.WSkill, FindSkill(KeyCode.W).SkillData.levels[0].cooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.ESkill, FindSkill(KeyCode.E).SkillData.levels[0].cooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, FindSkill(KeyCode.R).SkillData.levels[0].cooldown);
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.DSkill, );
        //SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.FSkill, );
    }
    #endregion

    #region Update Animation & Controller
    protected override void UpdateAnimation()
    {
        // 상태가 전환되면 한 번만 호출됨

        if (_animator == null)
            return;

        if (State == CreatureState.Idle)
            PlayAnimation("WAIT", 0.1f);
        else if (State == CreatureState.Moving)
            PlayAnimation("RUN", 0.1f);
        else if (State == CreatureState.Attack)
        {
            Debug.Log($"평타 코루틴 시작");
            _attackRoutine = StartCoroutine(CoAttackLoop());
        }
        else if (State == CreatureState.Rest)
        {
            PlayAnimation("REST_START", 0.1f);
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
            case CreatureState.Attack:
                GetMouseInput();
                break;
        }

        UpdateKeyInput();

        if (_isUseSkill)
            ExecuteSkill();

        base.UpdateController();
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
    #endregion

    #region State
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
        // 목적지까지 실제로 움직임
        // 목적지까지 도착했으면 Moving 상태 종료 -> Idle

        if (_agent == null)
            return;

        if (!_agent.pathPending)
        {
            // 목적지 도착
            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                State = CreatureState.Idle;
                _moveKeyPressed = false;

                CellPos = transform.position;
                RotInfo = transform.rotation;
                CheckUpdatedFlag();
            }
            // 이동 중
            else
            {
                State = CreatureState.Moving;
                CellPos = transform.position;
                RotInfo = transform.rotation;
                CheckUpdatedFlag();
            }

            if (_isTargetOn)
                LookAtTargetMonster();
        }
    }

    protected override void UpdateRest()
    {
        // TODO : 쉬는 동안 자원 회복
    }

    protected override void UpdateDead()
    {
    }
    #endregion

    #region State : Moving
    protected void LookAtTarget(Vector3 targetPos, bool snapToTarget = false, float speed = 10.0f)
    {
        // 타겟을 바라보도록 방향 조정
        // snapToTarget : Target을 바로 바라볼지
        Vector3 lookDir = (targetPos - transform.position).normalized;
        lookDir.y = 0f;

        if (lookDir != Vector3.zero)
        {
            if(!snapToTarget)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * speed);
            else
                transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    protected void LookAtTargetMonster(bool snapToTarget = false, float speed = 10.0f)
    {
        // 타겟을 바라보도록 방향 조정
        LookAtTarget(_targetMonster.transform.position, snapToTarget, speed);
    }

    protected Vector3 GetReachablePosition(Vector3 startPos, Vector3 targetPos, out NavMeshHit navHit)
    {
        if (NavMesh.Raycast(startPos, targetPos, out NavMeshHit rayHit, NavMesh.AllAreas))
        {
            targetPos = rayHit.position;
        }

        // 최종 목적지 설정
        if (NavMesh.SamplePosition(targetPos, out navHit, 1.0f, NavMesh.AllAreas))
        {
            return navHit.position;
        }

        return Vector3.zero;
    }
    
    protected Vector3 GetTargetPos(float range, bool isMaxDistance = true)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f);

        Vector3 dir = (hit.point - transform.position).normalized;

        if(!isMaxDistance && (hit.point - transform.position).magnitude < range)
        {
            return hit.point;
        }

        return transform.position + dir * range;
    }
    #endregion

    #region State : Attack
    // 평타 반복 코루틴
    IEnumerator CoAttackLoop()
    {
        while (true)
        {
            string animName = (_attackIndex == 0) ? "ATTACK_1" : "ATTACK_2";
            PlayAnimation(animName, 0.1f);

            _attackIndex = 1 - _attackIndex;

            yield return null;

            yield return new WaitUntil(() =>
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.IsName(animName) && stateInfo.normalizedTime >= 0.9f)
                    return true;

                if (!stateInfo.IsName(animName) && !_isAttackLoop)
                    return true;

                return false;
            });

            if (!_isAttackLoop)
            {
                State = CreatureState.Idle;
                _attackRoutine = null;
                Debug.Log($"평타 코루틴 끝");

                StartCoroutine(CoComboResetTimer());

                yield break;
            }
        }
    }

    // 평타 콤보 초기화 코루틴
    private IEnumerator CoComboResetTimer()
    {
        float timer = 0f;

        while (timer < 2f)
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(1))
                yield break;

            timer += Time.deltaTime;
            yield return null;
        }

        _attackIndex = 0;
        Debug.Log("콤보 리셋!");
    }
    #endregion

    #region State : Rest
    protected void ExitRest()
    {
        // 휴식 종료 애니메이션 재생
        // 종료 시점을 체크

        PlayAnimation("REST_END", 0.1f);
        _coRest = StartCoroutine(CoRestEnd());
    }

    IEnumerator CoRestEnd()
    {
        // 애니메이션 종료 시점을 체크해서 Idle or Moving 상태로 전환

        yield return new WaitForSeconds(0.1f);

        float elapsed = 0f;

        AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfos.Length > 0)
        {
            float length = clipInfos[0].clip.length;
            while (elapsed < length - 0.1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        _isResting = false;

        SetMovementState();
    }
    #endregion

    #region Input
    protected virtual void UpdateKeyInput()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                _playerInterface.SpecificSkillLevelUp(GameObjects.QSkill);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                _playerInterface.SpecificSkillLevelUp(GameObjects.WSkill);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                _playerInterface.SpecificSkillLevelUp(GameObjects.ESkill);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                _playerInterface.SpecificSkillLevelUp(GameObjects.RSkill);
            }
        }
        else
        {
            UpdateSkillKeyInput();
        }

        // 처음 X를 눌렀고 Idle이나 Moving 상태였을 때 -> Rest 상태로 변경
        // 다시 X를 누르면 -> 휴식 종료
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!_isResting && (State == CreatureState.Idle || State == CreatureState.Moving))
            {
                State = CreatureState.Rest;
                _isResting = true;
            }
            else if (_isResting)
            {
                ExitRest();
            }
        }
    }

    protected virtual void UpdateSkillKeyInput() { }

    protected virtual void GetMouseInput()
    {
        // Shift + 우클릭 -> 평타 애니메이션
        // 마우스 우클릭이 눌렸을 경우 유효한 곳이 클릭 되었다면 해당 위치를 목적지로 설정 -> Moving 상태로 변경
        // 몬스터 클릭 시 평타 사거리만큼 떨어진 곳으로 설정

        // Shift 누르고 우클릭 시 → 평타 애니메이션
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetMouseButton(1))
            {
                if (_attackRoutine == null)
                {
                    _isAttackLoop = true;
                    State = CreatureState.Attack;
                }
            }

        }
        // 그냥 우클릭 시 → 이동 처리
        else if (Input.GetMouseButton(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 targetPos = FindMonster();

            // 몬스터를 못 찾았을 때 -> 지형 클릭
            if (targetPos == Vector3.zero)
            {
                int mapMask = 1 << LayerMask.NameToLayer("Map");
                if (Physics.Raycast(ray, out RaycastHit rayHit, 1000.0f, mapMask))
                {
                    _isTargetOn = false;
                    _targetMonster = null;

                    targetPos = rayHit.point;
                }
            }
          
            // 최종 목적지 설정
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                _agent.SetDestination(navHit.position);
                State = CreatureState.Moving;

                _moveKeyPressed = true;
            }
        }
        else
        {
            _isAttackLoop = false;
        }
    }

    protected Vector3 FindMonster()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Vector3 targetPos = Vector3.zero;
        float radius = 0.02f;
        int monsterMask = 1 << LayerMask.NameToLayer("Monster");
        if (Physics.SphereCast(ray, radius, out RaycastHit sphereHit, 1000.0f, monsterMask))
        {
            _isTargetOn = true;
            _targetMonster = sphereHit.collider.gameObject;

            Vector3 monsterPos = _targetMonster.transform.position;
            Vector3 dir = (monsterPos - transform.position).normalized;

            float distance = Vector3.Distance(transform.position, monsterPos);

            // TODO : 실제 사거리 가져와야함!
            // 이미 사거리 안이라면 제자리
            if (distance <= _attackRange)
                targetPos = transform.position;
            else
                targetPos = monsterPos - dir * _attackRange;

            return targetPos;
        }

        return Vector3.zero;
    }
    #endregion

    #region Animation
    protected override void PlayAnimation(string animName, float ratio)
    {
        int layerIndex = _animator.GetLayerIndex(layerName);
        if (layerIndex == -1)
            return;

        _animator.CrossFadeInFixedTime(animName, ratio);
        SendAnimPacket(animName, ratio);
    }

    #endregion

    #region Skill
    protected void ExecuteSkill()
    {
        _isUseSkill = false;
        if (_coolDownDict.ContainsKey(_keyCode))
        {
            if (State != CreatureState.Skill && !_coolDownDict[_keyCode].isCoolDown)
            {
                // 다른 조건 체크하기

                // 패킷 보내기
                SendSkillPacket(_keyCode);

                // 스킬 실행 UI, TODO 스킬 사용할 수 있는 검증이 다 끝난 곳으로 옮겨야함
                _playerInterface.UseSkill(KeyToUIEnum(_keyCode));

                Debug.Log($"스킬 사용! : {_keyCode}");
            }
        }
    }

    protected SkillBase FindSkill(KeyCode keyCode)
    {
        SkillBase skillBase = null;

        if (!_skills.TryGetValue(keyCode.ToString(), out skillBase))
        {
            Debug.Log($"Skill을 찾을 수 없음 : {keyCode}");
            return null;
        }

        return skillBase;
    }

    protected SkillBase FindSkill(string keyCode)
    {
        SkillBase skillBase = null;

        if (!_skills.TryGetValue(keyCode, out skillBase))
        {
            Debug.Log($"Skill을 찾을 수 없음 : {keyCode}");
            return null;
        }

        return skillBase;
    }

    protected float GetCoolTime(KeyCode key)
    {
        return _coolDownDict[key].coolTime;
    }

    public void StartCoCoolTime(KeyCode key)
    {
        SkillBase skill = FindSkill(key);

        // 쿨타임 체크
        StartCoroutine(CoInputCooltime(key, skill.CurLevelCooldown));
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
        Debug.Log("쿨타임 끝");
    }

    protected void MakeSkillDict()
    {
        Dictionary<KeyCode, Data.SkillData> skills = DataManager.SkillDict[ObjInfo.CharType];

        // Q, W, E, R
        foreach(Key key in Enum.GetValues(typeof(Key)))
        {
            SkillBase skill = new SkillBase();

            string keyCode = key.ToString();
            if (!Enum.TryParse<KeyCode>(keyCode, out var result))
                Debug.Log($"KeyCode를 찾을 수 없음 : {keyCode}");

            skill.SkillData = skills[result];
            _skills.Add(keyCode, skill);
        }
    }

    private void MakeCoolDownDict()
    {
        foreach (var skill in _skills)
        {
            KeyCode key = (KeyCode)Enum.Parse(typeof(KeyCode), skill.Key);
            _coolDownDict[key] = new CoolTime { isCoolDown = false, coolTime = 0.0f };
        }
    }
    #endregion

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

    protected void OnCharSkillLevelUp(SkillEnum skill)
    {
        //For QWERT
        _skills[skill.ToString()].CurLevel += 1;

        float skillAcc = 0.0f;
        //float skillAcc = Stat.GetSkillAcc();

        switch (skill)
        {
            case SkillEnum.Q:
                SkillBase QSkill = FindSkill(KeyCode.Q);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, CalculateMaxCool(QSkill.CurLevelCooldown, skillAcc));
                break;
            case SkillEnum.W:
                SkillBase WSkill = FindSkill(KeyCode.W);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.WSkill, CalculateMaxCool(WSkill.CurLevelCooldown, skillAcc));
                break;
            case SkillEnum.E:
                SkillBase ESkill = FindSkill(KeyCode.E);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.ESkill, CalculateMaxCool(ESkill.CurLevelCooldown, skillAcc));
                break;
            case SkillEnum.R:
                SkillBase RSkill = FindSkill(KeyCode.R);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, CalculateMaxCool(RSkill.CurLevelCooldown, skillAcc));
                break;
        }

    }

    #endregion

    #region Camera
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
    #endregion

    #region Util
    protected void UpdateTransform()
    {
        CellPos = transform.position;
        RotInfo = transform.rotation;
        CheckUpdatedFlag();
    }

    protected void SetMovementState()
    {
        if (_moveKeyPressed)
            State = CreatureState.Moving;
        else
            State = CreatureState.Idle;
    }

    protected float GetCurrentAnimClipLength()
    {
        AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfos.Length > 0)
            return clipInfos[0].clip.length;

        return 0.0f;
    }
    #endregion

    #region Packet
    private void SendSkillPacket(KeyCode key)
    {
        int targetId = -1;
        if (_targetMonster)
        {
            MonsterController monster = _targetMonster.GetComponentInChildren<MonsterController>();
            if (monster)
            {
                targetId = monster.ObjInfo.ObjectId;
            }
        }
        C_Skill skillPacket = new C_Skill()
        {
            ObjectInfo = ObjInfo,
            SkillInfo = new SkillInfo() { KeyCode = (int)key },
            TargetId = targetId
        };
        Managers.Network.Send(skillPacket);
        Debug.Log("스킬 패킷 보내기");
    }

    protected void SendFXPacket(KeyCode key)
    {
        C_Fx fxPacket = new C_Fx();

        fxPacket.FxInfo = new EffectInfo() { KeyCode = (int)_keyCode };

        Managers.Network.Send(fxPacket);
    }

    private void SendAnimPacket(string name, float ratio)
    {
        C_Anim animPacket = new C_Anim() { AnimInfo = new AnimInfo() { Name = name, Ratio = ratio } };
        Managers.Network.Send(animPacket);
    }
    #endregion
}
