using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_SelectEvent : MonoBehaviour
{
    public void OnButtonYukiClick()
    {
        C_Character pickPacket = new C_Character();
        pickPacket.CharType = CharacterType.Yuki;
        Managers.Network.Send(pickPacket);

        Managers.Scene.LoadScene(Define.Scene.Game);
    }

    public void OnButtonRozziClick()
    {
        C_Character pickPacket = new C_Character();
        pickPacket.CharType = CharacterType.Rozzi;
        Managers.Network.Send(pickPacket);

        Managers.Scene.LoadScene(Define.Scene.Game);
    }
}
