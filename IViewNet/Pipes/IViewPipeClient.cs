using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace IViewNet.Pipes
{
    public class IViewPipeClient
    {
        #region "Feilds"
        private PipeConfig PipeClientConfig;
        private MemoryStream MessageStore;
        private NamedPipeClientStream Pipe;
        private byte[] InnerBuffer;
        private object ShutdownLock;
        #endregion

        #region "Properties"
        public bool IsPipeConnected
        {
            get
            {
                if (Pipe != null)
                {
                    return Pipe.IsConnected;
                }
                return false;
            }
        }

        public bool IsPipeShutdown { get; private set; }
        #endregion

        #region "Events"
        public delegate void ConnectedDelegate();
        public event ConnectedDelegate ClientConnectedEvent;

        public delegate void ShutdownDelegate();
        public event ShutdownDelegate ClientShutdownEvent;

        public delegate void ReceivedDelegate(byte[] Message);
        public event ReceivedDelegate ClientReceiveEvent;

        public delegate void SendDelegate(byte[] Message);
        public event SendDelegate ClientSendEvent;

        public delegate void ExpectionDelegate(Exception Error);
        public event ExpectionDelegate ClientExceptionEvent;
        #endregion

        #region "Events Handlers"
        public void OnClientConnected()
        {
            ClientConnectedEvent?.Invoke();
        }

        public void OnClientDisconnected()
        {
            ClientShutdownEvent?.Invoke();
        }

        public void OnClientReceive(byte[] Message)
        {
            ClientReceiveEvent?.Invoke(Message);
        }

        public void OnClientSend(byte[] Message)
        {
            ClientSendEvent?.Invoke(Message);
        }

        public void OnClientException(Exception Error)
        {
            ClientExceptionEvent?.Invoke(Error);
        }
        #endregion

        public IViewPipeClient(PipeConfig PipeClientConfig)
        {
            this.PipeClientConfig = PipeClientConfig;
            InitializePipeClient();
        }

        public async Task AttemptToConnect()
        {
            try
            {
                if (IsPipeShutdown == true && IsPipeConnected == false)
                {
                    InitializePipeClient();
                }
                await Pipe.ConnectAsync(200);
                OnClientConnected();
                StartReceivingData();
            }
            catch (Exception ex)
            {
                OnClientException(ex);
                ShutdownClient();
            }
        }

        public void SendMessage(byte[] Message)
        {
            if (Pipe.IsConnected)
            {
                try
                {
                    Pipe.Write(Message, 0, Message.Length);
                    OnClientSend(Message);
                }
                catch (Exception ex)
                {
                    OnClientException(ex);
                    ShutdownClient();
                }
            }
        }

        public void ShutdownClient()
        {
            lock (ShutdownLock)
            {
                if (IsPipeShutdown == false)
                {
                    Pipe.Close();
                    Pipe.Dispose();
                    IsPipeShutdown = true;
                    OnClientDisconnected();
                }
            }
        }

        #region "Private methods"
        private void InitializePipeClient()
        {
            Pipe = new NamedPipeClientStream(".", "IViewServerManager", PipeDirection.InOut);

            MessageStore = new MemoryStream();

            InnerBuffer = new byte[PipeClientConfig.GetBufferSize()];

            ShutdownLock = new object();

            IsPipeShutdown = false;
        }

        private void StartReceivingData()
        {
            if (Pipe.IsConnected)
            {
                try
                {
                    Pipe.BeginRead(InnerBuffer, 0, InnerBuffer.Length, new AsyncCallback(StartReceivingDataCallBack), null);
                }
                catch (Exception ex)
                {
                    OnClientException(ex);
                    ShutdownClient();
                }
            }
        }

        private void StartReceivingDataCallBack(IAsyncResult Ar)
        {
            try
            {
                int BytesReceived = Pipe.EndRead(Ar);

                if (BytesReceived <= 0)
                {
                    ShutdownClient();
                }
                else
                {
                    MessageStore.Write(InnerBuffer, 0, BytesReceived);

                    if (Pipe.IsMessageComplete)
                    {
                        OnClientReceive(MessageStore.ToArray());
                        MessageStore.Position = 0;
                        MessageStore.SetLength(0);
                    }
                    StartReceivingData();
                }
            }
            catch (Exception ex)
            {
                OnClientException(ex);
                ShutdownClient();
            }
        }
        #endregion
    }
}