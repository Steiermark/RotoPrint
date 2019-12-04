using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Plc
{
    class Udp : IDisposable
    {
        byte[] bytes = new byte[1024];

        public Udp()
        {
        }

        public byte[] Message(IPAddress ip, int port, byte[] msg)
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int i = 0;
                while (true)
                {
                    try
                    {
                        EndPoint plc = new IPEndPoint(ip, port);
                        socket.ReceiveTimeout = 200;
                        socket.SendTo(msg, plc);
                        socket.ReceiveFrom(bytes, ref plc);
                        return bytes;
                    }
                    catch (Exception ex)
                    {
                        if (i++ == 3)
                        {
                            bytes = null;
                            throw new Exception("UDP error: " + ex.Message);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
