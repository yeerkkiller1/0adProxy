using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UdpSessions;

namespace _0adHostProxy
{
    class Program
    {
        static string proxyAddress = "localhost";
        static int proxyPort = 20599;
        static int clientPort = 20595;

        static int hostPort = 18019;

        static int retryTime = 5000;

        static TcpClient proxyServer;
        static NetworkStream proxyStream;

        static void ReconnToProxy()
        {
            bool connected = false;
            while(!connected)
            try
            {
                Console.WriteLine("Connecting to proxy at {0}:{1}", proxyAddress, proxyPort);
                proxyServer = new TcpClient(proxyAddress, proxyPort);
                proxyStream = proxyServer.GetStream();
                connected = true;
                Console.WriteLine("Connected to proxy");
            }
            catch(Exception e)
            {
                Console.WriteLine("Failed to connect to proxy, retrying in {0}ms. Reason: {1}", retryTime, e.ToString());
                Thread.Sleep(retryTime);
            }
        }

        static void Main(string[] args)
        {
            ReconnToProxy();

            Dictionary<int, UdpSender> hostConnections = new Dictionary<int, UdpSender>();
            //var hostServer = new UdpSender("localhost", hostPort);
            //hostServer.OnMessage = (bytes)

            
            while(true)
            {
                int port;
                byte[] packet;
                //Read the packet from the server proxy
                try
                {
                    port = proxyStream.ReadInt();
                    var packetSize = proxyStream.ReadInt();

                    packet = proxyStream.ReadBytes(packetSize);

                    Console.WriteLine(port + "\t read " + packet.Length + " from proxy");
                }
                catch(Exception e)
                {
                    Console.WriteLine("Failed to read from proxy, reconnecting in {0}ms. Reason: " + e.ToString());
                    Thread.Sleep(retryTime);
                    ReconnToProxy();
                    continue;
                }

                UdpSender hostConn;
                if(!hostConnections.ContainsKey(port))
                {
                    Console.WriteLine(port + "\t connected, make new conn to host for this client");
                    hostConn = hostConnections[port] = new UdpSender("localhost", hostPort);
                    hostConn.OnMessage = (bytes) =>
                    {
                        lock(proxyStream)
                        {
                            Console.WriteLine(port + "\t sent " + bytes.Length + " packet, sending to proxy");
                            proxyStream.WriteInt(port);
                            proxyStream.WriteInt(bytes.Length);
                            proxyStream.Write(bytes);
                        }
                    };
                    hostConn.OnError = (e) =>
                    {
                        Console.WriteLine(port + "\t lost connection to host, assuming bad client and dropping client");
                        //BUG: This is not thread safe...
                        hostConnections.Remove(port);
                    };
                }

                hostConn = hostConnections[port];

                hostConn.Send(packet);
            }
        }
    }
}
