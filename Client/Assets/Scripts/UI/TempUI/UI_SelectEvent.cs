using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_SelectEvent : MonoBehaviour
{
    public void OnButtonYukiClick()
    {
        Managers.Scene.LoadScene(Define.Scene.Game);
        Managers.Object.Character = Define.Character.Yuki;
    }

    public void OnButtonRozziClick()
    {
        Managers.Scene.LoadScene(Define.Scene.Game);
        Managers.Object.Character = Define.Character.Rozzi;
    }
}
