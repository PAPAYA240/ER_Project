using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FxController : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Projectile") ||
            other.gameObject.layer == LayerMask.NameToLayer("FX"))
        {
            Debug.Log("FX Controller Trigger : Projectile");
        }
    }
}
