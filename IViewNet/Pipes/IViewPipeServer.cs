using System;
using System.IO;
using System.IO.Pipes;
using System.ServiceModel.Description;
using System.Windows.Forms;

namespace IViewNet.Pipes
{
    public class IViewPipeServer
    {
        #region "Fields"
        private PipeConfig PipeServerConfig;
        private MemoryStream MessageStore;
        private NamedPipeServerStream Pipe;
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
        private void OnClientConnected()
        {
            ClientConnectedEvent?.Invoke();
        }
        private void OnClientDisconnected()
        {
            ClientShutdownEvent?.Invoke();
        }
        private void OnClientReceive(byte[] Message)
        {
            ClientReceiveEvent?.Invoke(Message);
        }
        private void OnClientSend(byte[] Message)
        {
            ClientSendEvent?.Invoke(Message);
        }
        private void OnClientException(Exception Error)
        {
            ClientExceptionEvent?.Invoke(Error);
        }
        #endregion

        public IViewPipeServer(PipeConfig PipeServerConfig)
        {
            this.PipeServerConfig = PipeServerConfig;
            Initialize();
        }

        public void StartPipeServer()
        {
            try
            {
                Pipe.BeginWaitForConnection(new AsyncCallback(WaitForConnectionCallBack), null);
            }
            catch (Exception ex)
            {
                OnClientException(ex);
                ShutdownServer();
            }
        }

        public void ShutdownServer()
        {
            lock (ShutdownLock)
            {
                if (IsPipeShutdown == false)
                {
                    if (IsPipeConnected)
                    {
                        Pipe.Disconnect();
                    }
                    Pipe.Close();
                    Pipe.Dispose();
                    IsPipeShutdown = true;
                    OnClientDisconnected();
                }
            }
        }

        public void SendMessage(byte[] Message)
        {
            if (Pipe.IsConnected)
            {
                Pipe.Write(Message, 0, Message.Length);
                OnClientSend(Message);
            }
        }

        #region "Private methods"
        private void Initialize()
        {
            Pipe = new NamedPipeServerStream("IViewServerManager", PipeDirection.InOut, PipeServerConfig.GetMaxNumOfServers(), PipeTransmissionMode.Message, PipeOptions.Asynchronous);

            MessageStore = new MemoryStream();

            InnerBuffer = new byte[PipeServerConfig.GetBufferSize()];

            ShutdownLock = new object();

            IsPipeShutdown = false;
        }

        private void WaitForConnectionCallBack(IAsyncResult Ar)
        {
            try
            {
                Pipe.EndWaitForConnection(Ar);
                if (IsPipeConnected)
                {
                    OnClientConnected();
                    StartReceivingData();
                }
            }
            catch (Exception ex)
            {
                OnClientException(ex);
                ShutdownServer();
            }
        }

        private void StartReceivingData()
        {
            if (IsPipeConnected)
            {
                try
                {
                    Pipe.BeginRead(InnerBuffer, 0, InnerBuffer.Length, new AsyncCallback(StartReceivingDataCallBack), null);
                }
                catch (Exception ex)
                {
                    OnClientException(ex);
                    ShutdownServer();
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
                    ShutdownServer();
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
                ShutdownServer();
            }
        }
        #endregion
    }
}