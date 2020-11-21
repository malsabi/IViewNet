using IViewNet.Common;
using IViewNet.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IViewNet.Server
{
    public class Server
    {
        private readonly NetConfig Config;
        private Socket Listener;
        private List<IPAddress> BlackListedIP;
        private CancellationTokenSource HeartBeatCancellationToken;
        private object OnlineClientsLock;
        private List<Operation> Clients;


        #region "Properties"
        public Operation[] OnlineClients
        {
            get
            {
                lock (OnlineClientsLock)
                {
                    return Clients.ToArray();
                }
            }
        }
        public PacketManager PacketManager { get; set; }
        public bool IsListening { get; private set; } = false;
        public bool IsShutdown { get; private set; } = false;
        #endregion

        public Server(NetConfig Config)
        {
            this.Config = Config;
            if (this.Config == null)
            {
                this.Config = NetConfig.CreateDefaultServerConfig();
            }
        }

        #region "Events"
        public delegate void OnExceptionDelegate(Exception Ex);
        public event OnExceptionDelegate OnException;

        public delegate void OnClientExceptionDelegate(Operation Client, Exception Ex);
        public event OnClientExceptionDelegate OnClientException;

        public delegate void OnClientConnectDelegate(Operation Client);
        public event OnClientConnectDelegate OnClientConnect;

        public delegate void OnClientDisconnectDelegate(Operation Client, string Reason);
        public event OnClientDisconnectDelegate OnClientDisconnect;

        public delegate void OnClientReceiveDelegate(Operation Client, Packet Message);
        public event OnClientReceiveDelegate OnClientReceive;

        public delegate void OnClientSendDelegate(Operation Client, Packet Packet);
        public event OnClientSendDelegate OnClientSend;

        public delegate void OnClientBlackListDelegate(IPAddress IP, string Reason);
        public event OnClientBlackListDelegate OnClientBlackList;
        #endregion

        #region "Event Handlers"
        private void AcceptEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
        private void SetOnClientConnect(Operation Client)
        {
            OnClientConnect?.Invoke(Client);
        }
        private void SetOnClientDisconnect(Operation Client, string Reason)
        {
            OnClientDisconnect?.Invoke(Client, Reason);
        }
        private void SetOnClientReceive(Operation Client, Packet Message)
        {
            OnClientReceive?.Invoke(Client, Message);
        }
        private void SetOnClientSend(Operation Client, Packet Packet)
        {
            OnClientSend?.Invoke(Client, Packet);
        }
        private void SetOnClientBlackList(IPAddress IP, string Message)
        {
            OnClientBlackList?.Invoke(IP, Message);
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

        #region "Public Methods"
        /// <summary>
        /// Starts Listening for incoming connections
        /// </summary>
        /// <returns></returns>
        public StartListenerResult StartListener()
        {
            StartListenerResult ListenerResult = null;
            try
            {
                OnlineClientsLock = new object();
                Clients = new List<Operation>();

                IPEndPoint ListenerEndPoint = new IPEndPoint(IPAddress.Any, Config.GetPort());

                Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    LingerState = new LingerOption(false, 0),
                    NoDelay = true
                };

                Listener.Bind(ListenerEndPoint);
                Listener.Listen(Config.GetMaxBackLogConnections());

                IsListening = true;
                IsShutdown = false;

                StartHeartBeat();

                ListenerResult = new StartListenerResult(string.Format("Server Started Listening on port: {0}", Config.GetPort()), "StartListener", DateTime.Now, true);

                return ListenerResult;
            }
            catch
            {
                if (ListenerResult == null) //Means from the Listen/Bind/EndPoint
                {
                    ListenerResult = new StartListenerResult(string.Format("Server failed to listen on port: {0}", Config.GetPort()), "StartListener", DateTime.Now, false);
                }
                return ListenerResult;
            }
        }


        /// <summary>
        /// Starts Establishing Incoming Connections
        /// </summary>
        /// <returns></returns>
        public StartAcceptorResult StartAcceptor()
        {
            StartAcceptorResult AcceptorResult;
            try
            {
                if (!IsListening)
                {
                    AcceptorResult = new StartAcceptorResult("Client Acceptor could not start since the server did not start listening", "StartAcceptor", DateTime.Now, false);
                }
                else
                {
                    StartAccept(null);
                    AcceptorResult = new StartAcceptorResult("Client Acceptor started successfully", "StartAcceptor", DateTime.Now, true);
                }
                return AcceptorResult;
            }
            catch
            {
                AcceptorResult = new StartAcceptorResult("Client Acceptor failed to start", "StartAcceptor", DateTime.Now, false);
                return AcceptorResult;
            }
        }

        /// <summary>
        /// Loads all the BlackListed IP's from the file to the black list
        /// </summary>
        public void LoadBlackListedIPS()
        {
            if (File.Exists(Config.GetBlackListPath()))
            {
                BlackListedIP = new List<IPAddress>();
                string[] IPS = File.ReadAllLines(Config.GetBlackListPath());
                foreach (string IP in IPS)
                {
                    if (NetHelper.ValidateIPv4(IP))
                    {
                        IPAddress EndPoint = IPAddress.Parse(IP);
                        BlackListedIP.Add(EndPoint);
                    }
                }
            }
            else
            {
                File.Create(Config.GetBlackListPath()).Dispose();
                LoadBlackListedIPS();
            }
        }

        /// <summary>
        /// Adds IP to the blacklist
        /// </summary>
        /// <param name="IP"></param>
        public void AddIpToBlackList(string IP)
        {
            IPAddress EndPoint = IPAddress.Parse(IP);
            BlackListedIP.Add(EndPoint);
        }

        /// <summary>
        /// Removes IP from the blacklist
        /// </summary>
        /// <param name="IP"></param>
        public void RemoveIpFromBlackList(string IP)
        {
            IPAddress EndPoint = IPAddress.Parse(IP);
            if (BlackListedIP.Contains(EndPoint))
            {
                BlackListedIP.Remove(EndPoint);
            }
        }

        /// <summary>
        /// Disconnect a specified client
        /// </summary>
        /// <param name="Client"></param>
        public void Disconnect(Operation Client)
        {
            RemoveHandlers(Client);
            Client.ShutdownOperation();
        }

        /// <summary>
        /// Disconnect All Clients whose having the same IP
        /// </summary>
        /// <param name="Client"></param>
        public void DisconnectAll(Operation Client)
        {
            foreach (Operation OnlineClient in Clients.ToArray())
            {
                if (OnlineClient.EndPoint.Address.Equals(Client.EndPoint.Address))
                {
                    RemoveHandlers(OnlineClient);
                    OnlineClient.ShutdownOperation();
                }
            }
        }

        /// <summary>
        /// Disconnect All Clients
        /// </summary>
        public void DisconnectAll()
        {
            foreach (Operation Client in Clients.ToArray())
            {
                RemoveHandlers(Client);
                Client.ShutdownOperation();
            }
        }

        /// <summary>
        /// Shutdown the server
        /// </summary>
        public ShutdownResult Shutdown()
        {
            ShutdownResult Result;
            try
            {
                if (IsShutdown == false)
                {
                    IsShutdown = true;
                    IsListening = false;
                    StopHeartBeat();
                    DisconnectAll();
                    if (Clients != null)
                    {
                        Clients.Clear();
                    }
                    if (BlackListedIP != null)
                    {
                        BlackListedIP.Clear();
                    }
                    if (PacketManager != null)
                    {
                        PacketManager.Dispose();
                    }
                    Listener.Close();
                    Result = new ShutdownResult("Server Shutdown Successfully", "Shutdown", DateTime.Now, true);
                }
                else
                {
                    throw new Exception("Server is not running");
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
        #region "Accept"
        private void StartAccept(SocketAsyncEventArgs AcceptEventArgs)
        {
            try
            {
                if (IsListening)
                {
                    if (AcceptEventArgs == null)
                    {
                        AcceptEventArgs = new SocketAsyncEventArgs();
                        AcceptEventArgs.Completed += AcceptEventArgs_Completed;
                    }
                    else
                    {
                        AcceptEventArgs.AcceptSocket = null;
                    }
                    if (!Listener.AcceptAsync(AcceptEventArgs))
                    {
                        ProcessAccept(AcceptEventArgs);
                    }
                }
            }
            catch (SocketException)
            {
                if (IsListening == true && IsShutdown == false)
                {
                    SetOnException(new Exception("Could not start accept due to server shutdown"));
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                if (IsListening == true && IsShutdown == false)
                {
                    SetOnException(ex);
                    Shutdown();
                }
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    Socket EstablishedConnection = e.AcceptSocket;

                    Operation Client = new Operation(EstablishedConnection, Config, PacketManager);

                    if (IsClientBlackListed(Client))
                    {
                        SetOnClientBlackList(Client.EndPoint.Address, "Connection blocked");
                        Disconnect(Client);
                    }
                    else
                    {
                        if (Clients.Count <= Config.GetMaxConnections())
                        {
                            AddClient(Client);
                            SetOnClientConnect(Client);
                        }
                        else
                        {
                            SetOnClientDisconnect(Client, "Server reached the maximum number of clients");
                            Disconnect(Client);
                        }
                    }
                    StartAccept(e);
                }
                else
                {
                    if (IsListening == true && IsShutdown == false)
                    {
                        SetOnException(new Exception("Could not process the accepted connections due to server shutdown"));
                        Shutdown();
                    }
                   
                }
            }
            catch (Exception ex)
            {
                if (IsListening == true && IsShutdown == false)
                {
                    SetOnException(ex);
                    Shutdown();
                }
            }
        }
        #endregion
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
        }
        private void Beat(CancellationToken Token)
        {
            while (IsListening == true && IsShutdown == false)
            {
                if (Token.IsCancellationRequested)
                {
                    break;
                }
                else
                {
                    Clients.ForEach(Client => Client.Beat());
                    Clients.RemoveAll(Client => Client.IsActive == false);
                }
                Thread.Sleep(250);
            }
        }
        #endregion
        #region "Helpers"
        private bool IsClientBlackListed(Operation Client)
        {
            if (BlackListedIP == null)
            {
                LoadBlackListedIPS();
            }
            if (BlackListedIP.Contains(Client.EndPoint.Address))
            {
                return true;
            }
            return false;
        }
        private void AddClient(Operation Client)
        {
            lock (OnlineClientsLock)
            {
                AddHandlers(Client);
                Clients.Add(Client);
                Client.StartOperation();
            }
        }
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
        }
        #endregion
        #endregion
    }
}