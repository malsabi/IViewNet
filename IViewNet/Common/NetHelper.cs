using System;
using System.Linq;
using System.Net.Sockets;
namespace IViewNet.Common
{
    public static class NetHelper
    {
        public static bool IsConnected(this Socket Handler)
        {
            if (Handler != null && Handler.Connected)
            {
                try
                {
                    if (Handler.Poll(0, SelectMode.SelectRead))
                    {
                        if (Handler.Receive(new byte[1], SocketFlags.Peek) == 0)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        public static byte[] AppendHeader(byte[] Message, int HeaderSize)
        {
            byte[] Packet = new byte[Message.Length + HeaderSize];
            Buffer.BlockCopy(BitConverter.GetBytes(Message.Length), 0, Packet, 0, HeaderSize);
            Buffer.BlockCopy(Message, 0, Packet, HeaderSize, Message.Length);
            return Packet;
        }

        public static bool ValidateIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            return splitValues.All(r => byte.TryParse(r, out byte tempForParsing));
        }
    }
}