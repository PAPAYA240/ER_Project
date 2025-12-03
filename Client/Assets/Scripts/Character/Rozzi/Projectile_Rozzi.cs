using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.GridLayoutGroup;

public class Projectile_Rozzi : Projectile
{
    private BOMB_ROZZI _state = BOMB_ROZZI.NoneBomb;

    private int _layer1Team;
    private int _layer2Team;
    private VisionCircle _visionCircle;

    private void Start()
    {
        Init();
        string layer1Name = $"FogTeam1";
        string layer2Name = $"FogTeam2";
        _layer1Team = LayerMask.NameToLayer(layer1Name);
        _layer2Team = LayerMask.NameToLayer(layer2Name);
    }

    public void ChangeState(S_ProjectileRozzi packet)
    {
        _state = packet.State;
        switch (_state)
        {
            case BOMB_ROZZI.Flying:

                break;
            case BOMB_ROZZI.AttachedToTarget:   // 대상에게 부착!
                {
                    GameObject target = Managers.Object.FindById(packet.TargetId);
                    PlayerController pc = target.GetComponentInChildren<PlayerController>();
                    _visionCircle = target.GetComponentInChildren<VisionCircle>();

                    if(_visionCircle != null && pc != null)
                    {
                        _visionCircle.SetActivate(true);
                        Debug.Log("hi");

                        if(pc.ObjInfo.Player.Team == 1)
                        {
                            _visionCircle.gameObject.layer = _layer2Team;
                        }
                        else
                        {
                            _visionCircle.gameObject.layer = _layer1Team;
                        }
                    }

                    SkillSoundRouter.Play(Owner, KeyCode.R, SkillSoundEvent.ProjectileAttach, transform.position);
                    SkillSoundRouter.Play(Owner, KeyCode.R, SkillSoundEvent.ProjectileCount, transform.position);
                }
                break;
            case BOMB_ROZZI.StuckOnGround:
                SkillSoundRouter.Play(Owner, KeyCode.R, SkillSoundEvent.ProjectileCount, transform.position);
                break;
            case BOMB_ROZZI.Exploded:   // 폭발
                if (_visionCircle != null)
                    _visionCircle.SetActivate(false);
                SkillSoundRouter.Play(Owner, KeyCode.R, SkillSoundEvent.ProjectileExplode, transform.position);
                break;
            default:

                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (Type == ProjectileType.ProjectileRozziR)
            return;
    }
}
