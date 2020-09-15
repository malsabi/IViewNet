using IViewNet.Common;
using IViewNet.Common.Enums;
using IViewNet.Common.Models;
using System;
using System.Net.Sockets;
using System.Threading;

namespace IViewNet.Client
{
    public class Client
    {
        private readonly NetConfig Config;
        private ManualResetEvent WaitHandler;
        private Socket EstablishedConnection;
        private bool IsEstablished;
        private CancellationTokenSource HeartBeatCancellationToken;
        private Operation OperationManager;

        #region "Properties"
        public PacketManager PacketManager { get; private set; }
        public bool IsShutdown { get; private set; }


        public bool IsConnected
        {
            get
            {
                if (OperationManager != null)
                {
                    return OperationManager.IsActive;
                }
                return false;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                if (OperationManager != null)
                {
                    return OperationManager.IsAuthenticated;
                }
                return false;
            }
        }


        #endregion

        #region "Events/Handlers"
        public delegate void OnExceptionDelegate(Exception Ex);
        public event OnExceptionDelegate OnException;

        public delegate void OnClientExceptionDelegate(Operation Client, Exception Ex);
        public event OnClientExceptionDelegate OnClientException;

        public delegate void OnClientConnectDelegate(Operation Client);
        public event OnClientConnectDelegate OnClientConnect;

        public delegate void OnClientDisconnectDelegate(Operation Client, string Reason);
        public event OnClientDisconnectDelegate OnClientDisconnect;

        public delegate void OnClientAuthenticatedDelegate(Operation Client, bool Success);
        public event OnClientAuthenticatedDelegate OnClientAuthenticated;

        public delegate void OnClientReceiveDelegate(Operation Client, Packet Message);
        public event OnClientReceiveDelegate OnClientReceive;

        public delegate void OnClientSendDelegate(Operation Client, Packet Message);
        public event OnClientSendDelegate OnClientSend;
        #endregion

        #region "Event Handlers"
        private void SetOnClientConnect(Operation Client)
        {
            OnClientConnect?.Invoke(Client);
        }
        private void SetOnClientDisconnect(Operation Client, string Reason)
        {
            OnClientDisconnect?.Invoke(Client, Reason);
            ShutdownResult ShutdownResult = Shutdown();
            if (ShutdownResult.IsOperationSuccess)
            {
                Console.WriteLine(ShutdownResult.Message);
            }
            else
            {
                Console.WriteLine(ShutdownResult.Message);
            }
        }

        private void SetOnClientAuthenticated(Operation Client, bool Success)
        {
            OnClientAuthenticated?.Invoke(Client, Success);
        }

        private void SetOnClientReceive(Operation Client, Packet Message)
        {
            if (Message.Code == (int)NetCommands.Unknown)
            {
                SetOnClientDisconnect(Client, "Unknown Client");
            }
            else
            {
                if (Client.IsAuthenticated == false)
                {
                    //GetAuthentication
                    if (Message.Code == (int)NetCommands.GetAuthentication)
                    {
                        StartAuthentication(Client);
                        SetOnClientAuthenticated(Client, true);
                        Client.SetAuthentication(true);
                    }
                    else
                    {
                        SetOnClientAuthenticated(Client, false);
                        SetOnClientDisconnect(Client, "Dropped");
                    }
                }
                else
                {
                    OnClientReceive?.Invoke(Client, Message);
                }
            }
        }
        private void SetOnClientSend(Operation Client, Packet Message)
        {
            OnClientSend?.Invoke(Client, Message);
        }
        private void SetOnClientException(Operation Client, Exception Ex)
        {
            OnClientException?.Invoke(Client, Ex);
        }
        private void SetOnException(Exception Ex)
        {
            OnException?.Invoke(Ex);
        }
        #endregion

        public Client(NetConfig Config)
        {
            this.Config = Config;
            InitializeClient();
        }

        #region "Public Methods"
        public void SendPacket(Packet Packet)
        {
            if (OperationManager != null)
            {
                OperationManager.SendPacket(Packet);
            }
        }

        public EstablishConnectionResult Connect(string Host, int Port)
        {
            EstablishConnectionResult Result;
            try
            {
                if (IsEstablished == false)
                {
                    WaitHandler = new ManualResetEvent(false);
                    EstablishedConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    EstablishedConnection.BeginConnect(Host, Port, new AsyncCallback(ConnectCallBack), null);
                    WaitHandler.WaitOne();

                    if (IsEstablished)
                    {
                        PacketManager = new PacketManager();
                        PacketManager.AddPacket(new Packet(0000, "SetAuthentication", null));
                        PacketManager.AddPacket(new Packet(0001, "GetAuthentication", null));
                        PacketManager.AddPacket(new Packet(0002, "Synchronize", null));
                     
                        OperationManager = new Operation(EstablishedConnection, Config, PacketManager);
                        AddHandlers(OperationManager);
                        SetOnClientConnect(OperationManager);
                        StartHeartBeat();
                        Result = new EstablishConnectionResult(true, "Client Successfully connected to the server");
                    }
                    else
                    {
                        Result = new EstablishConnectionResult(false, "Client Failed to connect to the server");
                    }
                }
                else
                {
                    Result = new EstablishConnectionResult(false, "Cannot preform 'Connect' operation while the client is connected to the server");
                    ShutdownResult ShutdownResult = Shutdown();
                    if (ShutdownResult.IsOperationSuccess)
                    {
                        Console.WriteLine(ShutdownResult.Message);
                    }
                    else
                    {
                        Console.WriteLine(ShutdownResult.Message);
                    }
                }
            }
            catch (Exception Ex)
            {
                Result = new EstablishConnectionResult(false, "Client Failed to connect to the server");
                SetOnException(Ex);
                IsEstablished = false;
            }
            return Result;
        }


        public ShutdownResult Shutdown()
        {
            ShutdownResult Result;
            try
            {
                if (IsShutdown == false)
                {
                    IsShutdown = true;
                    StopHeartBeat();
                    EstablishedConnection.Close();
                    PacketManager.Dispose();
                    WaitHandler.Dispose();
                    RemoveHandlers(OperationManager);
                    OperationManager.ShutdownOperation();
                    Result = new ShutdownResult("Client Shutdown Successfully", "Shutdown", DateTime.Now, true);
                }
                else
                {
                    throw new Exception("Client is not running");
                }
            }
            catch (Exception ex)
            {
                Result = new ShutdownResult(string.Format("Shutdown Exception: {0}", ex.Message), "Shutdown", DateTime.Now, false);
            }
            return Result;
        }

        #endregion

        #region "Private Methods"
        private void InitializeClient()
        {
            IsShutdown = false;
            IsEstablished = false;
            WaitHandler = null;
            EstablishedConnection = null;
            HeartBeatCancellationToken = null;
        }

        #region "HeartBeat"
        private void StartHeartBeat()
        {
            if (HeartBeatCancellationToken == null)
            {
                HeartBeatCancellationToken = new CancellationTokenSource();
            }
            HeartBeat(HeartBeatCancellationToken.Token);
        }
        private void StopHeartBeat()
        {
            if (HeartBeatCancellationToken.IsCancellationRequested == false)
            {
                HeartBeatCancellationToken.Cancel();
            }
        }
        private void HeartBeat(CancellationToken Token)
        {
            new Thread(() => Beat(Token)).Start();
            new Thread(() => Pulse(Token)).Start();
        }
        /// <summary>
        /// Used to check if the client heart is still alive
        /// </summary>
        /// <param name="Token"></param>
        private void Beat(CancellationToken Token)
        {
            while (OperationManager.IsActive)
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }
                else
                {
                    OperationManager.Beat();
                }
                Thread.Sleep(150);
            }
        }
        /// <summary>
        /// Used to send to server a ping for proving its still alive
        /// </summary>
        /// <param name = "Token" ></ param >
        private void Pulse(CancellationToken Token)
        {
            while (OperationManager.IsActive)
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }
                else
                {
                    if (OperationManager.IsAuthenticated)
                    {
                        OperationManager.SendPacket(new Packet((int)NetCommands.Synchronize, NetCommands.Synchronize.ToString(), null));
                    }
                }
                Thread.Sleep(250);
            }
        }
        #endregion
        #region "Call Backs"
        private void ConnectCallBack(IAsyncResult Ar)
        {
            try
            {
                EstablishedConnection.EndConnect(Ar);
            }
            catch (Exception Ex)
            {
                SetOnException(Ex);
                IsEstablished = false;
            }
            finally
            {
                IsEstablished = true;
                WaitHandler.Set();
            }
        }
        #endregion
        #region "Helpers"
        private void RemoveHandlers(Operation Client)
        {
            Client.OnClientDisconnected -= SetOnClientDisconnect;
            Client.OnClientReceive -= SetOnClientReceive;
            Client.OnClientSend -= SetOnClientSend;
            Client.OnClientException -= SetOnClientException;
        }
        private void AddHandlers(Operation Client)
        {
            Client.OnClientDisconnected += SetOnClientDisconnect;
            Client.OnClientReceive += SetOnClientReceive;
            Client.OnClientSend += SetOnClientSend;
            Client.OnClientException += SetOnClientException;
            Client.StartOperation();
        }
        private void StartAuthentication(Operation Client)
        {
            Client.SendPacket(new Packet((int)NetCommands.SetAuthentication, NetCommands.SetAuthentication.ToString(), null));
        }
        #endregion
        #endregion
    }
}