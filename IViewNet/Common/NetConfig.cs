namespace IViewNet.Common
{
    public class NetConfig
    {
        public NetConfig(int maxKeepAliveInterval, int maxPendingConnections, int maxConnections, int maxConnectionsSameIP, bool enableDuplicateIPS, int maxMessageSize, int port, int bufferSize, int headerSize, string path)
        {
            SetMaxTimeOut(maxKeepAliveInterval);
            SetMaxBackLogConnections(maxPendingConnections);
            SetMaxConnections(maxConnections);
            SetMaxSameIPConnections(maxConnectionsSameIP);
            SetEnableDuplicateIPS(enableDuplicateIPS);
            SetMaxMessageSize(maxMessageSize);
            SetPort(port);
            SetBufferSize(bufferSize);
            SetHeaderSize(headerSize);
            SetBlackListPath(path);
        }

        //The port is 1669
        public static NetConfig CreateDefaultServerConfig()
        {
            return new NetConfig(50, 10000, 10000, 99, false, 1024 * 1024 * 30, 1669, 1024 * 8, 4, "./BLackList.txt");
        }

        public static NetConfig CreateDefaultClientConfig()
        {
            return new NetConfig(50, 0, 0, 0, false, 1024 * 1024, 1669, 1024 * 8, 4, "");
        }


        private int MaxTimeOut;
        /// <summary>
        /// The Time to wait for a client to response
        /// </summary>
        public int GetMaxTimeOut()
        {
            return MaxTimeOut;
        }
        public void SetMaxTimeOut(int Value)
        {
            if (Value < 0 || Value > 60)
            {
                Value = 50;
            }
            MaxTimeOut = Value;
        }


        private int MaxBackLogConnections;
        /// <summary>
        /// Maxiumim Connection Requests in the BackLog
        /// </summary>
        public int GetMaxBackLogConnections()
        {
            return MaxBackLogConnections;
        }
        public void SetMaxBackLogConnections(int Value)
        {
            if (Value < 0)
            {
                Value = 1000;
            }
            MaxBackLogConnections = Value;
        }


        private int MaxConnections;
        /// <summary>
        /// Maxiumum number of Clients can server have
        /// </summary>
        public int GetMaxConnections()
        {
            return MaxConnections;
        }
        public void SetMaxConnections(int Value)
        {
            if (Value < 0)
            {
                Value = 1000;
            }
            MaxConnections = Value;
        }


        private int MaxSameIPConnections;
        /// <summary>
        /// Maximum number of Clients with same IP can server have
        /// </summary>
        public int GetMaxSameIPConnections()
        {
            return MaxSameIPConnections;
        }
        public void SetMaxSameIPConnections(int Value)
        {
            if (Value < 0)
            {
                Value = 10;
            }
            MaxSameIPConnections = Value;
        }


        private bool EnableDuplicateIPS;
        /// <summary>
        /// if True, The server will allow Same IP Clients, otherwise only one is accepted
        /// </summary>
        public bool GetEnableDuplicateIPS()
        {
            return EnableDuplicateIPS;
        }
        public void SetEnableDuplicateIPS(bool Value)
        {
            EnableDuplicateIPS = Value;
        }


        private int MaxMessageSize;
        /// <summary>
        /// The Maximum Size allowed for a message
        /// </summary>
        public int GetMaxMessageSize()
        {
            return MaxMessageSize;
        }
        public void SetMaxMessageSize(int Value)
        {
            if (Value < 0)
            {
                Value = (1024 * 1024) * 10;
            }
            MaxMessageSize = Value;
        }


        private int Port;
        /// <summary>
        /// The Port used For the Server to Listen From.
        /// </summary>
        public int GetPort()
        {
            return Port;
        }
        public void SetPort(int Value)
        {
            if (Value > ushort.MaxValue || Value <= 0)
            {
                Value = 1669;
            }
            Port = Value;
        }


        private int BufferSize;
        /// <summary>
        /// The Bufer Size to allocate for the received packet
        /// </summary>
        public int GetBufferSize()
        {
            return BufferSize;
        }
        public void SetBufferSize(int Value)
        {
            if (Value < 0)
            {
                Value = 1024 * 16;
            }
            BufferSize = Value;
        }


        private int HeaderSize;
        /// <summary>
        /// The Header Size which contains our packet information
        /// </summary>
        public int GetHeaderSize()
        {
            return HeaderSize;
        }
        public void SetHeaderSize(int Value)
        {
            if (Value < 0)
            {
                Value = 4; //4Bytes
            }
            HeaderSize = Value;
        }

        private string BlackListPath;
        /// <summary>
        /// Gets the Black List File Path which contains the black listed IP's
        /// </summary>
        public string GetBlackListPath()
        {
            return BlackListPath;
        }

        public void SetBlackListPath(string Value)
        {
            BlackListPath = Value;
        }

        /// <summary>
        /// Gets the public key used which is used to verify the connection
        /// </summary>
        /// <returns></returns>
        public byte[] GetPublicKey()
        {
            return System.Text.Encoding.UTF8.GetBytes("PublicVexareNetworking");
        }

        /// <summary>
        /// Gets the private key which is used to verify the connection
        /// </summary>
        /// <returns></returns>
        public byte[] GetPrivateKey()
        {
            return System.Text.Encoding.UTF8.GetBytes("PrivateVexareNetworking");
        }
    }
}