using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum Character
    {
        Rozzi,
        Yuki,
    }

    public enum Scene
    {
        Unknown,
        Login,
        Lobby,
        Pick,
        Game,
    }

    public enum Object
    {
        Unknown,
        MyPlayer,
        OtherPlayer,
        Monster,
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

    public enum Key { Q, W, E, R, T }

    //public enum Trait 
    //{
    //    FrailtyInfliction,  //취약
    //    VampiricBloodline,  //흡혈마
    //    Adrenaline,         //아드레날린
    //    Accelerator,        //엑셀러레이터
    //    StellarCharge,      //스텔라차지
    //    GhostLight,         //도깨비불
    //    RedSprite,          //벽력
    //    SiphonMaelstrom,    //와류
    //    DiamondShard,       //금강
    //    Ironclad,           //불괴
    //    HeavyKneepads,      //빛의 수호
    //    BitterRetribution,  //응징
    //    HealingFactor,      //초재생
    //    AmplificationDrone, //증폭 드론 
    //    HealingDrone,       //치유 드론 
    //    Sentinel            // 헌신
    //}
}
