using System;
using System.Collections.Generic;
using System.Text;

namespace Google.Protobuf.Protocol
{
    public sealed partial class PositionInfo
    {
        public float Distance(PositionInfo other)
        {
            float dx = PosX - other.PosX;
            float dy = PosX - other.PosX;
            float dz = PosX - other.PosX;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}