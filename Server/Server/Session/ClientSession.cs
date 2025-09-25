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

		public void Send(IMessage packet)
		{
			string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
			MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
            ushort size = (ushort)packet.CalculateSize();
            byte[] sendBuffer = new byte[size + 4];
            Array.Copy(BitConverter.GetBytes((ushort)size + 4), 0, sendBuffer, 0, sizeof(ushort));
            Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
            Send(new ArraySegment<byte>(sendBuffer));
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

			SessionManager.Instance.Remove(this);

			Console.WriteLine($"OnDisconnected : {endPoint}");
		}

		public override void OnSend(int numOfBytes)
		{
			//Console.WriteLine($"Transferred bytes: {numOfBytes}");
		}

		public bool CheckTimeout(int secs = 5)
		{
			return (DateTime.Now - LastPing).TotalSeconds > secs;
		}
	}
}
