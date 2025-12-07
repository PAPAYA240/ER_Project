using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Env_BarrierSpawnpoint : EnvController
{
    private GameObject _phase2Object;
    void Start()
    {
        Transform phase2Transform = transform.Find("Phase2");

        if (phase2Transform != null)
        {
            _phase2Object = phase2Transform.gameObject;
            _phase2Object.SetActive(true);
        }
        else
            UnityEngine.Debug.LogError("Phase2 오브젝트를 찾을 수 없습니다");

    }

    public void ActivatePhase2(bool active = false)
    {
        if (_phase2Object != null)
            _phase2Object.SetActive(active);
    }
}
