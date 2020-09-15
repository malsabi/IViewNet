using IViewNet.Common;
using IViewNet.Common.Models;
using IViewNet.Server;
using System;
using System.Text;
using System.Windows.Forms;

namespace ServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.SetBufferSize(Console.BufferWidth, 32766);
            //Console.ReadKey();
            Application.Run(new StreamForm());
        }
    }
}