
namespace Server.Game
{
    public abstract class Room : JobSerializer
    {        
        public int RoomId { get; set; }

        System.Timers.Timer _timer;

        public virtual void Update()
        {
            
        }

        public abstract void CheckLastPing();

        public void StartTick(int tick = 100)
        {
            _timer = new System.Timers.Timer();
            _timer.Interval = tick;
            _timer.Elapsed += ((s, e) => { Update(); });
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public void StopTick()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }
}
