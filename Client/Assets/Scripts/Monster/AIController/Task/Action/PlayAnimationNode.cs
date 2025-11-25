using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif

public interface IStateChangeListener
{
    void HandleStateChange(CreatureState newState, bool isClear = true);
}

public abstract class AnimationControlNode : ActionNode, IStateChangeListener
{
    protected Animator _animator;
    protected MonsterController _controller;
    protected NavMeshAgent _agent;
    protected string _waitAnim = "WAIT";

    protected readonly int MoveVelocityHash = Animator.StringToHash("moveVelocity");
    protected bool _hasMoveVelocityParam = false;

    protected bool Check(GameObject owner)
    {
        if (_controller == null)
            _controller = owner.GetComponentInChildren<MonsterController>();

        if (_animator == null)
            _animator = owner.GetComponentInChildren<Animator>();

        if (_agent == null)
            _agent = owner.GetComponentInChildren<NavMeshAgent>();

        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            if (param.nameHash == MoveVelocityHash &&
                param.type == AnimatorControllerParameterType.Float)
            {
                _hasMoveVelocityParam = true;
                break;
            }
        }


        return (_controller != null && _animator != null);
    }
    public abstract void HandleStateChange(CreatureState newState, bool isClear = true);
}

public class PlayAnimation : AnimationControlNode
{
    public List<string> chainAnimNames;

    private bool _animStart = false;
    private int _currentChainIndex = 0;
    private string _currentAnimName;

    public override NodeStatus Execute(GameObject owner)
    {
        if (!Check(owner))
            return NodeStatus.Failure;
       
        if (!_animStart)
        {
            if (_hasMoveVelocityParam)
                _animator.SetFloat(MoveVelocityHash, 0);

            _animStart = true;
            _currentChainIndex = 0;
            Play(chainAnimNames[_currentChainIndex]);
            return NodeStatus.Running;
        }


        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(_currentAnimName))
        {
            if (stateInfo.normalizedTime >= 0.95f)
            {
                ++_currentChainIndex;

                // 애니메이션 끝
                if (_currentChainIndex >= chainAnimNames.Count)
                    return NodeStatus.Success;

                // 다음 애니메이션 재생
                Play(chainAnimNames[_currentChainIndex]);
                return NodeStatus.Running;
            }
        }
        return NodeStatus.Running;
    }

    private void Play(string anim)
    {
        _currentAnimName = anim;

        int animHash = Animator.StringToHash(anim);
        if (_animator.HasState(0, animHash))
        {
            _animator.CrossFadeInFixedTime(anim, 0.1f, 0);
        }
        else
        {
            if(_controller.Type == MonsterType.Gamma)
                Debug.LogError($"{_controller.Type}에 {anim} 상태가 없습니다!"); 
        }
    }

    public void ClearAnimationRunState()
    {
        _currentAnimName = string.Empty;
        _currentChainIndex = 0;
        _animStart = false;
    }

    private void ClearAnim()
    {
        _animator?.CrossFadeInFixedTime(_waitAnim, 0.1f, 0);
        _currentAnimName = string.Empty;
        _currentChainIndex = 0;
        _animStart = false;
    }
    public override void HandleStateChange(CreatureState newState, bool isClear = true)
    {
        if (isClear) // 애니메이션 초기화할 것인가?
        {
            ClearAnim();
        }
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
            _animator.SetBool(paramName, false);
            return NodeStatus.Success;
        }

        _animator.SetBool(paramName, true);
        return NodeStatus.Running;
    }

    public override void HandleStateChange(CreatureState newState, bool isClear = true)
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

        if (_controller.State != CreatureState.Moving)
        {
            if (_hasMoveVelocityParam)
                _animator.SetFloat(MoveVelocityHash, 0);
            return NodeStatus.Failure;
        }

        float speed = 0;
        if (!_isFirstFrame)
        {
            float distance = Vector3.Distance(owner.transform.position, _lastPos);

            speed = distance / Time.deltaTime;
        }

        _isFirstFrame = false;

        if (_hasMoveVelocityParam)
            _animator.SetFloat(MoveVelocityHash, speed);

        _lastPos = owner.transform.position;

        return NodeStatus.Running;
    }
    public override void HandleStateChange(CreatureState newState, bool isClear = true) { }
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

    public override void HandleStateChange(CreatureState newState, bool isClear = true)
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