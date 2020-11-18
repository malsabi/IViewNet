using IViewNet.Common.Enums;
using IViewNet.Common.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IViewNet.Common
{
    public class Operation
    {
        private readonly Socket AcceptedClient;
        private readonly NetConfig Config;
        private readonly PacketManager PacketManager;

        private byte[] HeaderStore;
        private byte[] BodyStore;
        private Queue<byte[]> BufferQueue;
        private object BufferQueueLock;
        private bool IsBuffering;
        private object IsBufferingLock;
        private int ReadOffset;
        private int WriteOffset;
        private int MessageSize;
        private BufferState State;

        #region "Properties"
        public bool IsActive { get; private set; }
        public bool IsAuthenticated { get; set; }
        public IPEndPoint EndPoint { get; private set; }
        public DateTime LastReceive { get; private set; }
        public DateTime LastSent { get; private set; }
        public int TotalBytesSent { get; private set; }
        public int TotalBytesReceived { get; private set; }
        public object Value { get; set; }
        #endregion

        #region "Events"
        public delegate void OnClientDisconnectedDelegate(Operation Client, string Reason);
        public event OnClientDisconnectedDelegate OnClientDisconnected;

        public delegate void OnClientReceiveDelegate(Operation Client, Packet Message);
        public event OnClientReceiveDelegate OnClientReceive;

        public delegate void OnClientSendDelegate(Operation Client, Packet Message);
        public event OnClientSendDelegate OnClientSend;

        public delegate void OnClientExceptionDelegate(Operation Client, Exception Ex);
        public event OnClientExceptionDelegate OnClientException;
        #endregion

        #region "EventHandlers"
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    break;
            }
        }
        private void SetOnReceiveMessage(Packet Message)
        {
            OnClientReceive?.Invoke(this, Message);
        }
        private void SetOnSendMessage(Packet Message)
        {
            OnClientSend?.Invoke(this, Message);
        }
        #endregion

        public Operation(Socket AcceptedClient, NetConfig Config, PacketManager PacketManager)
        {
            this.AcceptedClient = AcceptedClient;
            this.Config = Config;
            this.PacketManager = PacketManager;
            InitializeOperation();
        }

        #region "Public Methods"
        public void SendPacket(Packet Packet)
        {
            byte[] Message = NetHelper.AppendHeader(Packet.ToPacket(), Config.GetHeaderSize());
            AcceptedClient.Send(Message, 0, Message.Length, SocketFlags.None);
            TotalBytesSent += Message.Length;
            SetOnSendMessage(Packet);
        }

        public void ShutdownOperation()
        {
            if (AcceptedClient.IsConnected())
            {
                AcceptedClient.Shutdown(SocketShutdown.Both);
                AcceptedClient.Disconnect(false);
            }
            AcceptedClient.Close();
            IsActive = false;
            IsAuthenticated = false;
            Value = null;
        }
        #endregion

        #region "Internal Methods"
        internal void StartOperation()
        {
            StartReceive(null);
        }
        internal void Synchronize(DateTime TimeStamp)
        {
            LastReceive = TimeStamp;
        }
        internal void Beat()
        {
            if (AcceptedClient != null && IsActive == true)
            {
                if (AcceptedClient.IsConnected() == false)
                {
                    OnClientDisconnected?.Invoke(this, "Dropped");
                    IsActive = false;
                }
                else if ((DateTime.Now - LastReceive).Seconds >= Config.GetMaxTimeOut() && Config.GetEnableKeepAlive() == true)
                {
                    OnClientDisconnected?.Invoke(this, "Timeout");
                    IsActive = false;
                }
            }
        }
        #endregion

        #region "Private Methods"
        private void InitializeOperation()
        {
            try
            {
                IsActive = true;
                IsAuthenticated = false;
                EndPoint = (IPEndPoint)AcceptedClient.RemoteEndPoint;
                LastReceive = DateTime.Now;
                LastSent = DateTime.Now;
                TotalBytesReceived = 0;
                TotalBytesSent = 0;
                HeaderStore = new byte[Config.GetHeaderSize()];
                BufferQueue = new Queue<byte[]>();
                BufferQueueLock = new object();
                IsBuffering = false;
                IsBufferingLock = new object();
                ReadOffset = 0;
                WriteOffset = 0;
                MessageSize = 0;
                State = BufferState.HEADER;
                Value = null;
            }
            catch (Exception ex)
            {
                OnClientException?.Invoke(this, ex);
            }
        }

        private void StartReceive(SocketAsyncEventArgs ReceiveEventArgs)
        {
            try
            {
                if (IsActive)
                {
                    if (ReceiveEventArgs == null)
                    {
                        ReceiveEventArgs = new SocketAsyncEventArgs();
                        ReceiveEventArgs.SetBuffer(new byte[Config.GetBufferSize()], 0, Config.GetBufferSize());
                        ReceiveEventArgs.Completed += IO_Completed;
                    }
                    if (!AcceptedClient.ReceiveAsync(ReceiveEventArgs))
                    {
                        ProcessReceive(ReceiveEventArgs);
                    }
                }
            }
            catch
            {
                OnClientException?.Invoke(this, new Exception("Cannot access a disposed object: Socket"));
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (IsActive)
            {
                if (e.SocketError == SocketError.Success)
                {
                    if (e.BytesTransferred > 0)
                    {
                        byte[] Packet = new byte[e.BytesTransferred];
                        Buffer.BlockCopy(e.Buffer, 0, Packet, 0, e.BytesTransferred);
                        Producer(Packet);
                        StartReceive(e);
                    }
                }
            }
        }

        private void Producer(byte[] Packet)
        {
            lock (BufferQueueLock)
            {
                BufferQueue.Enqueue(Packet);
                lock (IsBufferingLock)
                {
                    if (IsBuffering == false)
                    {
                        IsBuffering = true;
                        ThreadPool.QueueUserWorkItem(new WaitCallback(HandleBuffering), null);
                    }
                }
            }
        }

        private void HandleBuffering(object o)
        {
            while (true)
            {
                byte[] Packet = null;
                lock (BufferQueueLock)
                {
                    if (BufferQueue.Count == 0)
                    {
                        lock (IsBufferingLock)
                        {
                            IsBuffering = false;
                        }
                        break;
                    }
                    Packet = BufferQueue.Dequeue();
                }
                int BytesToProcess = Packet.Length;
                while (BytesToProcess > 0)
                {
                    switch (State)
                    {
                        case BufferState.HEADER:
                            if (BytesToProcess + WriteOffset >= Config.GetHeaderSize())
                            {
                                int ExactLength = (BytesToProcess >= Config.GetHeaderSize()) ? Config.GetHeaderSize() - WriteOffset : BytesToProcess;
                                Buffer.BlockCopy(Packet, ReadOffset, HeaderStore, WriteOffset, ExactLength);

                                WriteOffset = 0;
                                ReadOffset += ExactLength;
                                BytesToProcess -= ExactLength;

                                MessageSize = BitConverter.ToInt32(HeaderStore, 0);

                                if (MessageSize <= 0 || MessageSize >= Config.GetMaxMessageSize())
                                {
                                    OnClientDisconnected?.Invoke(this, "Corrupted Header Packet");
                                    IsActive = false;
                                    BytesToProcess = 0;
                                }
                                State = BufferState.BODY;
                            }
                            else
                            {
                                Buffer.BlockCopy(Packet, ReadOffset, HeaderStore, WriteOffset, BytesToProcess);
                                WriteOffset += BytesToProcess;
                                BytesToProcess = 0;
                            }
                            break;
                        case BufferState.BODY:
                            if (BodyStore == null)
                            {
                                BodyStore = new byte[MessageSize];
                            }
                            else
                            {
                                if (BodyStore.Length != MessageSize)
                                {
                                    BodyStore = new byte[MessageSize];
                                }
                            }
                            int BodyLength = (BytesToProcess + WriteOffset > MessageSize) ? MessageSize - WriteOffset : BytesToProcess;

                            Buffer.BlockCopy(Packet, ReadOffset, BodyStore, WriteOffset, BodyLength);

                            WriteOffset += BodyLength;
                            ReadOffset += BodyLength;
                            BytesToProcess -= BodyLength;
                            if (WriteOffset == MessageSize)
                            {
                                Packet Message = PacketManager.GetPacket(BodyStore, 0);
                                SetOnReceiveMessage(Message);
                                State = BufferState.HEADER;
                                WriteOffset = 0;
                            }
                            break;
                    }
                    if (BytesToProcess == 0)
                    {
                        ReadOffset = 0;
                    }
                }
            }
        }
        #endregion
    }
}