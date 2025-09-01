using UnityEngine;

[RequireComponent(typeof(MonsterController))]
public class MonsterAI : MonoBehaviour
{
    // 루트 노드
    private Node _rootNode;

    private float _tickInterval = 0.2f;
    private float _timer = 0f;

    void Start()
    {
        _rootNode = CreateBehaviorTree();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _tickInterval)
            return;
        _timer = 0f;
        _rootNode?.Execute(this.gameObject);
    }

    private Node CreateBehaviorTree()
    {
        // TODO : 해당 코드를 어떻게 DATA 화 시킬 것인지 생각해 봐야 함
        var attack1Anim = ScriptableObject.CreateInstance<PlayAnimationNode>();
        attack1Anim.triggerName = "tAttack01";

        var attack2Anim = ScriptableObject.CreateInstance<PlayAnimationNode>();
        attack2Anim.triggerName = "tAttack02";

        var isWPressed = ScriptableObject.CreateInstance<IsKeyPressedNode>();
        isWPressed.key = KeyCode.W;

        var builder = new BehaviorTreeBuilder();
        builder.Sequence("Root").Condition(isWPressed).
            Action(attack1Anim).Action(attack2Anim).End(); // ex. 연계 공격
        return builder.Build();
    }
}