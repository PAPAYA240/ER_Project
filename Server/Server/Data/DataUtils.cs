using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Server.Data
{
    public class QuaternionHelper
    {
        // Unity의 Quaternion.Euler(0, y, 0)와 동일
        public static Quaternion FromYRotation(float degrees)
        {
            float radians = degrees * MathF.PI / 180f;
            float halfAngle = radians * 0.5f;

            return new Quaternion(
                x: 0f,
                y: MathF.Sin(halfAngle),
                z: 0f,
                w: MathF.Cos(halfAngle)
            );
        }

        // 2D 방향 벡터를 Y축 회전 Quaternion으로 변환 (Unity 호환)
        public static Quaternion LookRotationY(float dirX, float dirZ)
        {
            // Unity는 Z축이 앞방향이므로 Atan2(x, z) 사용
            float angle = MathF.Atan2(dirX, dirZ);
            float degrees = angle * 180f / MathF.PI;

            return FromYRotation(degrees);
        }

        // Vector2로 LookRotation 계산
        public static Quaternion LookRotationY(Vector2 from, Vector2 to)
        {
            Vector2 dir = to - from;
            return LookRotationY(dir.X, dir.Y);
        }
    }

    public class DataUtils
    {
        public static Dictionary<DataUtils.KeyCode, List<string>> ConvertProtoInteractionsToKeyCodeDictionary(MapField<string, InteractionList> protoInteractions)
        {
            if (protoInteractions == null)
                return new Dictionary<DataUtils.KeyCode, List<string>>();

            return protoInteractions
                .Where(pair => Enum.TryParse(typeof(DataUtils.KeyCode), pair.Key, true, out _))
                .ToDictionary(
                    pair =>
                    {
                        return (DataUtils.KeyCode)Enum.Parse(typeof(DataUtils.KeyCode), pair.Key, true);
                    },
                    pair =>
                    {
                        return new List<string>(pair.Value.Interaction);
                    }
                );
        }

        public enum KeyCode
        {
            None = 0,
            Q = 113,  // UnityEngine.KeyCode.Q
            W = 119,  // UnityEngine.KeyCode.W
            E = 101,  // UnityEngine.KeyCode.E
            R = 114,  // UnityEngine.KeyCode.R
            D = 100,  // UnityEngine.KeyCode.D
            F = 102,  // UnityEngine.KeyCode.F
            T = 116,  // UnityEngine.KeyCode.T
            F1 = 282,
            F2 = 283,
            F3 = 284,
        }
    }
}
