using Google.Protobuf.Protocol;
using UnityEngine;


 public class IsKeyPressedNode : DecoratorNode
 {
     public KeyCode key = KeyCode.W;

    // TODO : _child => 제어할 애니메이션을 집어넣을 예정
    public override NodeStatus Execute(GameObject agent)
     {
        return Input.GetKey(key) ? NodeStatus.Success : NodeStatus.Failure;
    }
 }

