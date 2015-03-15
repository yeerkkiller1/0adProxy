using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UdpSessions;

namespace YP2P
{
    class Program
    {
        enum NatType
        {
            //Behind a corporate NAT we don't know what port we are really sending from, so we send from a lot of
            //  packets both ways, and hopefully the router guess a port we are sending from, allowing a packet through

            //Sends to destIP from RandomPort to Port
            corporate,
            //Sends to destIP from Port to RandomPort
            router
        }

        class Options
        {
            [Option('n', "nat", Required = true, HelpText = "Our NAT type, (corporate|router)")]
            public NatType Nat { get; set; }

            [Option('p', "port", Required = true, HelpText = "Port the router sends from (and so the corporate sends to)")]
            public int Port { get; set; }

            [Option('d', "destIP", Required = true, HelpText = "Public IP of target")]
            public string DestIP { get; set; }

            [Option('c', "count", Required = true, HelpText = "# of ports to try")]
            public int PortCount { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }

        static void Main(string[] args)
        {
            Options options = new Options();

            if (args.Length == 0)
            {
                Console.WriteLine(options.GetUsage());
                return;
            }

            if (!Parser.Default.ParseArguments(args, options))
            {
                Console.WriteLine(options.GetUsage());
                return;
            }

            Console.WriteLine(JsonConvert.SerializeObject(options));

            switch (options.Nat)
            {
                case NatType.corporate:
                    Corporate(options);
                    break;
                case NatType.router:
                    Router(options);
                    break;
            }

            while (Console.ReadLine() != "exit")
            {
                Console.WriteLine("Type exit to exit");
            }
        }

        static Random rand = new Random();
        static int RandomPort()
        {
            return rand.Next(50000, 60000);
        }

        static void Corporate(Options options)
        {
            List<int> sourcePorts = Enumerable.Range(50000, options.PortCount).Select(x => RandomPort()).ToList();

            sourcePorts.ForEach(sourcePort =>
            {
                UdpSender sender = new UdpSender(options.DestIP, sourcePort, read: false, sourcePort: options.Port);
                sender.Send(new byte[] { 1, 2, 3 });
            });
        }
        static void Router(Options options)
        {
            List<int> sourcePorts = Enumerable.Range(50000, options.PortCount).Select(x => RandomPort()).ToList();

            sourcePorts.ForEach(sourcePort =>
            {
                UdpSender sender = new UdpSender(options.DestIP, options.Port, sourcePort: sourcePort);
                sender.Send(new byte[] { 1, 2, 3 });
            });
        }
    }
}
