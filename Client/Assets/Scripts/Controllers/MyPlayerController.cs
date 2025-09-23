using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Define;
using static UI_PlayerInterface;
using static UI_SkillBase;

public class MyPlayerController : PlayerController
{
    #region Variable
    protected bool _moveKeyPressed = false;
    protected int _monsterMask;
    protected int _playerMask; 

    // State
    public override CreatureState State
    {
        get { return PosInfo.State; }
        set
        {
            if (PosInfo.State == value)
                return;

            // Moving -> 다른 상태 : 길찾기 초기화
            if (_agent != null && _agent.isActiveAndEnabled &&
                State == CreatureState.Moving)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            // Attack -> 다른 상태 : Attack 모션 종료
            if (State == CreatureState.Attack)
            {
                _isAttackLoop = false;
                if(_coLookAtTarget != null)
                    StopCoroutine(_coLookAtTarget);

                if(value != CreatureState.Moving)
                    ResetTarget();
            }

            PosInfo.State = value;
            UpdateAnimation();
            _updated = true;
        }
    }
    protected bool _isStop = false;

    // State : Skill
    protected bool _isUseSkill = false;
    protected KeyCode _keyCode = KeyCode.None;
    protected Dictionary<string, SkillBase> _skills = new Dictionary<string, SkillBase>();
    Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
    class CoolTime
    {
        public bool isCoolDown;
        public float coolTime;
    }

    // State : Moving
    protected int _mask = (1 << (int)Define.Layer.Map);
    protected Vector3 _dstPos = Vector3.zero;
    protected float _minMoveDistance = 0.5f;
    protected float _rotSpeed = 8.0f;
    protected Coroutine _coLookAtTarget = null;

    // State : Attack
    protected bool _isAttackLoop = false;
    int _attackIndex = 0;
    protected Coroutine _attackRoutine;
    protected float _attackRange = 3.0f; // Temp
    protected GameObject _target;
    protected GameObject Target
    {
        get { return _target; }
        set
        {
            if (value == this.gameObject)
                return;

            if (State == CreatureState.Attack && _target != value &&
                value != null && _attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = StartCoroutine(CoAttackLoop());              
            }

            _target = value;
            if (_target != null)
                LookAtTarget(_target.transform.position, false, _rotSpeed);
        }
    }
    protected GameObjectType _targetType;
    protected Vector3 _finalPos;

    // State : Rest
    protected bool _isResting = false;
    protected Coroutine _coRest;

    //UI
    //UI_PlayerHUD _playerHUD = null;
    public UI_PlayerInterface PlayerInterface { get; protected set; }

    // Weapon
    public HashSet<int> VisibleObjectIds { get; set; } = new HashSet<int>();
    public WeaponInfo MyWeapon { get; set; } = new WeaponInfo();

    public float WeaponMasteryAS { get; set; }
    public float ItemAttackSpeed { get; set; } = 0;

    public float AttackSpeed
    {
        get
        {
            float baseSpeed = Stat.AttackSpeed + MyWeapon.AttackSpeed;
            float multiplier = 1 + WeaponMasteryAS + ItemAttackSpeed;
            return baseSpeed * multiplier;
        }
    }
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

        _monsterMask = 1 << LayerMask.NameToLayer("Monster");
        _playerMask = 1 << LayerMask.NameToLayer("Fog");

        // NavMesh Agent
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed;
        _agent.acceleration = 999;
        _agent.angularSpeed = 720;
        _agent.stoppingDistance = 0.1f;

        //UI
        GameObject go = Managers.Resource.Instantiate("UI/Scene/PlayerHUD");
        go.transform.SetParent(gameObject.transform);
        PlayerInterface = go.GetComponentInChildren<UI_PlayerInterface>();
        PlayerInterface.CharacterCode = CharTypeToCharCode(ObjInfo.CharType);
        PlayerInterface.CharacterName = Enum.GetName(typeof(CharacterType), ObjInfo.CharType);
        PlayerInterface.WeaponCode = CharTypeToWeaponCode(ObjInfo.CharType);
        PlayerInterface.Init();
        PlayerInterface.OnCharSkillLevelUpAction += OnCharSkillLevelUp;
        
        UI_Minimap minimap = GetComponentInChildren<UI_Minimap>();
        minimap.ActivatePlayerIcon(UI_MinimapCharIcon.IconType.MyPlayer, this);

        //쿨타임 설정
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, FindSkill(KeyCode.Q).MaxCooldown);

        //업데이트 함수들 호출
        Stat = Stat;

        _nameTag.GetComponentInChildren<UI_PlayerNameTag>().SetHPColor();
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
            case CreatureState.Skill:
                SkillBase currentSkill = FindSkill(_keyCode);
                if (currentSkill != null && currentSkill.SkillData.canMoveDuringCast == true)
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

        if(!_isStop)
        {
            Target = FindAttackablePlayer();
            TryChangeToAttackState();
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
                if (Target != null)
                {
                    transform.position = _finalPos;
                    if (!TryChangeToAttackState())
                    {
                        UpdateTargetPos();
                    }
                }
                else
                {
                    State = CreatureState.Idle;
                    _moveKeyPressed = false;
                }
            }
            // 이동 중
            else
            {
                State = CreatureState.Moving;
            }

            CellPos = transform.position;
            RotInfo = transform.rotation;
            CheckUpdatedFlag();
        }
    }

    protected override void UpdateAttack()
    {
        if(Target == null)
        {
            State = CreatureState.Idle;
            return;
        }

        LookAtTarget(Target.transform.position);
        UpdateTargetPos();
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
            if (!snapToTarget)
            {
                if(_coLookAtTarget != null)
                    StopCoroutine(_coLookAtTarget);
                _coLookAtTarget = StartCoroutine(CoLookAtTarget(lookDir, speed));
            }         
            else
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
                UpdateTransform();
            }
        }
    }

    protected IEnumerator CoLookAtTarget(Vector3 lookDir, float speed)
    {
        Quaternion targetRot = Quaternion.LookRotation(lookDir);

        while(true)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * speed);
            UpdateTransform();
            if (Quaternion.Angle(transform.rotation, targetRot) < 0.1f)
                break;

            yield return null;
        }
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
                if(State != CreatureState.Skill)
                    SetMovementState();
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

    // 타겟이 공격 가능한 사거리 내에 있는 지 검사
    // 없다면 Moving 상태로 변경 -> 따라감
    protected void UpdateTargetPos()
    {
        // TODO : Target이 공격 가능한 상태인지 체크
        if (Target == null)
        {
            State = CreatureState.Idle;
            _moveKeyPressed = false;
            return;
        }

        Vector3 targetPos = TryGetAttackPosition(Target);
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
        {
            if (Vector3.Distance(targetPos, transform.position) > 0.0f)
            {
                _agent.SetDestination(navHit.position);
                State = CreatureState.Moving;

                _moveKeyPressed = true;
            }
        }
    }

    protected GameObject TryGetAttackableObject()
    {
        GameObject gameObject = null;
        _targetType = GameObjectType.None;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float radius = 0.1f;
        if (Physics.SphereCast(ray, radius, out RaycastHit sphereHit, 1000.0f, _monsterMask | _playerMask))
        {
            gameObject = sphereHit.collider.gameObject;
            BaseController bc = gameObject.GetComponent<BaseController>();
            if (bc.Id != Id)
            {
                Target = gameObject;
                _targetType = ObjectManager.GetObjectTypeById(bc.ObjInfo.ObjectId);
            }
        }

        return gameObject;
    }

    // 사거리를 고려해 타겟을 공격 가능한 위치 반환
    protected Vector3 TryGetAttackPosition(GameObject go)
    {
        if (go == null)
            return Vector3.zero;

        Vector3 pos = go.transform.position;
        Vector3 dir = (pos - transform.position).normalized;

        float distance = Vector3.Distance(transform.position, pos);

        // TODO : 실제 사거리 가져와야함!
        // 이미 사거리 안이라면 제자리
        if (distance <= _attackRange)
            pos = transform.position;
        else
            pos = pos - dir * _attackRange * 0.9f;

        _finalPos = pos;

        return pos;
    }

    // 시야 범위 내 && 평타 사거리 내 가장 가까운 적 플레이어 반환
    protected GameObject FindAttackablePlayer()
    {
        GameObject targetObject = null;
        float minDistance = _attackRange + 0.1f;
        foreach (int num in VisibleObjectIds)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(num);
            if (type == GameObjectType.Player && num != Id)
            {
                GameObject go = Managers.Object.FindById(num);
                if (go != null)
                {
                    PlayerController pc = go.GetComponent<PlayerController>();
                    if (pc.ObjInfo.Team != ObjInfo.Team)
                    {
                        float distance = Vector3.Distance(go.transform.position, transform.position);
                        if (distance <= minDistance)
                        {
                            targetObject = go;
                            minDistance = distance;
                        }
                    }
                }
            }
        }

        Target = targetObject;

        return targetObject;
    }

    // 타겟이 있고, 평타 사거리 내라면 true 반환
    protected bool TryChangeToAttackState()
    {
        if (Target != null && Vector3.Distance(Target.transform.position, transform.position) <= _attackRange)
        {
            State = CreatureState.Attack;
            _isAttackLoop = true;
            return true;
        }

        return false;
    }

    protected void ResetTarget()
    {
        Target = null;
        _targetType = GameObjectType.None;
        _finalPos = Vector3.zero;
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
        // LeftCtrl + Q/W/E/R : 스킬 레벨업
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                PlayerInterface.SpecificSkillLevelUp(GameObjects.QSkill);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                PlayerInterface.SpecificSkillLevelUp(GameObjects.WSkill);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                PlayerInterface.SpecificSkillLevelUp(GameObjects.ESkill);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                PlayerInterface.SpecificSkillLevelUp(GameObjects.RSkill);
            }
        }
        // Q, W, E, R, T, D, F
        else
        {
            if(!_isResting)
                UpdateSkillKeyInput();
        }

        // S : 공격, 이동 중지
        if(Input.GetKeyDown(KeyCode.S))
        {
            if (State == CreatureState.Attack || State == CreatureState.Moving)
                State = CreatureState.Idle;

            _isStop = true;
        }
        // H : 이동 중지
        else if(Input.GetKeyDown(KeyCode.H))
        {
            if(State == CreatureState.Moving)
                State = CreatureState.Idle;
        }
        // X : 휴식
        // REST_START - REST_LOOP / REST_END
        // 처음 X를 눌렀고 Idle이나 Moving 상태였을 때 -> Rest 상태로 변경
        // 다시 X를 누르면 -> 휴식 종료
        else if (Input.GetKeyDown(KeyCode.X))
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
        // 마우스 우클릭이 눌렸을 경우 유효한 곳이 클릭 되었다면 해당 위치를 목적지로 설정 -> Moving 상태로 변경
        // 몬스터 클릭 시 평타 사거리만큼 떨어진 곳으로 설정

        // 그냥 우클릭 시 → 이동 처리
        if (Input.GetMouseButton(1))
        {
            _isStop = false;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Target = TryGetAttackableObject();
            Vector3 targetPos = TryGetAttackPosition(Target);

            // 타겟을 못 찾았을 때 -> 지형 클릭
            if (Target == null)
            {
                int mapMask = 1 << LayerMask.NameToLayer("Map");
                if (Physics.Raycast(ray, out RaycastHit rayHit, 1000.0f, mapMask))
                {
                    targetPos = rayHit.point;
                }
            }

            // 최종 목적지 설정
            // 플레이어와 너무 가까운 곳을 클릭하면 이동하지 않음
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                if(Target == null)
                {
                    float distance = Vector3.Distance(transform.position, navHit.position);
                    if (distance >= _minMoveDistance)
                    {
                        _agent.SetDestination(navHit.position);
                        State = CreatureState.Moving;

                        _moveKeyPressed = true;
                    }
                }
                else
                {
                    if(Vector3.Distance(targetPos, transform.position) > 0.0f)
                    {
                        _agent.SetDestination(navHit.position);
                        State = CreatureState.Moving;

                        _moveKeyPressed = true;
                    }
                    else
                    {
                        TryChangeToAttackState();
                    }
                }
            }
        }
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

        // 스킬을 사용하고 있는 상태가 아닐 때 && 쿨타임이 끝났을 때
        if (_coolDownDict.ContainsKey(_keyCode))
        {
            if (State != CreatureState.Skill && !_coolDownDict[_keyCode].isCoolDown)
            {
                // 다른 조건 체크하기

                // 패킷 보내기
                SendSkillPacket(_keyCode);

                // 스킬 실행 UI, TODO 스킬 사용할 수 있는 검증이 다 끝난 곳으로 옮겨야함
                PlayerInterface.UseSkill(KeyToUIEnum(_keyCode));

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

    protected void SetSkillInput(KeyCode keyCode)
    {
        _isUseSkill = true;
        _keyCode = keyCode;
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
        PlayerInterface.SetSkillMaxCool(skillEnum, value);
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
            case SkillEnum.T:
                SkillBase TSkill = FindSkill(KeyCode.T);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, CalculateMaxCool(TSkill.CurLevelCooldown, skillAcc));
                break;
        }

    }

    protected override void UpdateHp()
    {
        base.UpdateHp();

        if (null == PlayerInterface)
            return;

        PlayerInterface.SetHp(Hp);
    }
    protected override void UpdateMaxHp()
    {
        base.UpdateMaxHp();

        if (null == PlayerInterface)
            return;

        PlayerInterface.SetMaxHp(MaxHp);
    }

    protected override void UpdateStamina()
    {
        base.UpdateStamina();

        if (null == PlayerInterface)
            return;

        PlayerInterface.SetStamina(Stamina);
    }  

    protected override void UpdateMaxStamina()
    {
        base.UpdateMaxStamina();

        if (null == PlayerInterface)
            return;

        PlayerInterface.SetMaxStamina(MaxStamina);
    }  

    public void UpdateLevel()
    {
        if (null == PlayerInterface)
            return;

        PlayerInterface.SetLevel(Stat.Level);
        SetNameTagLevel();
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
        if (!_animator.IsInTransition(0))
            return _animator.GetCurrentAnimatorStateInfo(0).length;
        else
            return _animator.GetNextAnimatorStateInfo(0).length;

        //AnimatorClipInfo[] clipInfos = _animator.GetCurrentAnimatorClipInfo(0);
        //if (clipInfos.Length > 0)
        //    return clipInfos[0].clip.length;

        //return 0.0f;
    }
    #endregion

    #region Packet
    private void SendSkillPacket(KeyCode key)
    {
        int targetId = -1;
        if (Target && _targetType == GameObjectType.Monster)
        {         
            MonsterController monster = Target.GetComponentInChildren<MonsterController>();
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
