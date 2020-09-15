using IViewNet.Common;
using IViewNet.Common.Models;
using IViewNet.Server;
using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerTest
{
    public partial class StreamForm : Form
    {
        private static Server SM;
        private static readonly object OnLogLock = new object();
        private static int ConnectionCounter = 1;
        private static PacketManager PM;

        private int FrameCounter = 0;
        private int FPS = 0;


        public StreamForm()
        {
            InitializeComponent();
            SM = new Server(NetConfig.CreateDefaultServerConfig());
            SM.OnClientConnect += SM_OnClientConnect;
            SM.OnClientAuthenticated += SM_OnClientAuthenticated;
            SM.OnClientReceive += SM_OnClientReceive;
            SM.OnClientSend += SM_OnClientSend;
            SM.OnClientBlackList += SM_OnClientBlackList;
            SM.OnClientDisconnect += SM_OnClientDisconnect;
            SM.OnClientException += SM_OnClientException;
            StartServer();
            PM = SM.PacketManager;
            PM.AddPacket(new Packet(0003, "ClientInformation", null));
            FpsCounter.Start();
        }
        private static void StartServer()
        {
            StartListenerResult ListenerResult = SM.StartListener();
            if (ListenerResult.IsOperationSuccess)
            {
                OnLog(ConsoleColor.DarkCyan, "[{0}\t{1}]\t{2}", ListenerResult.TimeStamp.ToString("dd-MM-yyyy HH:mm:ss.ffff"), ListenerResult.Type, ListenerResult.Message);

                StartAcceptorResult AcceptorResult = SM.StartAcceptor();

                if (AcceptorResult.IsOperationSuccess)
                {
                    OnLog(ConsoleColor.DarkCyan, "[{0}\t{1}]\t{2}", AcceptorResult.TimeStamp.ToString("dd-MM-yyyy HH:mm:ss.ffff"), AcceptorResult.Type, AcceptorResult.Message);

                    SM.LoadBlackListedIPS();
                }
                else
                {
                    OnLog(ConsoleColor.Red, "[{0}\t{1}]\t{2}", AcceptorResult.TimeStamp.ToString("dd-MM-yyyy HH:mm:ss.ffff"), AcceptorResult.Type, AcceptorResult.Message);
                }
            }
            else
            {
                OnLog(ConsoleColor.Red, "[{0}\t{1}]\t{2}", ListenerResult.TimeStamp.ToString("dd-MM-yyyy HH:mm:ss.ffff"), ListenerResult.Type, ListenerResult.Message);
            }
        }
        private static void SM_OnClientConnect(Operation Client)
        {
            OnLog(ConsoleColor.Blue, "[{0}\tClient Connected: {1}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint);
        }
        private static void SM_OnClientAuthenticated(Operation Client, bool Success)
        {
            if (Success)
            {
                OnLog(ConsoleColor.Green, "[{0}\tClient authentication succeeded: {1}\t{2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint, ConnectionCounter);
                ConnectionCounter++;
            }
            else
            {
                OnLog(ConsoleColor.Red, "[{0}\tClient authentication failed: {1}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint);
            }
        }
        private static void SM_OnClientSend(Operation Client, Packet Message)
        {
            //OnLog(ConsoleColor.Yellow, "[{0}\tClient[{1}]\tSent: {2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint, Message.Name);
        }
        private void SM_OnClientReceive(Operation Client, Packet Message)
        {
            SetFrame(Message.Content);
            //OnLog(ConsoleColor.Yellow, "[{0}\tClient[{1}]\tReceived: {2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint, Encoding.Default.GetString(Message.Content));
        }
        private static void SM_OnClientBlackList(System.Net.IPAddress IP, string Reason)
        {
            OnLog(ConsoleColor.Red, "[{0}\t{1}: {2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), IP, Reason);
        }
        private static void SM_OnClientDisconnect(Operation Client, string Reason)
        {
            OnLog(ConsoleColor.Red, "[{0}\tClient: {1} {2}]", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.ffff"), Client.EndPoint, Reason);
        }
        private static void SM_OnClientException(Operation Client, Exception Ex)
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

        public delegate void SetFrameDelegate(byte[] FrameData);
        public  void SetFrame(byte[] FrameData)
        {
            //Frame per secomd
            FrameCounter++;
            if (StreamBox.InvokeRequired)
            {
                StreamBox.Invoke(new SetFrameDelegate(SetFrame), new object[] { FrameData });
            }
            else
            {
                StreamBox.Image = Image.FromStream(new System.IO.MemoryStream(FrameData));
            }
        }

        private void StreamForm_Load(object sender, EventArgs e)
        {
            //OutBox[Python Script(Image)] ->    Detect Image
            //Run Mutiple Threads 5
            //1: OutBox[Python Script(Image)] -> Detect Image
            //2: OutBox[Python Script(Image)] -> Detect Image
            //3: OutBox[Python Script(Image)] -> Detect Image
            //4: OutBox[Python Script(Image)] -> Detect Image
            //5: OutBox[Python Script(Image)] -> Detect Image
            //Frames Stored In a queue
            //Do
            //Image = Dequeue()
            //Loop
        }

        private void FpsCounter_Tick(object sender, EventArgs e)
        {
            if (FrameCounter > 0)
            {
                FPS = FrameCounter;
                Text = "StreamForm   FPS: " + FPS.ToString();
                FrameCounter = 0;
            }
        }
    }
}
