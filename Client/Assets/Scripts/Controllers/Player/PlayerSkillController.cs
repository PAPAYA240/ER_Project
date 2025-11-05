using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Android;
using UnityEngine.InputSystem;
using UnityEngine.Timeline;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static Unity.Burst.Intrinsics.X86.Avx;

public class PlayerSkillController : MonoBehaviour
{
    private MyPlayerController _player;
    private NavMeshAgent _agent;

    private Dictionary<KeyCode, SkillVariants> _skillSpecs = new Dictionary<KeyCode, SkillVariants>();
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

    private Coroutine _motionCo;
    private bool _isSkillMotion;

    public bool IsInSkillMotion => _isSkillMotion;

    Coroutine _streamCo;
    int _currentInstanceId;

    KeyCode _key = KeyCode.None;
    SkillSpec _curSkill;
    int _targetId;
    Vector3 mousePos;

    // TEMP
    Vector3 _endPosition;


    private void Awake()
    {
        _player = GetComponentInChildren<MyPlayerController>();
        _agent = GetComponentInChildren<NavMeshAgent>();
    }

    public void Init()
    {
        MakeSkillSpecDict();
        MakeSkillDict();
        MakeCoolDownDict();
    }

    public C_SkillInput TryCast(int skillKey, int targetId, Vector3 clickWorld)
    {
        _key = (KeyCode)skillKey;
        _targetId = targetId;
        mousePos = clickWorld;

        if (_coolDownDict.ContainsKey(_key))
        {
            if (FindSkill(_key).CurLevel <= 0)
                return null;

            // 스킬을 사용하고 있는 상태가 아닐 때
            if (_player.State == CreatureState.Skill && false == _player.CanStopSkill)
                return null;
        
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
        if(packet.Type == SkillMotionType.Agent)
        {
            PlaySkillMotion((SkillMotionType)packet.Type,
            new Vector3(packet.StartX, packet.StartY, packet.StartZ),
            new Vector3(packet.EndX, packet.EndY, packet.EndZ),
            packet.Duration, packet.Anim, packet.CurveId,
            packet.ServerCollision, packet.AuthoritativeEnd);
        }
        else if(packet.Type == SkillMotionType.Transform)
        {
            ApplySkillMotion((SkillMotionType)packet.Type,
            new Vector3(packet.EndX, packet.EndY, packet.EndZ));
        }
    }

    // 스킬 시작 승인(시전별 instanceId 포함 -> 안함)
    public void OnSkillConfirm(S_SkillConfirm packet)
    {
        _curSkill = GetSkillSpec((KeyCode)packet.SkillKey, packet.Variants);
        if (_curSkill != null)
        {
            SendSkillCollisionPacket();
        }

        KeyCode key = (KeyCode)packet.SkillKey;

        //스킬 실행 UI 연동
        //_player.UI.PlayerInterface.UseSkill(KeyToUIEnum(key));

        CreateSkillMesh(key);
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

    private void SendSkillCollisionPacket()
    {
        if (_curSkill.proposalMode == ProposalMode.SingleShot)
        {
            _player.SendPacket(ComputeSkillCollision((int)_key, _curSkill, _targetId, mousePos.x, mousePos.z));
        }
        else
        {
            //_currentInstanceId = m.instanceId;
            if (_streamCo != null)
                StopCoroutine(_streamCo);
            _streamCo = StartCoroutine(Co_StreamPropose());
        }
    }

    IEnumerator Co_StreamPropose(/*S_SkillFollow m*/)
    {
        //if (!SkillSpecCache.TryGet(m.skillKey, out var spec))
        //    yield break;

        int seq = 0;
        float t = 0f;

        // 추적 연출 중엔 Agent가 좌표를 끌어올리지 못하게
        _agent.isStopped = true;
        _agent.updatePosition = false;
        _agent.updateRotation = false;

        while (/*t < m.maxDuration*/ _player.State == CreatureState.Skill)
        {
            t += Time.deltaTime;

            // 10~15Hz 전송
            if (Time.frameCount % 6 == 0)
            {
                ++seq;
                _player.SendPacket(ComputeSkillCollision((int)_key, _curSkill, _targetId, mousePos.x, mousePos.z, seq));
            }

            // 로컬 추적 연출(간단)
            LocalFollowTick(_targetId, 6.0f, /*m.passThroughWalls*/ false);
            yield return null;
        }

        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.isStopped = false;
        _streamCo = null;
    }

    private SkillSpec GetSkillSpec(KeyCode key, VariantKey variants)
    {
        if (variants == VariantKey.NoCollision)
            return null;
        if(variants == VariantKey.Cast)
            return _skillSpecs[key].cast;
        if(variants == VariantKey.Followup)
            return _skillSpecs[key].followup;

        return null;
    }

    private C_SkillCollisionPropose ComputeSkillCollision(int skillKey, SkillSpec spec, int targetId, float clickX, float clickZ, int seq = 1)
    {
        C_SkillCollisionPropose packet = new C_SkillCollisionPropose();
        packet.SkillKey = skillKey;
        packet.Seq = seq;
        packet.Mode = spec.proposalMode;
        //packet.Speed = spec.limits.speed;  // TEMP : 필요한가

        if (_curSkill.needs.endBlocked)
        {
            Vector3 endBlocked = ComputeEndBlocked(skillKey, spec, clickX, clickZ);
            packet.EndBlockedX = endBlocked.x;
            packet.EndBlockedZ = endBlocked.z;

            Debug.Log($"EndBlocked => X : {endBlocked.x}, Z : {endBlocked.z}");
        }
        if(_curSkill.needs.endPass)
        {
            Vector3 endPass = ComputeEndPass(skillKey, spec, clickX, clickZ);
            packet.EndPassX = endPass.x;
            packet.EndPassZ = endPass.z;

            Debug.Log($"EndPass => X : {endPass.x}, Z : {endPass.z}");
        }
        if(_curSkill.needs.behindBlocked)
        {
            Vector3 behindBlocked = ComputeBehindBlocked(skillKey, spec, targetId, clickX, clickZ);
            packet.BehindBlockedX = behindBlocked.x;
            packet.BehindBlockedZ = behindBlocked.z;

            if(_curSkill.needs.candidateTargetId)
                packet.CandidateTargetId = targetId;
        }

        return packet;
    }

    public void StopSkillMotion()
    {
        if (_motionCo != null)
            StopCoroutine(_motionCo);

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

    private void ApplySkillMotion(SkillMotionType type, Vector3 targetPos)
    {
        if(_agent != null)
            _agent.enabled = false;

        // NavMesh 위로 수정
        //if (NavMesh.SamplePosition(targetPos, out var endHit, 2.0f, NavMesh.AllAreas))
        //    _endPosition = endHit.position;

        transform.position = targetPos;
        _player.UpdateTransform();

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

    #region Util
    Vector3 ComputeEndBlocked(int skillKey, SkillSpec spec, float clickX, float clickZ) 
    {
        var start = transform.position;

        Vector3 blocked;
        Vector3 targetPos = GetTargetPos(spec.limits.baseMaxDist);
        blocked = GetReachablePosition(start, targetPos, out NavMeshHit navHit);

        Vector3 pass = GetTargetPos(3.0f);

        return blocked;
    }

    Vector3 ComputeEndPass(int skillKey, SkillSpec spec, float clickX, float clickZ)
    {
        Vector3 targetPos = GetTargetPos(spec.limits.baseMaxDist);

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        else
        {
            var start = transform.position;
            return GetReachablePosition(start, targetPos, out NavMeshHit hit);
        }
    }

    Vector3 ComputeBehindBlocked(int skillKey, SkillSpec spec, int targetId, float clickX, float clickZ)
    {
        // TODO : 
        return Vector3.zero;
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

        return Vector3.zero;
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
        if (DataManager.SkillHitboxDict[_player.ObjInfo.Player.CharType].TryGetValue(keyCode, out SkillHitbox skillHitbox))
        {
            GameObject go = Managers.Resource.Instantiate("Debug/SkillMesh", gameObject.transform);
            SkillMesh sm = go.GetComponent<SkillMesh>();
            if (sm == null)
                return;
            sm.Init(skillHitbox, gameObject.transform, _player.ObjInfo.Player.Team);

            if (_player.ObjInfo.Player.CharType == CharacterType.Abigail && keyCode == KeyCode.Q)
                CreateSkillMesh(KeyCode.F1);
        }
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
    private void MakeSkillSpecDict()
    {
        Dictionary<KeyCode, Data.SkillVariants> skills = DataManager.SkillSpecDict[_player.ObjInfo.Player.CharType];

        foreach (var data in skills)
        {
            SkillVariants skill = new SkillVariants();
            skill = data.Value;
            _skillSpecs.Add(data.Key, skill);
        }
    }

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
        //_skills[KeyCode.F].CurLevel = 1;
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

