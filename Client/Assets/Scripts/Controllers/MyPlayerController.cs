using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using static Data.SkillEffectList;
using static UI_PlayerInterface;
using static UI_SkillBase;
using static UnityEngine.GraphicsBuffer;

public class MyPlayerController : PlayerController
{
    #region Variable
    protected bool _moveKeyPressed = false;
    protected int _monsterMask;
    protected int _playerMask;
    protected int _myPlayerMask;
    const int _maxInventorySlot = 10;
    // State
    public override CreatureState State
    {
        get { return PosInfo.State; }
        set
        {
            if (PosInfo.State == value)
                return;

            // TEMP
            Debug.Log($"Change State - Cur : {PosInfo.State}, Next : {value}");

            // Moving -> 다른 상태 : 길찾기 초기화
            if (_agent != null && _agent.isActiveAndEnabled &&
                State == CreatureState.Moving)
            {
                _agent.isStopped = true;
                _agent.ResetPath();

                // Moving -> Attack 이 아니라면 Target 초기화
                if (value != CreatureState.Attack)
                    ResetTarget();
            }

            // Attack -> 다른 상태 : Attack 모션 종료
            if (State == CreatureState.Attack)
            {
                _isAttackLoop = false;
                if (_coLookAtTarget != null)
                    StopCoroutine(_coLookAtTarget);

                if (value != CreatureState.Moving)
                    ResetTarget();
            }

            // Dead -> 다른 상태 : agent 활성화
            if (State == CreatureState.Dead)
                _agent.enabled = true;

            PosInfo.State = value;
            UpdateAnimation();
            _updated = true;
            SendStatePacket();
        }
    }
    protected bool _isStop = true;

    // State : Skill
    protected bool _isUseSkill = false;
    protected KeyCode _keyCode = KeyCode.None;
    protected Dictionary<KeyCode, SkillBase> _skills = new Dictionary<KeyCode, SkillBase>();
    Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
   

    class CoolTime
    {
        public bool isCoolDown;
        public float coolTime;
    }

    // State : Moving
    protected int _mask = (1 << (int)Define.Layer.Map);
    protected float _minMoveDistance = 0.5f;
    protected float _rotSpeed = 8.0f;
    protected Coroutine _coLookAtTarget = null;
    protected bool _isWarp = false;

    // State : Attack
    protected bool _isAttackLoop = false;
    int _attackIndex = 0;
    protected Coroutine _attackRoutine;
    protected float _attackRange = 3.0f; // Temp
    protected GameObject _target;
    protected GameObject _nextTarget = null;

    
    protected GameObject Target
    {
        get { return _target; }
        set
        {
            if (value == this.gameObject)
                return;

            // Attack 중 다른 대상을 타겟팅 했을 때 : 현재 재생 중인 모션 종료 후 타겟 변경
            if(State == CreatureState.Attack && value != null &&
                _target != null && _target != value)
            {
                _nextTarget = value;
            }
            else
            {
                _target = value;
            }
        }
    }
    protected GameObjectType _targetType;
    protected Vector3 _finalPos;

    protected List<int> SkillTargetId { get; set; }

    protected float _ratioSkillDuration = 0f;

    // State : Rest
    protected bool _isResting = false;
    protected Coroutine _coRest;

    //UI
    UI_PlayerHUD _playerHUD;
    public UI_PlayerInterface PlayerInterface { get; protected set; }

    // Inventory
    List<ItemInfoBase> _inventory = new List<ItemInfoBase>();

    // Weapon
    public HashSet<int> VisibleObjectIds { get; set; } = new HashSet<int>();
    public WeaponInfo MyWeapon { get; set; } = new WeaponInfo();

    public float WeaponMasteryAS { get; set; }
    public float ItemAttackSpeed { get; set; } = 0;

    // Cursor
    Texture2D _cursorAttack;
    Texture2D _cursorDefault;

    bool _isAttackGround = false;

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

    protected override void Init()
    {
        base.Init();

        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);

        _cursorDefault = Managers.Resource.Load<Texture2D>("Cursor/Cursor_01");
        _cursorAttack = Managers.Resource.Load<Texture2D>("Cursor/Pointer_01");

        layerName = _animator.GetLayerName(0);
        //Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);

        ObjectType = Define.Object.MyPlayer;
        MakeSkillDict();
        MakeCoolDownDict();
        MakeInventory();

        _monsterMask = 1 << LayerMask.NameToLayer("Monster");
        _playerMask = 1 << LayerMask.NameToLayer("Fog");
        _myPlayerMask = 1 << LayerMask.NameToLayer("MyPlayer");
        gameObject.layer = LayerMask.NameToLayer("MyPlayer");

        // NavMesh Agent
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = Speed;
        _agent.acceleration = 999;
        _agent.angularSpeed = 720;
        _agent.stoppingDistance = 0.1f;

        //UI
        GameObject go = Managers.Resource.Instantiate("UI/Scene/PlayerHUD");
        go.transform.SetParent(gameObject.transform);
        _playerHUD = go.GetComponent<UI_PlayerHUD>();
        _playerHUD.Init();
        PlayerInterface = go.GetComponentInChildren<UI_PlayerInterface>();
        PlayerInterface.CharacterCode = CharTypeToCharCode(ObjInfo.Player.CharType);
        PlayerInterface.CharacterName = Enum.GetName(typeof(CharacterType), ObjInfo.Player.CharType);
        PlayerInterface.WeaponCode = CharTypeToWeaponCode(ObjInfo.Player.CharType);
        PlayerInterface.Init();
        PlayerInterface.OnCharSkillLevelUpAction += OnCharSkillLevelUp;
        
        UI_Minimap minimap = GetComponentInChildren<UI_Minimap>();
        minimap.ActivatePlayerIcon(UI_MinimapCharIcon.IconType.MyPlayer, this);

        //쿨타임 설정
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, FindSkill(KeyCode.Q).MaxCooldown);

        //업데이트 함수들 호출
        Stat = Stat;
        _originSpeed = _agent.speed;

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
            _attackRoutine = StartCoroutine(CoAttackLoop());
        else if (State == CreatureState.Rest)
            PlayAnimation("REST_START", 0.1f);
        else if(State == CreatureState.Dead)
            PlayAnimation("DEAD", 0.1f);
    }

    protected override void UpdateController()
    {
        UpdateCool();

        if (State == CreatureState.Dead)
            return;

        switch (State)
        {
            case CreatureState.Idle:
                GetMouseInput(1);
                break;
            case CreatureState.Moving:
               GetMouseInput(1);
                break;
            case CreatureState.Attack:
                GetMouseInput(1);
                break;
            case CreatureState.Charging:
                GetMouseInputDuringCharging();
                UpdateTransform();
                break;
            case CreatureState.Skill:
                SkillBase currentSkill = FindSkill(_keyCode);
                if (currentSkill != null && currentSkill.SkillData.canMoveDuringCast == true)
                {
                    GetMouseInputDuringSkill();
                    UpdateTransform();
                }
                break;
        }

        UpdateKeyInput();

        if (_isUseSkill)
            ExecuteSkill();

        base.UpdateController();

        CheckUpdatedFlag();
    }

    protected override void CheckUpdatedFlag()
    {
        if (_updated)
        {
            C_Move movePacket = new C_Move();
            movePacket.PosInfo = PosInfo;
            movePacket.RotInfo = RotInfo;
            movePacket.IsWarp = _isWarp;
            Managers.Network.Send(movePacket);
            _updated = false;
            _isWarp = false;
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
            //Target = FindAttackablePlayer();
            //TryChangeToAttackState();
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

            UpdateTransform();
        }
    }

    protected override void UpdateAttack()
    {
        if(Target == null || !IsAttackable(Target))
        {
            State = CreatureState.Idle;
            return;
        }

        LookAtTarget(Target.transform.position);
        UpdateTargetPos();
    }

    protected override void UpdateCharging()
    {
        if (_agent == null)
            return;

        if (_agent.remainingDistance <= _agent.stoppingDistance)
        {
            if (_moveKeyPressed)
                PlayAnimation("CHARGING", 0.1f);

            _agent.speed = _originSpeed;
            _moveKeyPressed = false;
        }
        UpdateTransform();
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
    protected void LookAtTarget(Vector3 targetPos, bool snapToTarget = false, float speed = 20.0f)
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

            if(_nextTarget != null)
            {
                _target = _nextTarget;
                _nextTarget = null;
            }

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

    // 주변에 존재하는 공격 가능한 모든 오브젝트  반환
    protected List<GameObject> TryGetAttackableObjectList(float radius = 0.1f)
    {
        List<GameObject> hitList = new List<GameObject>();
        _targetType = GameObjectType.None;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit[] hits = Physics.SphereCastAll(ray, radius, 1000.0f, _monsterMask | _playerMask);

        if (hits.Length > 1)
            hits = hits.OrderBy(h => h.distance).ToArray();

        foreach (RaycastHit hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            CreatureController cc = hitObject.GetComponent<CreatureController>();

            if (IsAttackable(hitObject))
            {
                hitList.Add(hitObject);
                if (_targetType == GameObjectType.None)
                    _targetType = ObjectManager.GetObjectTypeById(cc.ObjInfo.ObjectId);
            }
        }
        return hitList;
    }

    protected GameObject TryGetAttackableObject(float radius = 0.1f)
    {
        GameObject gameObject = null;
        _targetType = GameObjectType.None;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.SphereCast(ray, radius, out RaycastHit sphereHit, 1000.0f, _monsterMask | _playerMask))
        {
            GameObject hitObject = sphereHit.collider.gameObject;
            CreatureController cc = hitObject.GetComponent<CreatureController>();
            if (IsAttackable(hitObject))
            {
                //Target = gameObject = hitObject;
                gameObject = hitObject;
                _targetType = ObjectManager.GetObjectTypeById(cc.ObjInfo.ObjectId);
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
        dir.y = 0f;

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
                    if (IsAttackable(go))
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
        _nextTarget = null;
        _targetType = GameObjectType.None;
        _finalPos = Vector3.zero;
    }

    protected void ChangeTarget(GameObject nextTarget)
    {

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

    #region State : Dead
    public override void OnDead()
    {
        base.OnDead();
        ResetCharacterState();
    }

    public override void OnRespawn(S_Respawn respawnPacket)
    {
        Vector3 pos = new Vector3
        {
            x = respawnPacket.PosInfo.PosX,
            y = respawnPacket.PosInfo.PosY,
            z = respawnPacket.PosInfo.PosZ
        };

        if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
        {
            pos = navHit.position;
        }

        _agent.Warp(pos);
        transform.position = pos;
        transform.rotation = new Quaternion
        {
            x = respawnPacket.RotInfo.Qx,
            y = respawnPacket.RotInfo.Qy,
            z = respawnPacket.RotInfo.Qz,
            w = respawnPacket.RotInfo.Qw
        };

        UpdateTransform(true);

        State = CreatureState.Idle;
        Hp = respawnPacket.Hp;
        Stamina = respawnPacket.Stamina;
    }

    #endregion

    #region Input
    protected virtual void UpdateKeyInput()
    {
        // LeftCtrl + Q/W/E/R/T : 스킬 레벨업
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                PlayerInterface.TrySkillLevelUp(KeyCode.Q);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                PlayerInterface.TrySkillLevelUp(KeyCode.W);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                PlayerInterface.TrySkillLevelUp(KeyCode.E);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                PlayerInterface.TrySkillLevelUp(KeyCode.R);
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                PlayerInterface.TrySkillLevelUp(KeyCode.T);
            }
        }
        // Q, W, E, R, T, D, F
        else
        {
            if (!_isResting)
                UpdateSkillKeyInput();
        }

        // S : 공격, 이동 중지
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (State == CreatureState.Attack || State == CreatureState.Moving)
                State = CreatureState.Idle;

            _isStop = true;
        }
        // H : 이동 중지
        else if (Input.GetKeyDown(KeyCode.H))
        {
            if (State == CreatureState.Moving)
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
        else if (Input.GetKeyDown(KeyCode.A))
        {
            Cursor.SetCursor(_cursorAttack, Vector2.zero, CursorMode.Auto);

            _isAttackGround = true;
        }
        else if (State == CreatureState.Rest && Input.GetMouseButtonDown(1))
        {
            ExitRest();
        }
        // TEMP : 데미지 테스트용!!
        else if(Input.GetKeyDown(KeyCode.P))
        {
            C_TestDamage packet = new C_TestDamage();
            packet.ObjectId = Id;
            Managers.Network.Send(packet);
        }

        if (_isAttackGround == true)
        {
            GetMouseInput(0);
        }
    }

    protected virtual void UpdateSkillKeyInput() { }

    protected bool _isInputLocked = false;
    protected virtual void GetMouseInput(int mouseButton)
    {
        // 마우스 우클릭이 눌렸을 경우 유효한 곳이 클릭 되었다면 해당 위치를 목적지로 설정 -> Moving 상태로 변경
        // 몬스터 클릭 시 평타 사거리만큼 떨어진 곳으로 설정
        if (_isInputLocked)
            return; 

        // 그냥 우클릭 시 → 이동 처리
        if (Input.GetMouseButton(mouseButton))
        {
            if (_isAttackGround == true)
            {
                _isAttackGround = false;
                Cursor.SetCursor(_cursorDefault, Vector2.zero, CursorMode.Auto);
            }

            _isStop = false;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Target = TryGetAttackableObject();
            Vector3 targetPos = Vector3.zero;

            // 타겟을 못 찾았을 때 -> 지형 클릭
            if (Target == null)
            {
                int mapMask = 1 << LayerMask.NameToLayer("Map");
                if (Physics.Raycast(ray, out RaycastHit rayHit, 1000.0f, mapMask))
                {
                    targetPos = rayHit.point;
                }
            }
            else
            {
                targetPos = TryGetAttackPosition(Target);
            }

            // 최종 목적지 설정
            // 플레이어와 너무 가까운 곳을 클릭하면 이동하지 않음
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(transform.position, navHit.position, NavMesh.AllAreas, path))
                {
                    Vector3 destination = navHit.position;
                    if (path.status == NavMeshPathStatus.PathPartial)
                    {
                        // 도달 가능한 마지막 지점
                        destination = path.corners[path.corners.Length - 1];
                    }

                    if (Target == null)
                    {
                        float distance = Vector3.Distance(transform.position, destination);
                        if (distance >= _minMoveDistance)
                        {
                            _agent.SetDestination(destination);
                            State = CreatureState.Moving;

                            _moveKeyPressed = true;
                        }
                    }
                    else
                    {
                        if (Vector3.Distance(targetPos, transform.position) > 0.0f)
                        {
                            _agent.SetDestination(destination);
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
    }

    protected float _originSpeed = 0;
    protected virtual void GetMouseInputDuringCharging()
    {
        if (_agent == null)
            return;

        if (Input.GetMouseButton(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f);

            Vector3 targetPos;

            targetPos = hit.point;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                if (_agent.isActiveAndEnabled)
                {
                    if (!_moveKeyPressed)
                    {
                        if(_keyCode == KeyCode.Q)
                            PlayAnimation("CHARGING_RUN", 0.1f);
                        _agent.speed = _originSpeed * 0.5f;
                    }

                    _agent.SetDestination(navHit.position);
                    _moveKeyPressed = true;
                }
            }
            UpdateTransform();
        }
    }

    protected virtual void GetMouseInputDuringSkill()
    {
        if (_agent == null)
            return;

        if (Input.GetMouseButton(1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f);

            Vector3 targetPos;

            targetPos = hit.point;

            if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            {
                if(_agent.isActiveAndEnabled)
                {
                    _agent.SetDestination(navHit.position);
                    _moveKeyPressed = true;
                }                 
            }

            UpdateTransform();
        }
    }
    #endregion

    #region Animation
    protected override void PlayAnimation(string animName, float ratio)
    {
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
            // 스킬 쓰는 시간이 한 순간일 때(0.5초쯤..) 판단을 못해서 지울게요
            //if (State == CreatureState.Skill)
               // return;

            // 쿨타임이 끝났을 때
            if (_coolDownDict[_keyCode].isCoolDown)
                return;

            // 스태미나가 충분할 때
            if(Stamina < FindSkill(_keyCode).CurLevelStamina)
                return;

            // 패킷 보내기
            SendSkillPacket(_keyCode);

            Debug.Log($"스킬 사용! : {_keyCode}");         
        }
    }

    public override void AmplificationSkill(KeyCode kc, KeyCode tkc)
    {
        SendSkillPacket(kc, tkc, true);
    }

    protected SkillBase FindSkill(KeyCode keyCode)
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
        if (State == CreatureState.Skill)
            return;

        _isUseSkill = true;
        _keyCode = keyCode;
    }

    protected float GetCoolTime(KeyCode key)
    {
        return _coolDownDict[key].coolTime;
    }

    public virtual void OnSkillConfirmed(S_Skill skillPacket)
    {
        KeyCode key = (KeyCode)skillPacket.SkillInfo.KeyCode;

        // 쿨타임 코루틴 시작
        StartCoroutine(CoInputCooltime(key, skillPacket.CostInfo.CoolTime));

        // 스태미너 연동
        Stamina = skillPacket.CostInfo.Stamina;

        // 스킬 실행 UI 연동
        //PlayerInterface.UseSkill(KeyToUIEnum(key));
    }

    IEnumerator CoInputCooltime(KeyCode key, float time)
    {
        if(time <= 0.0f)
        {
            _coolDownDict[key].isCoolDown = false;
            _coolDownDict[key].coolTime = 0.0f;
            yield break;
        }

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

    protected void MakeSkillDict()
    {
        Dictionary<KeyCode, Data.SkillData> skills = DataManager.SkillDict[ObjInfo.Player.CharType];

        foreach(var data in skills)
        {
            SkillBase skill = new SkillBase();
            skill.SkillData = data.Value;
            _skills.Add(data.Key, skill);
        }
    }

    private void MakeCoolDownDict()
    {
        foreach (var skill in _skills)
        {
            _coolDownDict[skill.Key] = new CoolTime { isCoolDown = false, coolTime = 0.0f };
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
        KeyCode key = (KeyCode)System.Enum.Parse(typeof(KeyCode), skill.ToString());
        _skills[key].CurLevel += 1;

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

    public void UpdateCool()
    {
        if (null == PlayerInterface) 
            return;

        PlayerInterface.SetSkillCool(GameObjects.QSkill, _coolDownDict[KeyCode.Q].coolTime);
        PlayerInterface.SetSkillCool(GameObjects.WSkill, _coolDownDict[KeyCode.W].coolTime);
        PlayerInterface.SetSkillCool(GameObjects.ESkill, _coolDownDict[KeyCode.E].coolTime);
        PlayerInterface.SetSkillCool(GameObjects.RSkill, _coolDownDict[KeyCode.R].coolTime);
        PlayerInterface.SetSkillCool(GameObjects.TSkill, _coolDownDict[KeyCode.T].coolTime);
        //PlayerInterface.SetSkillCool(GameObjects.DSkill, );
        //PlayerInterface.SetSkillCool(GameObjects.FSkill, );
    }

    private void MakeInventory()
    {
        for (int i = 0; i < _maxInventorySlot; ++i)
        {
            _inventory.Add(null); //비어 있는 인벤토리를 생성
        }
    }

    public void SetTimer(int phase, float clientLocalTargetRealtimeSinceStartupEnd)
    {
        _playerHUD.SetTimer(phase, clientLocalTargetRealtimeSinceStartupEnd);
    }

    public override void SetKDA(int kill, int death, int asist)
    {
        base.SetKDA(kill, death, asist);
        _playerHUD.SetKDA(kill, death, asist);
    }

    public void NotifyKill(PlayerController attPc, PlayerController diePc)
    {
        _playerHUD.NotifyKill(attPc, diePc);
    }

    #endregion

    #region Effect
    protected GameObject FindEffect(string fxName)
    {
        return Managers.FX.FindEffect(ObjInfo.ObjectId, fxName);
    }
            // 스킬 시전 이펙트 : TODO : 나중에 키에 따른 이펙트만 지워줄 것
    protected void RemoveAllEffect()
    {
        Managers.FX.RemoveAllEffect(ObjInfo.ObjectId);
    }
    protected void RemoveEffect(string fxName)
    {
        Managers.FX.RemoveEffect(ObjInfo.ObjectId, FindEffect(fxName));
    }
    protected List<GameObject> PlayEffect(string fxName, Vector3 position = new Vector3(), Quaternion rot = new Quaternion())
    {
        List<EffectData> effectList =Managers.FX.GetEffectsByPrefabName(fxName);

        return Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, transform, position, rot);
    }

    protected override List<GameObject> PlayEffectTransform(CreatureState state, KeyCode key, EffectType type = EffectType.Caster, 
        GameObject target = null, Transform targetTransform = null)
    {
        List<EffectData> effectList = 
            Managers.FX.GetSkillEffectList(ObjInfo.Player.CharType, state, key, type);

        List<GameObject> EffectList = null;

        // 타겟의 이펙트
        if (type == EffectType.HitTarget && target != null)
        {
            EffectList = (targetTransform != null) ?
             Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, targetTransform)
             : Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, target.transform);
        }
        // 나의 이펙트
        else if (type == EffectType.Caster)
        {
            EffectList = (targetTransform != null) ?
            Managers.FX.PlayEffect(ObjInfo.ObjectId,effectList, targetTransform)
            : Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, this.transform);
        }

        return EffectList;
    }

    protected List<GameObject> PlayEffectAtPosition(CreatureState state, KeyCode key, Vector3 position, Quaternion rot, EffectType type = EffectType.Caster)
    {
        List<EffectData> effectList = Managers.FX.GetSkillEffectList(ObjInfo.Player.CharType, state, key, type); 

        if (effectList == null || effectList.Count == 0) return null;

        List<GameObject> EffectList = Managers.FX.PlayEffect(ObjInfo.ObjectId, effectList, this.transform, position, rot);

        return EffectList;
    }
    #endregion
    #region Inventory

    public void ChangeInventory(S_ChangeInventory packet)
    {
        foreach (var change in packet.Changes)
        {
            //빈칸 처리
            if(change.ItemId == 0)
            {
                //TODO UI 작업
                _inventory[change.InventoryIndex] = null;
            }
            else
            {
                if (DataManager.ItemDict.TryGetValue(change.ItemId, out ItemInfoBase item))
                {
                    if(change.Count == 0)
                    {
                        // 장비 아이템
                        _inventory[change.InventoryIndex] = item;
                    }
                    else
                    {
                        // 소모 아이템
                        ConsumableItemInfo consumableItem = item as ConsumableItemInfo;
                        if (consumableItem == null)
                        {
                            Debug.Log($"Error. [{GetType()}] in ChangeInventory, consumableItem == null");
                            continue;
                        }
                        consumableItem.Count = change.Count;

                        _inventory[change.InventoryIndex] = consumableItem;
                    }
                }
                else
                {
                    //유효하지 않은 아이템 아이디.
                }
            }
        }
    }
    #endregion

    #region Util
    // 스킬 사용이 가능한가?
    protected bool EnabledSkill(KeyCode key)
    {
        if (!PlayerInterface.IsActiveKey.ContainsKey(key))
            return false;

        if (_coolDownDict[key].isCoolDown)
            return false;

        return true;
    }
    protected void UpdateTransform(bool isWarp = false)
    {
        CellPos = transform.position;
        RotInfo = transform.rotation;
        _updated = true;
        _isWarp = isWarp;
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

    protected virtual void ResetCharacterState()
    {
        // Input
        _moveKeyPressed = false;
        _isStop = false;

        // State : Skill
        _isUseSkill = false;
        _keyCode = KeyCode.None;

        foreach (var skill in _coolDownDict)
        {
            skill.Value.isCoolDown = false;
            skill.Value.coolTime = 0;
        }

        // State : Moving
        ResetCoroutine(_coLookAtTarget);

        // State : Attack
        _isAttackLoop = false;
        _attackIndex = 0;
        ResetTarget();
        ResetCoroutine(_attackRoutine);

        // State : Rest
        _isResting = false;
        ResetCoroutine(_coRest);

        // TODO : 필요한가
        // NavMeshAgent
        _agent.enabled = false;
    }

    protected void ResetCoroutine(Coroutine coroutine)
    {
        if(coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    protected Vector3 GetCursorPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return new Vector3(hit.point.x, 0, hit.point.z); // 충돌 지점이 곧 월드 좌표
        }
        return new Vector3(-1, -1, -1);
    }
    #endregion

    // 타겟 저장
    protected void RegisterTarget(GameObject target)
    {

    }

    #region Packet
    private void SendSkillPacket(KeyCode key, KeyCode tKey = KeyCode.None, bool isAmplification = false)
    {
        Vector3 mousePos = new Vector3();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            mousePos = new Vector3(hit.point.x, hit.point.y, hit.point.z);

        C_Skill skillPacket = new C_Skill()
        {
            ObjectInfo = ObjInfo,
            SkillInfo = new SkillInfo() 
            {
                KeyCode = (int)key, 
                Amplification = isAmplification,
                AmplifiKeyCode = (int)tKey,
            },
            TargetPosX = mousePos.x, TargetPosZ = mousePos.z,
            ChargeRatio = _ratioSkillDuration,
        };

        _ratioSkillDuration = 0f;
        Managers.Network.Send(skillPacket);
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

    private void SendStatePacket()
    {
        C_PlayerState statePacket = new C_PlayerState();
        statePacket.State = State;
        Managers.Network.Send(statePacket);
    }
    #endregion

    #region Util
    // 시작 지점과 타겟 지점 사이에 장애물이 있으면 충돌 위치 반환
    // 없으면 유효 위치 반환
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

    // 플레이어 위치로부터 마우스 방향으로 사거리 내 이동 가능한 위치 반환
    // isMaxDistance가 true 이면 항상 최대 사거리 기준 반환, false 이면 사거리 내 위치 반환
    protected Vector3 GetTargetPos(float range, bool isMaxDistance = true)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f);

        Vector3 dir = (hit.point - transform.position).normalized;
        dir.y = 0;

        if (!isMaxDistance && (hit.point - transform.position).magnitude < range)
        {
            return hit.point;
        }

        return transform.position + dir * range;
    }

    // 마우스 바라보기
    protected void LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = (1 << LayerMask.NameToLayer("Map")) | (1 << LayerMask.NameToLayer("Wall"));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000, layerMask))
        {
            Vector3 direction = (hit.point - transform.position).normalized;
            direction.y = 0;
            direction.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = targetRotation;
            UpdateTransform();
        }
    }

    #endregion
}
