using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif

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
    public string stateName;
    public override NodeStatus Execute(GameObject owner)
    {
        if(Check(owner) == false)
            return NodeStatus.Failure;

        if (string.IsNullOrEmpty(paramName))
            return NodeStatus.Failure;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.normalizedTime >= 0.95f)
        {
            monsterController.isSpawned = true;
            _animator.SetBool(paramName, false);
            return NodeStatus.Success;
        }
       
        _animator.SetBool(paramName, true);
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
        if (Check(owner) == false)
            return NodeStatus.Failure;

        if (monsterController.State != CreatureState.Moving)
        {
            _animator.SetFloat("moveVelocity", 0);
            return NodeStatus.Failure;
        }

        float speed = 0;
        if (!_isFirstFrame)
        {
            float distance = Vector3.Distance(owner.transform.position, _lastPos);

            speed = distance / Time.deltaTime;
        }

        _isFirstFrame = false;

        _animator.SetFloat("moveVelocity", speed);

        _lastPos = owner.transform.position;

        return NodeStatus.Running;
    }
    public override void HandleStateChange(CreatureState newState) { }
}

// 스킬 사용 중에 사용될 애니메이션 노드
public class PlayAnimatorTriggerNode : AnimationControlNode
{
    [Tooltip("Animator에 설정된 Trigger 이름")]
    public string triggerName;
    public string boolName;
    public bool bLoop = false;

    [Tooltip("Animator에 설정된 실제 애니메이션 상태의 이름. 예: Skill01State")]
    public string animationStateName;
    private bool _isSentEndPacket = false;

    public override NodeStatus Execute(GameObject owner)
    {
        if (Check(owner) == false)
            return NodeStatus.Failure;
       
        if (_isSentEndPacket == false)
        {
            _animator.SetTrigger(triggerName);
            _isSentEndPacket = true;

            if (bLoop)
                _animator.SetBool(boolName, true);

            return NodeStatus.Running;
        }

        //if (_animator.GetCurrentAnimatorStateInfo(0).IsName(animationStateName) || _animator.IsInTransition(0))
        //    return NodeStatus.Running;

        return NodeStatus.Running;
    }

    public override void HandleStateChange(CreatureState newState)
    {
        if (_animator == null)
            return;

        if (newState == CreatureState.Idle)
        {
            _animator.ResetTrigger(triggerName);
            //_animator.CrossFade("Idle", 0.1f);
            _isSentEndPacket = false;

            if (bLoop)
                _animator.SetBool(boolName, false);
        }
    }
}