using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Google.Protobuf.Protocol;
using Lucene.Net.Index;

namespace Server.Game
{
    public enum Beacon { BEACON_NONE, BEACON_LEFT, BEACON_CENTER, BEACON_RIGHT }
    
    public class BeaconManager
    {
        Dictionary<Beacon, Vector3> _positions = new Dictionary<Beacon, Vector3>() // 비콘 위치
        {
            { Beacon.BEACON_NONE,   Vector3.Zero },
            { Beacon.BEACON_LEFT,   new Vector3(0, 0, 21) },
            { Beacon.BEACON_CENTER, new Vector3(0, 0, 0) },
            { Beacon.BEACON_RIGHT,  new Vector3(0, 0, -21) }
        };

        ConcurrentDictionary<Beacon, int> _occupation = new ConcurrentDictionary<Beacon, int>(
            new[]
            {
                new KeyValuePair<Beacon, int>(Beacon.BEACON_NONE, 0),
                new KeyValuePair<Beacon, int>(Beacon.BEACON_LEFT, 0),
                new KeyValuePair<Beacon, int>(Beacon.BEACON_CENTER, 0),
                new KeyValuePair<Beacon, int>(Beacon.BEACON_RIGHT, 0)
            });

        ConcurrentDictionary<Beacon, int> _operatingTeam = new ConcurrentDictionary<Beacon, int>(
            new[]
            {
                new KeyValuePair<Beacon, int>(Beacon.BEACON_NONE, 0),
                new KeyValuePair<Beacon, int>(Beacon.BEACON_LEFT, 0),
                new KeyValuePair<Beacon, int>(Beacon.BEACON_CENTER, 0),
                new KeyValuePair<Beacon, int>(Beacon.BEACON_RIGHT, 0)
            });

        readonly float _beaconDist = 3.0f;

        public Vector3 GetBeaconPos(Beacon beacon)
        {
            return _positions[beacon];
        }

        public bool IsInRange(Vector3 playerPos, Beacon beacon)
        {
            if (!_positions.TryGetValue(beacon, out var beaconPos))
                return false;

            float dist = Vector3.Distance(playerPos, beaconPos);
            return dist <= _beaconDist;
        }

        public bool IsOperatable(int team, Beacon beacon)
        {
            if (_operatingTeam[beacon] == 0)
                return true;

            return false;
        }

        public bool IsOccupiable(int team, Beacon beacon)
        {
            if (_occupation[beacon] == team)
                return false;

            return true;
        }

        public void OccupyBeacon(Player player)
        {
            _occupation[player.Beacon] = player.Team;

            S_OccupyBeacon occupyBeaconPkt = new S_OccupyBeacon();
            occupyBeaconPkt.Team = player.Team;
            occupyBeaconPkt.BeaconName = FormatBeaconName(player.Beacon);
            player.Room.Push(player.Room.Broadcast, occupyBeaconPkt);
            Console.WriteLine($"L: {_occupation[Beacon.BEACON_LEFT]}, " +
                $"C: {_occupation[Beacon.BEACON_CENTER]}, " +
                $"R: {_occupation[Beacon.BEACON_RIGHT]}");
        }

        public static string FormatBeaconName(Beacon beacon)
        {
            string name = beacon.ToString().ToLower(); // "beacon_left"
            char[] chars = name.ToCharArray();

            // 첫 번째 글자 대문자 (B)
            if (chars.Length > 0)
                chars[0] = char.ToUpper(chars[0]);

            // 언더스코어('_') 뒤 첫 글자 대문자
            int underscoreIndex = name.IndexOf('_');
            if (underscoreIndex >= 0 && underscoreIndex + 1 < chars.Length)
                chars[underscoreIndex + 1] = char.ToUpper(chars[underscoreIndex + 1]);

            return new string(chars);
        }

        public void Operate(Player player)
        {
            _operatingTeam[player.Beacon] = player.Info.Player.Team;
        }

        public void ExitOperate(Player player)
        {
            _operatingTeam[player.Beacon] = 0;
        }
    }
}
