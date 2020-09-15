namespace IViewNet.Pipes
{
    public class PipeConfig
    {
        #region "Fields"
        private readonly int BufferSize;
        private readonly int MaxNumOfServers;
        #endregion

        public PipeConfig()
        {
            BufferSize = 1024 * 10;
            MaxNumOfServers = 1;
        }

        public PipeConfig(int BufferSize, int MaxNumOfServers)
        {
            this.BufferSize = BufferSize;
            this.MaxNumOfServers = MaxNumOfServers;
        }

        public int GetBufferSize()
        {
            if (BufferSize >= 0)
            {
                return BufferSize;
            }
            else
            {
                return 1024 * 10;
            }
        }

        public int GetMaxNumOfServers()
        {
            if (MaxNumOfServers >= 1)
            {
                return MaxNumOfServers;
            }
            else
            {
                return 1;
            }
        }
    }
}