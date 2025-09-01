using UnityEngine;

// 애니메이션 재생 노드

public class PlayAnimationNode : ActionNode
{
    [Tooltip("Trigger 이름 작성하기")]
    public string triggerName;

    public override NodeStatus Execute(GameObject obj)
    {
        if (string.IsNullOrEmpty(triggerName))
            return NodeStatus.Failure;
        Debug.Log($"[BT] PlayAnimationNode 실행! 트리거:{ triggerName} ");
        Animator animator = obj.GetComponent<Animator>();
        if(animator == null)
            return NodeStatus.Failure;

        animator.SetTrigger(triggerName);
        return NodeStatus.Success;
    }
}
