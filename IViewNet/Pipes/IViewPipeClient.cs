using IViewNet.Common;
using IViewNet.Common.Models;
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
        public PacketManager PacketManager { get; set; }
        public bool IsPipeShutdown { get; private set; }
        #endregion

        #region "Events"
        public delegate void PipeConnectedDelegate();
        public event PipeConnectedDelegate PipeConnectedEvent;

        public delegate void PipeReceivedDelegate(Packet Message);
        public event PipeReceivedDelegate PipeReceivedEvent;

        public delegate void PipeSentDelegate(Packet Message);
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
        private void SetOnPipeReceived(Packet Message)
        {
            PipeReceivedEvent?.Invoke(Message);
        }
        private void SetOnPipeSent(Packet Message)
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
                await Pipe.ConnectAsync(1000);
                Pipe.ReadMode = PipeTransmissionMode.Message;
                SetOnPipeConnected();
                StartReceivingData();
            }
            catch (Exception ex)
            {
                SetOnPipeException(ex);
                ShutdownClient();
            }
        }

        public void SendMessage(Packet Message)
        {
            if (Pipe.IsConnected)
            {
                try
                {
                    byte[] MessageBytes = Message.ToPacket();
                    Pipe.Write(MessageBytes, 0, MessageBytes.Length);
                    Pipe.Flush();
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
                    if (PacketManager != null)
                    {
                        PacketManager.Dispose();
                    }
                    SetOnPipeClosed();
                }
            }
        }

        #region "Private Methods"
        private void InitializePipeClient()
        {
            Pipe = new NamedPipeClientStream("IViewServerManager");

            PacketManager = new PacketManager();

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
                    Console.WriteLine("CLIENT LESS THAN ZERO");
                    ShutdownClient();
                }
                else
                {
                    MessageStore.Write(InnerBuffer, 0, BytesReceived);

                    if (Pipe.IsMessageComplete)
                    {
                        Packet Message = PacketManager.GetPacket(MessageStore.ToArray(), 0);
                        SetOnPipeReceived(Message);
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