using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FxController : MonoBehaviour
{
    void OnParticleCollision(GameObject other)
    {
        Debug.Log($"파티클이 {other.name}과 충돌했습니다!");
    }
}
