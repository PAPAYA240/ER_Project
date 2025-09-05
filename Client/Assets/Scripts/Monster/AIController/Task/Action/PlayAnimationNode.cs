using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.AI;

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
public class PlayAnimatorFloatNode : AnimationControlNode
{

    public override NodeStatus Execute(GameObject owner)
    {
        if (Check(owner) == false)
            return NodeStatus.Failure;

        float speed = _navMeshAgent.velocity.magnitude;
        _animator.SetFloat("moveVelocity", speed);

        return NodeStatus.Running;
    }

    public override void HandleStateChange(CreatureState newState)
    {
    }

}

public class PlayAnimatorTriggerNode : AnimationControlNode
{
    [Tooltip("Animator에 설정된 Trigger 이름")]
    public string triggerName;

    [Tooltip("Animator에 설정된 실제 애니메이션 상태의 이름. 예: Skill01State")]
    public string animationStateName;
    private bool _isTriggerSet = false;

    public override NodeStatus Execute(GameObject owner)
    {
        if (Check(owner) == false)
        {
            return NodeStatus.Failure;
        }

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (_isTriggerSet && stateInfo.normalizedTime >= 1.0f)
        {
            _animator.ResetTrigger(triggerName);
            _isTriggerSet = false; 
            monsterController.SendSkillEndPacket(monsterController.Skill);
            monsterController.Skill = MonsterSkill.MsNone;
            monsterController.State = CreatureState.Idle;
            return NodeStatus.Success; 
        }

        if (_isTriggerSet == false)
        {
            _animator.SetTrigger(triggerName);
            _isTriggerSet = true;
            Debug.Log("애니메이션 시작");
        }
        return NodeStatus.Running;
    }

    public override void HandleStateChange(CreatureState newState)
    {

    }

}