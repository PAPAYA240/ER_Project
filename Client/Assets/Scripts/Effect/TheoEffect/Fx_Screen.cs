using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fx_Screen : FxController
{
    void Update()
    {
        if (bTrigger)
        {
            StartCoroutine(EffectPlay());
            bTrigger = false;
        }
    }
    
    IEnumerator EffectPlay()
    {
        Util.FindChildByName(transform, "FX_QSkill_Indicator").SetActive(true);

        yield return new WaitForSeconds(3.0f);
        Util.FindChildByName(transform, "FX_QSkill_Indicator").SetActive(false);

    }
}
