using Google.Protobuf.Protocol;
using UnityEngine;

public abstract class AnimationControlNode : ActionNode
{
    protected Animator _animator;
}
// 애니메이션 재생 노드
public class PlayAnimatorBoolNode : AnimationControlNode
{
    public string paramName;
    public bool valueToSet;

    public override NodeStatus Execute(GameObject owner)
    {
        if (_animator == null)
        {
            _animator = owner.GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogError("No Animation");
                return NodeStatus.Failure;
            }
        }
        MonsterController monsterController = owner.GetComponentInChildren<MonsterController>();
        if (monsterController == null)
            return NodeStatus.Failure;

        if (string.IsNullOrEmpty(paramName))
        {
            Debug.LogError("No param");
            return NodeStatus.Failure;
        }

        Debug.Log("Animation Moving");
        _animator.SetBool(paramName, valueToSet);
        return NodeStatus.Success; 
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
        if (_animator == null)
        {
            _animator = owner.GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogError("No Animation");
                return NodeStatus.Failure;
            }
        }
        _animator.SetBool("walk", false);

        if (string.IsNullOrEmpty(triggerName) || string.IsNullOrEmpty(animationStateName))
        {
            Debug.LogError("Trigger Name 또는 Animation State Name이 설정되지 않았습니다.");
            return NodeStatus.Failure;
        }
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        MonsterController monsterController = owner.GetComponentInChildren<MonsterController>();
        if (monsterController == null)
            return NodeStatus.Failure;
       
        // 현재 우리가 의도한 스킬 애니메이션이 재생 중이고, 거의 끝났다면 성공 처리
        if (monsterController.isAnimEnd)
        {
            Debug.Log("스킬 끝");
            monsterController.isAnimEnd = false;
            _isTriggerSet = false; 
            return NodeStatus.Success;
        }

        if (!_isTriggerSet)
        {
            Debug.Log($"{triggerName} 스킬 시작");

            _animator.SetTrigger(triggerName);
            _isTriggerSet = true;
        }
        return NodeStatus.Running;
    }
}