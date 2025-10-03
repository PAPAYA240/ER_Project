using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class WeaponController : BaseController
{
    private CreatureController _owner;
    protected override void Init()
    {
        base.Init();
    }

    #region Animation
    private void SendAnimPacket(string name, float ratio)
    {
        C_Anim animPacket = new C_Anim() { AnimInfo = new AnimInfo() { Name = name, Ratio = ratio } };
        Managers.Network.Send(animPacket);
    }
    public void PlayAnimation(string animName, float ratio)
    {
        _animator.CrossFadeInFixedTime(animName, ratio);
        SendAnimPacket(animName, ratio);
    }
}
#endregion
