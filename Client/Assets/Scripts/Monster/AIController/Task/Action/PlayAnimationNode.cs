using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

public interface IStateChangeListener
{
    void HandleStateChange(CreatureState newState);
}

public abstract class AnimationControlNode : ActionNode, IStateChangeListener
{
    protected Animator _animator;
    protected MonsterController monsterController;
    protected NavMeshAgent _navMeshAgent;

    protected bool Check(GameObject owner)
    {
        if (monsterController == null)
            monsterController = owner.GetComponentInChildren<MonsterController>();

        if (_animator == null)
            _animator = owner.GetComponentInChildren<Animator>();

        if (_navMeshAgent == null)
            _navMeshAgent = owner.GetComponentInChildren<NavMeshAgent>();

        return (monsterController != null && _animator != null && _navMeshAgent != null);
    }

    public abstract void HandleStateChange(CreatureState newState);

}

// 애니메이션 재생 노드
public class PlayAnimatorBoolNode : AnimationControlNode
{
    public string paramName;
    public bool valueToSet;

    public override NodeStatus Execute(GameObject owner)
    {
        if(Check(owner) == false)
            return NodeStatus.Failure;

        if (string.IsNullOrEmpty(paramName))
        {
            Debug.LogError("No param");
            return NodeStatus.Failure;
        }

        _animator.SetBool(paramName, valueToSet);
        return NodeStatus.Running;
    }

    public override void HandleStateChange(CreatureState newState)
    {
    }
}

// 움직임에 사용될 애니메이션 노드
public class PlayAnimatorFloatNode : AnimationControlNode
{
    private Vector3 _lastPos;
    private bool _isFirstFrame = true;

    public override NodeStatus Execute(GameObject owner)
    {
        // 1. 필요한 컴포넌트가 모두 있는지 확인
        if (Check(owner) == false)
        {
            return NodeStatus.Failure;
        }

        // 이동 상태가 아니면 걷기 애니메이션을 멈춥니다.
        if (monsterController.State != CreatureState.Moving)
        {
            _animator.SetFloat("moveVelocity", 0);
            return NodeStatus.Failure;
        }

        // 2. 현재 프레임의 속도 계산
        float speed = 0;
        if (!_isFirstFrame)
        {
            // 이전 프레임 위치와 현재 위치의 거리를 구합니다.
            float distance = Vector3.Distance(owner.transform.position, _lastPos);

            // `Time.deltaTime`으로 나눠 초당 속도를 계산합니다.
            speed = distance / Time.deltaTime;
        }

        _isFirstFrame = false;

        // 3. 애니메이션 파라미터 업데이트
        _animator.SetFloat("moveVelocity", speed);

        // 4. 다음 프레임을 위해 현재 위치를 저장
        _lastPos = owner.transform.position;

        return NodeStatus.Running;
    }

    public override void HandleStateChange(CreatureState newState)
    {
    }

}

// 스킬 사용 중에 사용될 애니메이션 노드
public class PlayAnimatorTriggerNode : AnimationControlNode
{
    [Tooltip("Animator에 설정된 Trigger 이름")]
    public string triggerName;

    [Tooltip("Animator에 설정된 실제 애니메이션 상태의 이름. 예: Skill01State")]
    public string animationStateName;
    private bool _isSentEndPacket = false;

    public override NodeStatus Execute(GameObject owner)
    {
        if (Check(owner) == false)
            return NodeStatus.Failure;

        Debug.Log("스킬 애니메이션 가동 중");
        if(monsterController.State != CreatureState.Skill)
        {
            Debug.Log("애니메이션 멈추기");
            _animator.ResetTrigger(triggerName);
            _animator.CrossFade("Idle", 0.1f);
            _isSentEndPacket = false;
            return NodeStatus.Failure; 
        }

        if (_isSentEndPacket == false)
        {
            _animator.SetTrigger(triggerName);
            _isSentEndPacket = true;
        }
        return NodeStatus.Running;
    }

    public override void HandleStateChange(CreatureState newState)
    {
        if (_animator == null)
            return;

        if (newState == CreatureState.Idle)
        {
            _animator.ResetTrigger(triggerName);
            _isSentEndPacket = false;
            //monsterController.SendSkillEndPacket(monsterController.Skill);
        }
    }

}