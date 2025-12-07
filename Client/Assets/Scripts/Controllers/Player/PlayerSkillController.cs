using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerSkillController : MonoBehaviour
{
    private MyPlayerController _player;
    private NavMeshAgent _agent;

    protected Dictionary<KeyCode, SkillBase> _skills = new Dictionary<KeyCode, SkillBase>();
    public Dictionary<KeyCode, SkillBase> SkillDict { get { return _skills; } }

    Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();
    public Dictionary<KeyCode, CoolTime> CoolDownDict {  get { return _coolDownDict; } }
    public class CoolTime
    {
        public bool isCoolDown;
        public float coolTime;
    }
    private Dictionary<KeyCode, Coroutine> _coolDownCoDict = new Dictionary<KeyCode, Coroutine>();
    
    Coroutine _applyVericalMotion = null;

    // TEMP 이거 쓰나
    private Coroutine _motionCo;
    private bool _isSkillMotion;
    public bool IsInSkillMotion => _isSkillMotion;
    Coroutine _streamCo;
    int _currentInstanceId;

    public bool CanMoveDuringCast = false;

    KeyCode _key = KeyCode.None;
    SkillSpec _curSkill;
    int _targetId;
    Vector3 _mousePos;

    // TEMP
    Vector3 _endPosition;

    private void Awake()
    {
        _player = GetComponentInChildren<MyPlayerController>();
        _agent = GetComponentInChildren<NavMeshAgent>();
    }

    public void Init()
    {
        MakeSkillDict();
        MakeCoolDownDict();
    }

    // checkSkillState -> 차징 상태일 때 Skill 변경이 안돼서 추가했어요
    public C_SkillInput TryCast(int skillKey, int targetId, Vector3 clickWorld, bool checkSkillState = true)
    {
        _key = (KeyCode)skillKey;
        _targetId = targetId;
        _mousePos = clickWorld;

        if (_coolDownDict.ContainsKey(_key))
        {
            // When the skill level is 0
            //if (FindSkill(_key).CurLevel <= 0)
            //    return null;

            // 스킬을 사용하고 있는 상태가 아닐 때
            //if (checkSkillState && 
            //    /*_player.State == CreatureState.Skill &&*/ false == _player.CanStopSkill)
            //    return null;

            // 쿨타임이 끝났을 때
            if (_coolDownDict[_key].isCoolDown)
                return null;
        
            // 스태미나가 충분할 때
            if (_player.Stamina < FindSkill(_key).CurLevelStamina)
                return null;

            // 패킷 보내기
            Debug.Log($"스킬 사용시도! : {_key}");
            return new C_SkillInput
            {
                SkillKey = skillKey,
                TargetId = targetId,
                MouseX = clickWorld.x,
                MouseZ = clickWorld.z
            };
        }
        else
            return null;
    }

    public void OnSkill(S_SkillMotion packet)
    {
        if (packet.Type == SkillMotionType.Agent)
        {
            PlaySkillMotion((SkillMotionType)packet.Type,
            new Vector3(packet.StartX, packet.StartY, packet.StartZ),
            new Vector3(packet.EndX, packet.EndY, packet.EndZ),
            packet.Duration, packet.Anim, packet.CurveId,
            packet.ServerCollision, packet.AuthoritativeEnd);
        }
        else if (packet.Type == SkillMotionType.Transform)
        {
            ApplySkillMotion(new Vector3(packet.EndX, packet.EndY, packet.EndZ),
            packet.AuthoritativeEnd);
        }
        else if (packet.Type == SkillMotionType.VerticalTransform)
        {
            if (_applyVericalMotion != null)
                StopCoroutine(_applyVericalMotion);

            Vector3 targetPos = new Vector3(packet.StartX, packet.StartY, packet.StartZ);
            Vector3 startPos = _player.transform.position;
            _applyVericalMotion = StartCoroutine(Co_ApplyVerticalMotion(startPos, targetPos, packet.Duration));
        }
    }
    public void OnSkillCollisionRequest(S_SkillCollisionRequest packet)
    {
        KeyCode key = (KeyCode)packet.SkillKey;

        _player.SendPacket(ComputeSkillCollision(packet.SkillKey, packet.RequestId, packet.Type, packet.StartX, packet.StartZ, packet.EndX, packet.EndZ));

        CreateSkillMesh(key);
    }

    // 스킬 시작 승인(시전별 instanceId 포함 -> 안함)
    public void OnSkillConfirm(S_SkillConfirm packet)
    {
        CanMoveDuringCast = packet.CanMove;     
    }

    public void OnSkillCost(S_SkillCost packet)
    {
        KeyCode key = (KeyCode)packet.SkillKey;

        //쿨타임 코루틴 시작
        //if (_CoolDownCo != null)
        //    StopCoroutine(_CoolDownCo);
        //_CoolDownCo = StartCoroutine(CoInputCooltime(key, packet.CostInfo.CoolTime));

        if(_coolDownCoDict.TryGetValue(key, out var co) && co != null)
        {
            StopCoroutine(co);           
            _coolDownCoDict.Remove(key);
        }

        var newCo = StartCoroutine(CoInputCooltime(key, packet.CostInfo.CoolTime));
        _coolDownCoDict[key] = newCo;

        //스태미너 연동
        _player.Stamina = packet.CostInfo.Stamina;

        CreateSkillMesh(key);
    }

    public C_SkillCollisionPropose ComputeSkillCollision(int skillKey, int requestId, CollisionType type, float startX, float startZ, float endX, float endZ)
    {
        C_SkillCollisionPropose packet = new C_SkillCollisionPropose();
        packet.SkillKey = skillKey;
        packet.RequestId = requestId;
        packet.Seq = 1;

        Vector3 startPos = new Vector3(startX, transform.position.y, startZ);
        Vector3 targetPos = new Vector3(endX, transform.position.y, endZ);

        if((startPos.x == 0 && startPos.z == 0) || (targetPos.x == 0 && targetPos.z == 0))
        {
            Debug.Log($"SkillCollision Input Error! : startPos - {startPos}, targetPos - {targetPos}");
            packet.CollisionX = startX;
            packet.CollisionZ = startZ;
            return packet;
        }    

        Vector3 collisionPos = startPos;
        if (type == CollisionType.Block)
            collisionPos = ComputeEndBlocked(startPos, targetPos);
        else if (type == CollisionType.Pass)
            collisionPos = ComputeEndPass(startPos, targetPos);
        else if (type == CollisionType.Clamp)
            collisionPos = ComputeClamp(startPos, targetPos);

        packet.CollisionX = collisionPos.x;
        packet.CollisionZ = collisionPos.z;

        return packet;
    }

    public void StopSkillMotion()
    {
        if (_motionCo != null)
            StopCoroutine(_motionCo);

        if (_player.AllowOffPathMovement)
            return;

        if (_agent == null)
            return;
        //_agent.Warp(_endPosition);
        _agent.enabled = true;
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.isStopped = false;

        _isSkillMotion = false;
        _motionCo = null;

        //_player.UpdateTransform();
    }

    public void PlaySkillMotion(SkillMotionType type, Vector3 start, Vector3 end,
                            float duration, string anim, string curveId,
                            bool serverCollision, bool authoritativeEnd)
    {
        if (_motionCo != null)
            StopCoroutine(_motionCo);
        _motionCo = StartCoroutine(Co_PlaySkillMotion(type, start, end, duration, anim, curveId, authoritativeEnd));
    }

    private IEnumerator Co_PlaySkillMotion(SkillMotionType type, Vector3 start, Vector3 end,
                                           float duration, string anim, string curveId, bool authoritativeEnd)
    {
        _isSkillMotion = true;
      
        // MoveSync 보낼 때 isSkillMotion=true로 태깅하도록 노출
        if (!_agent.enabled)
            _agent.enabled = true;
        _agent.isStopped = true;
        _agent.updatePosition = false;
        _agent.updateRotation = false;
        _agent.ResetPath();

        // NavMesh 위로 수정
        if (NavMesh.SamplePosition(start, out var startHit, 2.0f, NavMesh.AllAreas))
            start = startHit.position;
        if (NavMesh.SamplePosition(end, out var endHit, 2.0f, NavMesh.AllAreas))
            _endPosition = end = endHit.position;

        // 시작점 동기화
        _agent.nextPosition = start;
        transform.position = start;

        //if (!string.IsNullOrEmpty(anim))
        //    PlayAnimFromServer(anim, 0.05f);

        if (type == SkillMotionType.Transform)
        {
            _agent.Warp(end);
            _agent.nextPosition = end;
            transform.position = end;
            _player.UpdateTransform();
        }
        else
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                u = ApplyCurve(u, curveId);

                // 정상 보간값 u 사용 (Time.time * 20 금지)
                Vector3 pos = Vector3.Lerp(start, end, u);

                // 에이전트 내부 좌표를 주도적으로 업데이트
                _agent.nextPosition = pos;
                transform.position = pos;  // 가시 좌표도 맞춤 (둘 중 하나만 써도 되지만 일치시키는 게 안전)
                _player.UpdateTransform();

                // (옵션) 회전도 보간하고 싶으면 여기서 수동 회전
                yield return null;
            }
        }
         
        // 최종 스냅: 서버 권위가 end라면 Warp
        if (authoritativeEnd)
            _agent.Warp(end);
        else
        {
            _agent.nextPosition = end;
            transform.position = end;
        }

        _agent.updatePosition = true;
        _agent.updateRotation = true;                        
        _agent.isStopped = false;

        _isSkillMotion = false;
        _motionCo = null;

        _player.UpdateTransform();
    }

    private void ApplySkillMotion(Vector3 targetPos, bool authoritativeEnd)
    {
        if(_agent != null || _agent.enabled)
            _agent.enabled = false;

        Vector3 finalPos = targetPos;
        finalPos.y = transform.position.y;

        //if (authoritativeEnd)
        //{
        //    if (NavMesh.SamplePosition(finalPos, out var finHit, 0.1f, NavMesh.AllAreas))
        //        finalPos = finHit.position;
        //}

        transform.position = finalPos;
        _player.UpdateTransform();

        if(authoritativeEnd)
            _agent.enabled = true;
    }

    private float ApplyCurve(float u, string id)
    {
        switch (id)
        {
            case "EaseOutCubic":
                return 1f - Mathf.Pow(1f - u, 3f);
            case "Linear":
            default:
                return u;
        }
    }

    private IEnumerator Co_ApplyVerticalMotion(Vector3 originPos, Vector3 targetPos, float duration)
    {
        _player.AllowOffPathMovement = true;

        if (_agent.enabled)
            _agent.enabled = false;

        Vector3 startPos = transform.position;
        float ascendDuration = duration * 0.1f;
        float descendDuration = duration * 0.5f;
        float timer = 0f;

        while (timer < ascendDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / ascendDuration);

            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

            transform.position = Vector3.Lerp(startPos, targetPos, easedProgress); // easedProgress 사용
            yield return null;
        }
        transform.position = targetPos;

        yield return new WaitForSeconds(0.8f);

        timer = 0f;
        Vector3 peakPos = transform.position;

        while (timer < descendDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / descendDuration);

             float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
             transform.position = Vector3.Lerp(peakPos, originPos, easedProgress);
            yield return null;
        }
        transform.position = originPos;

        _agent.enabled = true;
        _applyVericalMotion = null;
        _agent.Warp(transform.position);

        _player.AllowOffPathMovement = false;
    }
    #region Util
    Vector3 ComputeEndBlocked(Vector3 startPos, Vector3 targetPos)
    {
        return GetReachablePosition(startPos, targetPos, out NavMeshHit hit);
    }

    Vector3 ComputeEndPass(Vector3 startPos, Vector3 targetPos)
    {
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
            return GetValidPosition(startPos, navHit.position);

        Debug.Log("ComputeEndPass Error!");
        return targetPos;
    }

    Vector3 ComputeClamp(Vector3 startPos, Vector3 targetPos)
    {
        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
            return GetValidPosition(startPos, navHit.position);
        else
        {
            return GetReachablePosition(startPos, targetPos, out NavMeshHit hit);
        }
    }

    private Vector3 GetValidPosition(Vector3 startPos, Vector3 targetPos)
    {
        Vector3 validPos = targetPos;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(startPos, targetPos, _agent.areaMask, path) || path.status != NavMeshPathStatus.PathComplete)
        {
            // 경로 자체가 없으면 레이캐스트로 첫 히트 포인트 클램프
            if (NavMesh.Raycast(startPos, targetPos, out var hit, NavMesh.AllAreas))
            {
                validPos = hit.position;
            }
        }
        return validPos;
    }

    protected Vector3 GetTargetPos(float range, bool isMaxDistance = true)
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        bool raycastHit = Physics.Raycast(ray, out hit, 1000.0f);

        Vector3 hitPos = hit.point; hitPos.y = transform.position.y;
        Vector3 dir = (hitPos - transform.position).normalized;

        if (!isMaxDistance && (hit.point - transform.position).magnitude < range)
        {
            return hit.point;
        }

        return transform.position + dir * range;
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

        Debug.Log("GetReachablePosition Error!");
        return startPos;
    }

    void LocalFollowTick(int targetId, float speed, bool passWalls) 
    {
        var tp = Managers.Object.FindById(targetId).transform.position;
        var me = transform.position;
        var dir = tp - me;
        dir.y = 0;
        var step = speed * Time.deltaTime;
        if (step > dir.magnitude)
            step = dir.magnitude;
        if (step > 0)
            transform.position += dir.normalized * step;
    }

    public void CreateSkillMesh(KeyCode keyCode)
    {
       //if (DataManager.SkillHitboxDict[_player.ObjInfo.Player.CharType].TryGetValue(keyCode, out SkillHitbox skillHitbox))
       //{
       //    GameObject go = Managers.Resource.Instantiate("Debug/SkillMesh", gameObject.transform);
       //    SkillMesh sm = go.GetComponent<SkillMesh>();
       //    if (sm == null)
       //        return;
       //    sm.Init(skillHitbox, gameObject.transform, _player.ObjInfo.Player.Team);

       //    if (_player.ObjInfo.Player.CharType == CharacterType.Abigail && keyCode == KeyCode.Q)
       //        CreateSkillMesh(KeyCode.F1);
       //}
    }
    #endregion

    #region Skill Cost
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

    IEnumerator CoInputCooltime(KeyCode key, float time)
    {
        if (!_coolDownDict.ContainsKey(key))
            yield break;

        if (time <= 0.0f)
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
    #endregion

    #region Dictionary
    protected void MakeSkillDict()
    {
        Dictionary<KeyCode, Data.SkillData> skills = DataManager.SkillDict[_player.ObjInfo.Player.CharType];

        foreach (var data in skills)
        {
            SkillBase skill = new SkillBase();
            skill.SkillData = data.Value;
            _skills.Add(data.Key, skill);
        }

        _skills[KeyCode.T].CurLevel = 1;
        _skills[KeyCode.F].CurLevel = 1;

        if (_skills.TryGetValue(KeyCode.D, out var value))
            value.CurLevel = 1;
    }

    private void MakeCoolDownDict()
    {
        foreach (var skill in _skills)
        {
            _coolDownDict[skill.Key] = new CoolTime { isCoolDown = false, coolTime = 0.0f };
        }
    }
    #endregion
}

