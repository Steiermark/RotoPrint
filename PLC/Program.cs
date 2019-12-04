using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PLC
{
    class Program
    {
        static int t1 = 65, t2 = 65, t3 = 65, t4 = 65, t5 = 65, t6 = 65;

        static void Main(string[] args)
        {
            Thread t = new Thread(new ThreadStart(TcpThread));
            t.Start();

            ConsoleKeyInfo cki;
            do
            {
                cki = Console.ReadKey();
                if (cki.Key == ConsoleKey.F1) if (cki.Modifiers == ConsoleModifiers.Shift) t1--; else t1++;
                if (cki.Key == ConsoleKey.F2) if (cki.Modifiers == ConsoleModifiers.Shift) t2--; else t2++;
                if (cki.Key == ConsoleKey.F3) if (cki.Modifiers == ConsoleModifiers.Shift) t3--; else t3++;
                if (cki.Key == ConsoleKey.F4) if (cki.Modifiers == ConsoleModifiers.Shift) t4--; else t4++;
                if (cki.Key == ConsoleKey.F5) if (cki.Modifiers == ConsoleModifiers.Shift) t5--; else t5++;
                if (cki.Key == ConsoleKey.F6) if (cki.Modifiers == ConsoleModifiers.Shift) t6--; else t6++;
            } while (cki.Key != ConsoleKey.Escape);
            t.Abort();
        }

        static void TcpThread()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 8080);
            TcpClient client = null;
            server.Start();

            while (true)
            {
                try
                {
                    Console.WriteLine("Wating for client");
                    client = server.AcceptTcpClient();
                    Console.WriteLine("Connection est.");
                    Stream stream = client.GetStream();
                    Byte[] bytes = new Byte[256];
                    String data = null;
                    String resp = "";

                    int i = 0;

                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.Write(String.Format("Received: {0} ", data));

                        if (data == "GET STATUS") resp = "IDLE";
                        if (data == "GET TEMP") resp = String.Format("TEMP {0} {1} {2} {3} {4} {5}", t1, t2, t3, t4, t5, t6);

                        stream.Write(Encoding.ASCII.GetBytes(resp), 0, resp.Length);
                        Console.WriteLine("Response: {0}", resp);
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Connection lost");
                }
                catch (ThreadAbortException e)
                {
                    Console.WriteLine("Aborting");
                    throw;
                }
                finally
                {
                    client.Close();
                }
            }
        }
    }
}
