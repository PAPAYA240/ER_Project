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
    public enum Beacon { BEACON_LEFT, BEACON_CENTER, BEACON_RIGHT, BEACON_END }
    
    class BeaconData
    {
        public int OccupiedTeam;    // 점령 완료한 팀 (0: 중립)
        public int OperatingTeam;   // 점령 시도 중인 팀 (0: 없음)
        public float RemainingTime; // 남은 점령 시간 (초)
        public int LastSentTime; // UI에 표시되는 시간
    }

    public class BeaconManager
    {
        readonly float _beaconDist = 3.0f;

        Vector3[] _positions =
        {
            new Vector3(0, 0, 21),    // BEACON_LEFT
            new Vector3(0, 0, 0),     // BEACON_CENTER
            new Vector3(0, 0, -21),    // BEACON_RIGHT
        };

        ConcurrentDictionary<Beacon, BeaconData> _beacons = new ConcurrentDictionary<Beacon, BeaconData>
        {
            [Beacon.BEACON_LEFT] = new BeaconData { OccupiedTeam = 0, OperatingTeam = 0, RemainingTime = 40f },
            [Beacon.BEACON_CENTER] = new BeaconData { OccupiedTeam = 0, OperatingTeam = 0, RemainingTime = 40f },
            [Beacon.BEACON_RIGHT] = new BeaconData { OccupiedTeam = 0, OperatingTeam = 0, RemainingTime = 40f },
        };

        public Vector3 GetBeaconPos(Beacon beacon)
        {
            return _positions[(int)beacon];
        }

        public bool IsInRange(Vector3 playerPos, Beacon beacon)
        {
            if (beacon == Beacon.BEACON_END)
                return false;

            float dist = Vector3.Distance(playerPos, _positions[(int)beacon]);
            return dist <= _beaconDist;
        }

        public bool IsOperatable(int team, Beacon beacon)
        {
            if (_beacons[beacon].OperatingTeam == 0)
                return true;

            return false;
        }

        public bool IsOccupiable(int team, Beacon beacon)
        {
            if (_beacons[beacon].OccupiedTeam == team)
                return false;

            return true;
        }

        public void Update(GameRoom room)
        {
            foreach(var kvp in _beacons)
            {
                Beacon beacon = kvp.Key;
                BeaconData data = kvp.Value;

                if (data.OccupiedTeam == 0)
                    continue;

                data.RemainingTime -= TimeUtil.DeltaTime;

                int currentTime = (int)Math.Ceiling(data.RemainingTime);
                if (currentTime != data.LastSentTime)
                {
                    data.LastSentTime = currentTime;

                    S_ChangeBeaconTime changeBeaconTimePkt = new S_ChangeBeaconTime()
                    {
                        Beacon = (int)beacon,
                        Time = currentTime
                    };

                    room.Push(room.Broadcast, changeBeaconTimePkt);
                }

                if (data.RemainingTime <= 0f)
                {
                    // 상대 점수 차감 + 초기화
                    int enemyTeam = (data.OccupiedTeam == 1) ? 2 : 1;
                    int newScore = room.ReduceScore(enemyTeam, 1);

                    S_ChangeScore changeScorePkt = new S_ChangeScore()
                    {
                        Team = enemyTeam,
                        Score = newScore
                    };
                    room.Push(room.Broadcast, changeScorePkt);

                    data.RemainingTime = 40f;
                }
            }
        }

        public void OccupyBeacon(Player player)
        {
            if (player == null || player.Beacon == Beacon.BEACON_END)
                return;

            _beacons[player.Beacon].OccupiedTeam = player.Team;

            S_OccupyBeacon occupyBeaconPkt = new S_OccupyBeacon();
            occupyBeaconPkt.Team = player.Team;
            occupyBeaconPkt.BeaconName = FormatBeaconNameShort(player.Beacon);
            player.Room.Push(player.Room.Broadcast, occupyBeaconPkt);

            Console.WriteLine($"L: {_beacons[Beacon.BEACON_LEFT].OccupiedTeam}, " +
                $"C: {_beacons[Beacon.BEACON_CENTER].OccupiedTeam}, " +
                $"R: {_beacons[Beacon.BEACON_RIGHT].OccupiedTeam}");
        }

        public static string FormatBeaconNameShort(Beacon beacon)
        {
            string name = beacon.ToString().ToLower(); // "beacon_left"

            int underscoreIndex = name.IndexOf('_');
            string shortName;

            if (underscoreIndex >= 0 && underscoreIndex + 1 < name.Length)
                shortName = name.Substring(underscoreIndex + 1); // '_' 뒤 문자열
            else
                shortName = name;

            // 첫 글자만 대문자
            char[] chars = shortName.ToCharArray();
            chars[0] = char.ToUpper(chars[0]);

            return new string(chars); // "Left"
        }

        public void Operate(Player player)
        {
            if (player == null || player.Beacon == Beacon.BEACON_END)
                return;

            _beacons[player.Beacon].OperatingTeam = player.Info.Player.Team;
        }

        public void ExitOperate(Player player)
        {
            if (player == null || player.Beacon == Beacon.BEACON_END)
                return;

            _beacons[player.Beacon].OperatingTeam = 0;
        }
    }
}
