using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Android;
using UnityEngine.InputSystem;

public class PlayerSkillController : MonoBehaviour
{
    private MyPlayerController _player;
    private NavMeshAgent _agent;

    private Dictionary<KeyCode, SkillSpec> _skillSpecs = new Dictionary<KeyCode, SkillSpec>();

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

    private void Awake()
    {
        _player = GetComponentInChildren<MyPlayerController>();
        _agent = GetComponentInChildren<NavMeshAgent>();
    }

    private void Start()
    {
        MakeSkillSpecDict();
    }

    public C_SkillInput TryCast(int skillKey, int targetId, Vector3 clickWorld)
    {
        _key = (KeyCode)skillKey;
        _curSkill = GetSkillSpec(_key);
        _targetId = targetId;
        mousePos = clickWorld;

        SendSkillCollisionPacket();

        return new C_SkillInput
        {
            SkillKey = skillKey,
            TargetId = targetId,
            MouseX = clickWorld.x,
            MouseZ = clickWorld.z
        };
    }

    public void OnSkill(S_SkillMotion packet)
    {
        PlaySkillMotion((SkillMotionType)packet.Type,
            new Vector3(packet.StartX, packet.StartY, packet.StartZ),
            new Vector3(packet.EndX, packet.EndY, packet.EndZ),
            packet.Duration, packet.Anim, packet.CurveId,
            packet.ServerCollision, packet.AuthoritativeEnd);
    }

    // 스킬 시작 승인(시전별 instanceId 포함 -> 안함)
    public void OnSkillConfirm(S_SkillFollow m)
    {
        //_currentInstanceId = m.instanceId;
        //if (!SkillSpecCache.TryGet(m.skillKey, out var spec))
        //    return;
        //if (spec.proposalMode != ProposalMode.SingleShot)
        //    return;
            
    }
 
    public void OnS_SkillMotion(S_SkillMotion s)
    {
        //if (s.instanceId != _currentInstanceId)
        //    return;
        //if (_streamCo != null)
        //{ StopCoroutine(_streamCo); _streamCo = null; }
        //GetComponent<PlayerViewController>().PlaySkillMotion(s); // 코루틴 → 끝에 Agent.Warp
    }

    //public void OnS_SkillEnd(S_SkillEnd e)
    //{
    //    if (e.instanceId != _currentInstanceId)
    //        return;
    //    if (_streamCo != null)
    //    { StopCoroutine(_streamCo); _streamCo = null; }
    //    // 취소/정리
    //}

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
                _player.SendPacket(ComputeSkillCollision((int)_key, _curSkill, _targetId, mousePos.x, mousePos.z));
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

    #region Util
    private SkillSpec GetSkillSpec(KeyCode key)
    {
        return _skillSpecs[key];
    }

    private C_SkillCollisionPropose ComputeSkillCollision(int skillKey, SkillSpec spec, int targetId, float clickX, float clickZ)
    {
        C_SkillCollisionPropose packet = new C_SkillCollisionPropose();
        packet.SkillKey = skillKey;
        packet.Seq = 1; // TEMP
        packet.Mode = spec.proposalMode;
        packet.Speed = spec.limits.speed;  // TEMP : 필요한가

        if (_curSkill.needs.endBlocked)
        {
            Vector3 endBlocked = ComputeEndBlocked(skillKey, spec, clickX, clickZ);
            packet.EndBlockedX = endBlocked.x;
            packet.EndBlockedZ = endBlocked.z;
        }
        if(_curSkill.needs.endPass)
        {
            Vector3 endPass = ComputeEndPass(skillKey, spec, clickX, clickZ);
            packet.EndPassX = endPass.x;
            packet.EndPassZ = endPass.z;
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

        // 시작점 동기화
        _agent.nextPosition = start;
        transform.position = start;

        //if (!string.IsNullOrEmpty(anim))
        //    PlayAnimFromServer(anim, 0.05f);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            u = ApplyCurve(u, curveId);

            // ★ 정상 보간값 u 사용 (Time.time * 20 금지)
            Vector3 pos = Vector3.Lerp(start, end, u);

            // ★ 에이전트 내부 좌표를 주도적으로 업데이트
            _agent.nextPosition = pos;
            transform.position = pos;  // 가시 좌표도 맞춤 (둘 중 하나만 써도 되지만 일치시키는 게 안전)
            _player.UpdateTransform();

            // (옵션) 회전도 보간하고 싶으면 여기서 수동 회전
            yield return null;
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

    // --- 후보 계산 유틸 (NavMesh 사용) ---
    Vector3 ComputeEndBlocked(int skillKey, SkillSpec spec, float clickX, float clickZ) 
    {
        var start = transform.position;

        Vector3 blocked;
        Vector3 targetPos = GetTargetPos(3.0f);
        blocked = GetReachablePosition(start, targetPos, out NavMeshHit navHit);

        Vector3 pass = GetTargetPos(3.0f);

        return blocked;
    }

    Vector3 ComputeEndPass(int skillKey, SkillSpec spec, float clickX, float clickZ)
    {
        Vector3 targetPos = GetTargetPos(3.0f);

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

        Vector3 dir = (hit.point - transform.position).normalized;
        dir.y = 0;

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

    // 타겟/오브젝트 좌표 및 전방을 얻는 헬퍼는 프로젝트에 맞춰 교체
    Vector3 GetTargetPos(int targetId)
    {
        var obj = Managers.Object.FindById(targetId);
        return obj != null ? obj.transform.position : transform.position;
    }

    Vector3 GetTargetForwardXZ(int targetId)
    {
        var obj = Managers.Object.FindById(targetId);
        if (obj == null)
            return transform.forward;
        var f = obj.transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 1e-6f ? f.normalized : transform.forward;
    }

    // NavMesh 위로 살짝 보정하는 헬퍼
    static Vector3 SampleOnNav(Vector3 p, float maxDist = 1.0f, int areaMask = NavMesh.AllAreas)
    {
        if (NavMesh.SamplePosition(p, out var hit, maxDist, areaMask))
            return hit.position;
        return p; // 실패시 원본 유지(서버가 최종 보정)
    }

    // 레이캐스트로 벽 앞 점 얻기 (없으면 dest 그대로 반환)
    static Vector3 RaycastBlocked(Vector3 start, Vector3 dest, Vector3 dir, float skin)
    {
        if (NavMesh.Raycast(start, dest, out var hit, NavMesh.AllAreas))
            return hit.position - dir * skin;
        return dest;
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

    private void MakeSkillSpecDict()
    {
        Dictionary<KeyCode, Data.SkillSpec> skills = DataManager.SkillSpecDict[_player.ObjInfo.Player.CharType];

        foreach (var data in skills)
        {
            SkillSpec skill = new SkillSpec();
            skill = data.Value;
            _skillSpecs.Add(data.Key, skill);
        }
    }
    #endregion
}

