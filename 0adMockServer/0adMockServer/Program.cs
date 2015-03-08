using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace _0adMockServer
{
    class Program
    {
        static byte[] response = new byte[] { 1, 2, 3, 4, 5 };

        static void Main(string[] args)
        {
            int clientPort = 20595;
            int serverPort = 18019;

            var server = new UdpClient(serverPort);
            while(true)
            {
                var clientSender = new IPEndPoint(IPAddress.Any, serverPort);
                var bytes = server.Receive(ref clientSender);
                Console.WriteLine("Received " + bytes.Length + " from " + clientSender.ToString());

                new UdpClient().Send(response, response.Length, clientSender);
                new UdpClient().Send(bytes, bytes.Length, clientSender);
            }
        }
    }
}
