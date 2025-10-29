using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Data
{
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
            F1 = 282
        }
    }
}
