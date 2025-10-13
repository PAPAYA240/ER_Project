using UnityEngine;

public class FxController : MonoBehaviour
{
    protected bool bTrigger = false;
    protected void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Projectile") ||
            other.gameObject.layer == LayerMask.NameToLayer("FX"))
        {
            Debug.Log("FX Controller Trigger : Projectile");
            bTrigger = true;
        }
    }
}
