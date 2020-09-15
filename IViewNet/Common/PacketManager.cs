using IViewNet.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IViewNet.Common
{
    /// <summary>
    /// Packet Manager
    /// Registering and unregistering the packets.
    /// Provide a usefull methods for manipulating the packet.
    /// Features: Full Cached to increase performance.
    /// </summary>
    public class PacketManager : IDisposable
    {

        private readonly Dictionary<int, Packet> Packets;

        public PacketManager()
        {
            Packets = new Dictionary<int, Packet>();
        }

        #region "Public Methods"
        /// <summary>
        /// Adds a Packet
        /// </summary>
        /// <param name="Packet"></param>
        public void AddPacket(Packet Packet)
        {
            if (Packets.ContainsKey(Packet.Code))
            {
                return;
            }
            else
            {
                Packets.Add(Packet.Code, Packet);
            }
        }

        /// <summary>
        /// Removes a Packet
        /// </summary>
        /// <param name="Packet"></param>
        public void RemovePacket(Packet Packet)
        {
            if (Packets.ContainsKey(Packet.Code))
            {
                Packets.Remove(Packet.Code);
            }
        }

        /// <summary>
        /// Retreives all of the packets
        /// </summary>
        /// <returns>Array of Packets</returns>
        public KeyValuePair<int, Packet>[] GetPackets()
        {
            return Packets.ToArray();
        }

        /// <summary>
        /// Retreive a packet name from a given code
        /// </summary>
        /// <param name="Code"></param>
        /// <returns>Name</returns>
        public string GetPacketName(int Code)
        {
            return Packets[Code].Name;
        }

        /// <summary>
        /// Sets a packet name from a given code
        /// </summary>
        /// <param name="Code"></param>
        /// <param name="Name"></param>
        public void SetPacketName(int Code, string Name)
        {
            if (Packets.ContainsKey(Code))
            {
                Packets[Code] = new Packet(Code, Name, GetPacketContent(Code));
            }
        }

        /// <summary>
        /// Gets a packet code from a given name
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public int GetPacketCode(string Name)
        {
            foreach (KeyValuePair<int, Packet> Packet in GetPackets())
            {
                if (Packet.Value.Name.Equals(Name))
                {
                    return Packet.Value.Code;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets a packet content from a given code
        /// </summary>
        /// <param name="Code"></param>
        /// <returns></returns>
        public byte[] GetPacketContent(int Code)
        {
            if (Packets.ContainsKey(Code))
            {
                return Packets[Code].Content;
            }
            return null;
        }

        /// <summary>
        /// Determine if the code is valid or not
        /// </summary>
        /// <param name="Code"></param>
        /// <returns>True if found, False otherwise</returns>
        public bool IsCodeValid(int Code)
        {
            if (Packets.ContainsKey(Code))
            {
                return true;
            }
            return false;
        }
        public void Dispose()
        {
            Packets.Clear();
        }
        #endregion
        #region "Static Methods"
        public Packet GetPacket(byte[] Packet, int Offset)
        {
            //The incoming packet now is combined with [Code][Content] so we will desolve them
            //Create an Packet Object to add the code, name, data
            Packet Message;
            //Extract the code
            int Code = BitConverter.ToInt16(Packet, Offset);
            //Verify the code
            if (IsCodeValid(Code))
            {
                //Initialize a content
                byte[] Content = new byte[Packet.Length - Offset - 2];
                //Copy the packet content into our content
                Buffer.BlockCopy(Packet, 2 + Offset, Content, 0, Content.Length);
                //Create an instance of packet object
                Message = new Packet(Code, GetPacketName(Code), Content);
            }
            else
            {
                //False Code, Currupted Message.
                Message = new Packet(-1, "", null);
            }
            return Message;
        }
        #endregion
    }
}