using System;

namespace IViewNet.Common.Models
{
    public class Packet
    {
        /// <summary>
        /// Represents a 16 bit code for the packet
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Represents a name for the packet
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Represents a data for the packet
        /// </summary>
        public byte[] Content { get; set; }

        public Packet(int Code, string Name, byte[] Content)
        {
            this.Code = Code;
            this.Name = Name;
            this.Content = Content;
        }

        /// <summary>
        /// Builds a Packet combined with code and content
        /// </summary>
        /// <returns></returns>
        public byte[] ToPacket()
        {
            byte[] Bucket;
            if (Content != null)
            {
                //Allocate a bucket of size 16Bit + Content Length
                Bucket = new byte[2 + Content.Length];
                //Copies the packet-code 16Bit into our Bucket
                Buffer.BlockCopy(BitConverter.GetBytes(Code), 0, Bucket, 0, 2);
                //Copies the packet-conent into our Bucket
                Buffer.BlockCopy(Content, 0, Bucket, 2, Content.Length);
            }
            else
            {
                //Allocate a bucket of size 16Bit
                Bucket = new byte[2];
                //Copies the packet-code 16Bit into our Bucket
                Buffer.BlockCopy(BitConverter.GetBytes(Code), 0, Bucket, 0, 2);
            }
           
            //Return our Bucket
            return Bucket;
        }
    }
}