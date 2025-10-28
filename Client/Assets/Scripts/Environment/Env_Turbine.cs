using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Env_Turbine : EnvController
{
    protected override void Init()
    {
        base.Init();

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
    }

    protected override void TryHandleInteraction()
    {
    }
}
