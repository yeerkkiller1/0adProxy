using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpSessions
{
    class Program
    {
        static void Main(string[] args)
        {
            int listenPort = 4874;

            var listener = new UdpListener(listenPort);
            listener.OnConn = session =>
            {
                Console.WriteLine(session + " connected");
            };
            listener.OnMessage = (session, message) =>
            {
                Console.WriteLine(session + " received message " + string.Join(" ", message));

                session.Send(BitConverter.GetBytes(session.Port));
                session.Send(BitConverter.GetBytes(session.Port));
            };
            listener.OnError = (session, e) =>
            {
                Console.WriteLine(session + " exception " + e.ToString());
            };

            for (var ix = 0; ix < 5; ix++)
            {
                var client = new UdpSender("localhost", listenPort);

                client.OnMessage = bytes =>
                {
                    Console.WriteLine("Port " + BitConverter.ToInt32(bytes, 0));
                };

                client.Send(new byte[] { 1, 2 });
            }

            Console.Read();
        }
    }

    public class UdpSender
    {
        public Action<byte[]> OnMessage = null;
        public Action<Exception> OnError = null;

        UdpClient conn;
        string hostname;
        int serverPort;

        bool reading = false;
        public UdpSender(string hostname, int serverPort)
        {
            conn = new UdpClient(0);

            this.hostname = hostname;
            this.serverPort = serverPort;
        }

        private void StartReading()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        //TODO: Should probably specify we can only read from the server that we sent the
                        //  message too... but it is not like that provides any security...
                        var serverEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        var readBytes = conn.Receive(ref serverEndPoint);
                        if (this.OnMessage != null)
                        {
                            this.OnMessage(readBytes);
                        }
                    }
                    catch (Exception e)
                    {
                        this.OnError(e);
                    }
                }
            });
        }

        public void Send(byte[] bytes)
        {
            conn.Send(bytes, hostname, serverPort);

            if(!reading)
            {
                reading = true;
                StartReading();
            }
        }
    }

    public class UdpSession
    {
        public IPAddress Address { get { return this.endPoint.Address; } }
        public int Port { get { return this.endPoint.Port; } }

        private UdpClient conn;
        private IPEndPoint endPoint;
        private UdpListener listener;
        public UdpSession(UdpClient conn, IPEndPoint endPoint, UdpListener listener)
        {
            this.conn = conn;
            this.endPoint = endPoint;
            this.listener = listener;
        }

        public override int GetHashCode()
        {
            return endPoint.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            UdpSession session = (UdpSession)obj;
            return endPoint.Equals(session.endPoint);
        }

        public override string ToString()
        {
            return endPoint.ToString();
        }

        public void Send(byte[] bytes)
        {
            try
            {
                conn.Send(bytes, endPoint);
            }
            catch(Exception e)
            {
                listener.Disconnected(this, e);
            }
        }
    }

    public class UdpListener
    {
        Dictionary<UdpSession, UdpSession> sessions = new Dictionary<UdpSession, UdpSession>();

        UdpClient conn;
        int port;

        public Action<UdpSession> OnConn = null;
        public Action<UdpSession, byte[]> OnMessage = null;
        public Action<UdpSession, Exception> OnError = null;

        public UdpListener(int port)
        {
            this.OnConn = session => Console.WriteLine(session.ToString() + " connected");
            this.OnMessage = (session, bytes) => Console.WriteLine(session.ToString() + " sent " + bytes.Length);
            this.OnError = (session, e) => Console.WriteLine(session != null ? session.ToString() : "?" + " threw " + e.ToString());

            this.port = port;

            this.conn = new UdpClient(port);

            Task.Run(() =>
            {
                while(true)
                {
                    this.Listen();
                }
            });
        }

        public UdpSession FindSession(int port)
        {
            return sessions.FirstOrDefault(x => x.Key.Port == port).Key;
        }

        public void Disconnected(UdpSession session, Exception e)
        {
            this.OnError(session, e);

            sessions.Remove(session);
        }

        private void Listen()
        {
            UdpSession session = null;
            try
            {
                IPEndPoint clientAddr = new IPEndPoint(IPAddress.Any, port);
                var bytes = conn.Receive(ref clientAddr);
                var tempSession = new UdpSession(conn, clientAddr, this);
                session = tempSession;

                if (!sessions.ContainsKey(tempSession))
                {
                    if (this.OnConn != null)
                    {
                        this.OnConn(session);
                    }
                    sessions.Add(tempSession, tempSession);
                }
                else
                {
                    session = sessions[tempSession];
                }


                if (this.OnMessage != null)
                {
                    this.OnMessage(session, bytes);
                }
            }
            catch (Exception e)
            {
                Disconnected(session, e);
            }
        }
    }

    public static class NetStreamExtensions
    {
        public static void Write(this NetworkStream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }
        public static int Read(this NetworkStream stream, byte[] bytes)
        {
            return stream.Read(bytes, 0, bytes.Length);
        }

        public static void WriteBytes(this NetworkStream stream, byte[] bytes)
        {
            stream.Write(bytes);
        }
        public static byte[] ReadBytes(this NetworkStream stream, int count)
        {
            int bytesRead = 0;
            var bytes = new byte[count];
            while (bytesRead < count)
            {
                int readCount = stream.Read(bytes, bytesRead, count - bytesRead);
                bytesRead += readCount;
            }
            if (bytesRead > count)
            {
                throw new NotImplementedException("Read too many bytes, wtf?");
            }
            return bytes;
        }

        public static void WriteInt(this NetworkStream stream, int num)
        {
            stream.Write(BitConverter.GetBytes(num));
        }
        public static int ReadInt(this NetworkStream stream)
        {
            byte[] bytes = stream.ReadBytes(4);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static int Send(this UdpClient client, byte[] bytes)
        {
            return client.Send(bytes, bytes.Length);
        }
        public static int Send(this UdpClient client, byte[] bytes, IPEndPoint endPoint)
        {
            return client.Send(bytes, bytes.Length, endPoint);
        }
        public static int Send(this UdpClient client, byte[] bytes, string hostname, int port)
        {
            return client.Send(bytes, bytes.Length, hostname, port);
        }
    }
}
 