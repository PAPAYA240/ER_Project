using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;


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
    protected string exitAnimName = "WAIT";

    protected bool Check(GameObject owner)
    {
        if (monsterController == null)
            monsterController = owner.GetComponentInChildren<MonsterController>();


        if (_animator == null)
            _animator = owner.GetComponentInChildren<Animator>();

        if (_navMeshAgent == null)
            _navMeshAgent = owner.GetComponentInChildren<NavMeshAgent>();

        return (monsterController != null && _animator != null);
    }
    public abstract void HandleStateChange(CreatureState newState);

}

public class PlayAnimation : AnimationControlNode
{
    public string animName;
    public List<string> chainAnimNames;
    public float ratio = 0.1f;

    private bool _hasStarted = false;
    private int _currentChainIndex = 0;
    private string _currentAnimName;

    public override NodeStatus Execute(GameObject owner)
    {
        if (!Check(owner))
            return NodeStatus.Failure;

        if (monsterController.State == CreatureState.Idle)
        {
            ClearAnim();
            return NodeStatus.Success;
        }

        if (!_hasStarted)
        {
            _currentAnimName = animName;
            _currentChainIndex = 0;
            _hasStarted = true;

            Play(_currentAnimName);
            return NodeStatus.Running;
        }

        if (monsterController._monsterType == MonsterType.Drone)
            Debug.Log($"{monsterController.State}");

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(_currentAnimName))
        {
            if (stateInfo.normalizedTime >= 0.95f)
            {
                if (chainAnimNames == null)
                    return NodeStatus.Running;

                if (_currentChainIndex < chainAnimNames.Count)
                {
                    _currentAnimName = chainAnimNames[_currentChainIndex];
                    _currentChainIndex++;

                    Play(_currentAnimName);
                    return NodeStatus.Running;
                }
            }
        }
        return NodeStatus.Running;
    }

    private void Play(string anim)
     => _animator.CrossFadeInFixedTime(anim, ratio);

    private void ClearAnim()
    {
        if (_animator == null) return;

        _animator.CrossFadeInFixedTime(exitAnimName, ratio);
        _currentAnimName = string.Empty;
        _hasStarted = false;
        _currentChainIndex = 0;
    }
    public override void HandleStateChange(CreatureState newState)
    {
        ClearAnim();
    }
}

#region 조건 Anim
// 애니메이션 재생 노드
public class PlayAnimatorBoolNode : AnimationControlNode
{
    public string paramName;
    public string stateName;
    public override NodeStatus Execute(GameObject owner)
    {
        if (Check(owner) == false)
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
            _isSentEndPacket = true;
            _animator.SetTrigger(triggerName);

            if (bLoop)
                _animator.SetBool(boolName, true);

            return NodeStatus.Running;
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
            //_animator.CrossFade("Idle", 0.1f);
            _isSentEndPacket = false;

            if (bLoop)
                _animator.SetBool(boolName, false);
        }
    }
}
#endregion