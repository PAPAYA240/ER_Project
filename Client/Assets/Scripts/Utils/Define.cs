using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum Scene
    {
        Unknown,
        Login,
        Lobby,
        Pick,
        Game,
    }

    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,
    }

    public enum UIEvent
    {
        Click,
        PointerEnter,
        PointerExit,
        BeginDrag,
        Drag,
        EndDrag,
    }

    public enum CameraMode
    {
        QuaterView,
    }

    public enum Layer
    {
        Map = 11,
    }
}
