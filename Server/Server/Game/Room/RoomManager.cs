using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Server.Game
{
    public class RoomManager
    {
        public static RoomManager Instance { get; } = new RoomManager();

        object _lock = new object();
        Dictionary<int, Room> _rooms = new Dictionary<int, Room>();
        int _roomId = 1;

        public GameRoom AddGameRoom()
        {
            GameRoom room = new GameRoom();

            room.Push(room.Init);

            lock (_lock)
            {
                room.RoomId = _roomId;
                _rooms.Add(_roomId, room);
                _roomId++;
                room.StartTick(10);
            }

            return room;
        }

        public PickRoom AddPickRoom()
        {
            PickRoom room = new PickRoom();
            room.Push(room.Init);

            lock (_lock)
            {
                room.RoomId = _roomId;
                _rooms.Add(_roomId, room);
                _roomId++;
                room.StartTick(10);
            }

            return room;
        }

        public bool Remove(int roomId)
        {
            lock (_lock)
            {
                if( _rooms.ContainsKey(roomId) )
                    _rooms[roomId].StopTick();

                return _rooms.Remove(roomId);
            }
        }

        public Room Find(int roomId = 0)
        {
            lock (_lock)
            {
                if (roomId == 0)
                    roomId = _roomId - 1;

                Room room = null;
                if (_rooms.TryGetValue(roomId, out room))
                    return room;

                return null;
            }
        }
    }
}
