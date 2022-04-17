using UnityEngine;

namespace Saro.XAsset.Update
{
    public interface INetworkMonitorListener
    {
        void OnReachablityChanged(NetworkReachability reachability);
    }

    public class NetworkMonitorComponent : IService
    {
        public INetworkMonitorListener Listener { get; set; }

        private NetworkReachability m_Reachability;
        private float m_SampleTime = 0.5f;
        private float m_Time;
        private bool m_Started;

        public void Restart()
        {
            m_Time = Time.timeSinceLevelLoad;
            m_Started = true;
        }

        public void Stop()
        {
            m_Started = false;
        }

        void IService.Awake()
        {
            m_Reachability = Application.internetReachability;
            Restart();
        }

        void IService.Update()
        {
            if (m_Started && Time.timeSinceLevelLoad - m_Time >= m_SampleTime)
            {
                var state = Application.internetReachability;
                if (m_Reachability != state)
                {
                    if (Listener != null)
                    {
                        Listener.OnReachablityChanged(state);
                    }
                    m_Reachability = state;
                }
                m_Time = Time.timeSinceLevelLoad;
            }
        }

        void IService.Dispose()
        {
        }
    }
}