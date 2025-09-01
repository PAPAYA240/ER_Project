using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Google.Protobuf.Protocol
{
    public sealed partial class RotationInfo
    {
        public static implicit operator RotationInfo(Quaternion q)
        {
            return new RotationInfo
            {
                Qx = q.x,
                Qy = q.y,
                Qz = q.z,
                Qw = q.w
            };
        }

        public static implicit operator Quaternion(RotationInfo v)
        {
            if (v == null) return Quaternion.identity;
            return new Quaternion(v.Qx, v.Qy, v.Qz, v.Qw);
        }
    }
}
