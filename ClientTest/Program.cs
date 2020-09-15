using IViewNet.Client;
using IViewNet.Common;
using IViewNet.Common.Models;
using System;
using System.Drawing.Imaging;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;

namespace ClientTest
{
    class Program
    {

        private static Client C;
        private static readonly object OnLogLock = new object();

        private static PacketManager PM;
        static void Main(string[] args)
        {
            Console.SetBufferSize(Console.BufferWidth, 32766);
            for (int i = 1; i <= 1; i++)
            {
                C = new Client(NetConfig.CreateDefaultClientConfig());
                C.OnClientConnect += C_OnClientConnect;
                C.OnClientAuthenticated += C_OnClientAuthenticated;
                C.OnClientReceive += C_OnClientReceive;
                C.OnClientSend += C_OnClientSend;
                C.OnClientDisconnect += C_OnClientDisconnect;
                C.OnClientException += C_OnClientException;
                C.Connect("127.0.0.1", 1669);
                PM = C.PacketManager;
                PM.AddPacket(new Packet(0003, "ClientInformation", null));
               
            }

        }

        private static void C_OnClientConnect(Operation Client)
        {
            OnLog(ConsoleColor.Blue, "[{0}\tClient Connected: {1}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint);
        }
        private static void C_OnClientAuthenticated(Operation Client, bool Success)
        {
            if (Success)
            {
                OnLog(ConsoleColor.Green, "[{0}\tClient authentication succeeded: {1}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint);
                Client.SendPacket(new Packet(0003, "ClientInformation", Encoding.Default.GetBytes("Tsunami PC")));
            }
            else
            {
                OnLog(ConsoleColor.Red, "[{0}\tClient authentication failed: {1}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint);
            }
        }
        private static void C_OnClientSend(Operation Client, IViewNet.Common.Models.Packet Message)
        {
            //OnLog(ConsoleColor.Yellow, "[{0}\tClient[{1}]\tSent: {2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint, Message.Name);
        }
        private static void C_OnClientReceive(Operation Client, IViewNet.Common.Models.Packet Message)
        {
            //OnLog(ConsoleColor.Yellow, "[{0}\tClient[{1}]\tReceived: {2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint, Message.Name);
        }
        private static void C_OnClientDisconnect(Operation Client, string Reason)
        {
            OnLog(ConsoleColor.Red, "[{0}\tClient: {1} {2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint, Reason);
        }
        private static void C_OnClientException(Operation Client, Exception Ex)
        {
            OnLog(ConsoleColor.Red, "[{0}\tClient: {1} {2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint, Ex.Message);
        }
        private static void OnLog(ConsoleColor color, string text, params object[] args)
        {
            lock (OnLogLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(text, args);
            }
        }
    }
}