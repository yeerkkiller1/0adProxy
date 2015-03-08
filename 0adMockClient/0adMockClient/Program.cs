using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _0adMockClient
{
    class Program
    {
        static byte[] payload1 = new byte[] { 1 };
        static byte[] payload2 = new byte[] { 1, 2 };

        static void Main(string[] args)
        {
            int clientPort = 20595;

            int serverPort = 18019;

            var client = new UdpClient(0);

            byte[] payload = payload1;

            while (true)
            {
                int tries = 5;
                int timeout = 5000;
                while (tries-- > 0)
                {
                    bool fuckyou = false;
                    bool gotResponse = false;
                    var task = Task.Run(() =>
                    {
                        IPEndPoint serverAddress = new IPEndPoint(IPAddress.Any, 0);
                        var receivedBytes = client.Receive(ref serverAddress);
                        if (fuckyou)
                        {
                            Console.WriteLine("Received response after timeout");
                        }
                        Console.WriteLine("Received " + receivedBytes.Length + " from " + serverAddress.ToString());
                        gotResponse = true;
                    });

                    Console.WriteLine("Sending packet");
                    client.Send(payload, payload.Length, "localhost", clientPort);

                    Thread.Sleep(timeout);
                    fuckyou = true;
                    if (gotResponse)
                    {
                        break;
                    }
                }

                payload = payload == payload1 ? payload2 : payload1;
            }
        }
    }
}
