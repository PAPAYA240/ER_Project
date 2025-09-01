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

        if (string.IsNullOrEmpty(paramName))
        {
            Debug.LogError("No param");
            return NodeStatus.Failure;
        }

        _animator.SetBool(paramName, valueToSet);
        return NodeStatus.Success; 
    }
}

public class PlayAnimatorTriggerNode : AnimationControlNode
{
    [Tooltip("Animator에 설정된 Trigger 이름")]
    public string triggerName; 

    [Tooltip("애니메이션 상태의 이름")]
    public string animationStateName;
    private bool _isTriggerSet = false; 

    public override NodeStatus Execute(GameObject owner)
    {
        // Animator 컴포넌트를 처음 사용할 때 한 번만 찾아서 캐싱
        if (_animator == null)
        {
            _animator = owner.GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                Debug.LogError("No Animation");
                return NodeStatus.Failure;
            }
        }

        if (string.IsNullOrEmpty(triggerName) || string.IsNullOrEmpty(animationStateName))
        {
            Debug.LogError("No Trigger Animation");
            return NodeStatus.Failure;
        }

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(animationStateName))
        {
            if (stateInfo.normalizedTime >= 1.0f)
            {
                _isTriggerSet = false;
                return NodeStatus.Success;
            }
            else
            {
                return NodeStatus.Running;
            }
        }
        else
        {
            if (!_isTriggerSet)
            {
                Debug.Log($"[BT] PlayAnimationNode 실행! 트리거:{triggerName}");
                _animator.SetTrigger(triggerName);
                _isTriggerSet = true;
            }
            else
            { 
                 _isTriggerSet = false;
                 return NodeStatus.Success;
            }
            return NodeStatus.Running;
        }
    }
}