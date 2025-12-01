using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using Server.Game;
using ServerCore;
using static System.Collections.Specialized.BitVector32;

namespace Server
{
	public class ClientSession : PacketSession
	{
		public Player MyPlayer { get; set; }
		public int SessionId { get; set; }

		public CharacterType MyCharacter { get; set; }

		public TraitType TraitType { get; set; }

		public Weapon WeaponType { get; set; }

		public int CurRoom {  get; set; }
		public int PickIdx { get; set; }

		public int Team { get; set; }

		public DateTime LastPing { get; set; } = DateTime.Now;

		public string UserName {  get; set; }

        public void Send(IMessage packet)
        {
            try
            {
                // 1) 크기 출력
                int calcSize = packet.CalculateSize();
                Console.WriteLine($"[DEBUG] Sending {packet.GetType().Name}, CalcSize={calcSize}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] CalculateSize FAILED: {ex}");
                Console.WriteLine($"[PACKET DUMP]\n{packet}");
                throw;
            }

            try
            {
                // 2) 패킷 직렬화 시도
                byte[] body = packet.ToByteArray(); // 여기서 주로 터짐

                Console.WriteLine($"[DEBUG] ToByteArray OK: {body.Length} bytes");

                // 3) 정상 직렬화되면 이후 로직 수행
                ushort size = (ushort)body.Length;
                byte[] sendBuffer = new byte[size + 4];

                Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, 2);
                string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
                MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
                Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, 2);
                Array.Copy(body, 0, sendBuffer, 4, size);

                Send(new ArraySegment<byte>(sendBuffer));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ToByteArray FAILED: {ex}");
                Console.WriteLine("=========== PACKET DUMP START =============");
                Console.WriteLine(packet.ToString());
                Console.WriteLine("=========== PACKET DUMP END ===============");
                throw;
            }
        }


        public override void OnConnected(EndPoint endPoint)
		{
			Console.WriteLine($"OnConnected : {endPoint}");

            // PROTO Test

        }

		public override void OnRecvPacket(ArraySegment<byte> buffer)
		{
			PacketManager.Instance.OnRecvPacket(this, buffer);
		}

		public override void OnDisconnected(EndPoint endPoint)
		{
            Room room = RoomManager.Instance.Find(CurRoom);
			if(room is GameRoom gr)
			{
                gr.Push(gr.LeaveGame, MyPlayer.Info.ObjectId);
            }
			else if(room is PickRoom pr)
			{
				pr.Push(pr.LeavePick, PickIdx);
			}
			else if(room is LobbyRoom lr)
			{
				lr.Push(lr.LeaveLobby, UserName);
				lr.Push(lr.BroadcastPlayerCntPkt);
			}

			SessionManager.Instance.Remove(this);

			Console.WriteLine($"OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}

		public bool CheckTimeout(int secs = 10)
		{
			return (DateTime.Now - LastPing).TotalSeconds > secs;
		}
	}
}
