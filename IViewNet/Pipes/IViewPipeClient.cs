using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace IViewNet.Pipes
{
    public class IViewPipeClient
    {
        #region "Private Feilds"
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
        public delegate void PipeConnectedDelegate();
        public event PipeConnectedDelegate PipeConnectedEvent;

        public delegate void PipeReceivedDelegate(byte[] Message);
        public event PipeReceivedDelegate PipeReceivedEvent;

        public delegate void PipeSentDelegate(byte[] Message);
        public event PipeSentDelegate PipeSentEvent;

        public delegate void PipeClosedDelegate();
        public event PipeClosedDelegate PipeClosedEvent;

        public delegate void PipeExpectionDelegate(Exception Error);
        public event PipeExpectionDelegate PipeExceptionEvent;
        #endregion

        #region "Events Handlers"
        private void SetOnPipeConnected()
        {
            PipeConnectedEvent?.Invoke();
        }
        private void SetOnPipeReceived(byte[] Message)
        {
            PipeReceivedEvent?.Invoke(Message);
        }
        private void SetOnPipeSent(byte[] Message)
        {
            PipeSentEvent?.Invoke(Message);
        }
        private void SetOnPipeClosed()
        {
            PipeClosedEvent?.Invoke();
        }
        private void SetOnPipeException(Exception Error)
        {
            if (IsPipeShutdown == false)
            {
                PipeExceptionEvent?.Invoke(Error);
            }
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
                SetOnPipeConnected();
                StartReceivingData();
            }
            catch (Exception ex)
            {
                SetOnPipeException(ex);
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
                    SetOnPipeSent(Message);
                }
                catch (Exception ex)
                {
                    SetOnPipeException(ex);
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
                    SetOnPipeClosed();
                }
            }
        }

        #region "Private Methods"
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
                    SetOnPipeException(ex);
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
                        SetOnPipeReceived(MessageStore.ToArray());
                        MessageStore.Position = 0;
                        MessageStore.SetLength(0);
                    }
                    StartReceivingData();
                }
            }
            catch (Exception ex)
            {
                SetOnPipeException(ex);
                ShutdownClient();
            }
        }
        #endregion

    }
}