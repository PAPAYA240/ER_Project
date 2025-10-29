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

    public sealed partial class PositionInfo
    {
        public PositionInfo(Vector3 vec) : this()
        {
            posX_ = vec.x;
            posY_ = vec.y;
            posZ_ = vec.z;
        }

        public Vector3 ToVector()
        {
            return new Vector3(posX_, posY_, posZ_);
        }

        public static implicit operator PositionInfo(Vector3 vec)
        {
            return new PositionInfo(vec);
        }
    }

    public sealed partial class SkillHitbox
    {
        public void SetDefaultsIfEmpty()
        {
            if (Fps == 0)
                Fps = 30;
        }
    }
}
