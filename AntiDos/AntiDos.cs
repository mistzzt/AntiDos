using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using OTAPI;
using Terraria;
using System.Net.Sockets;
using AntiDos.Sockets;
using TerrariaApi.Server;
using TShockAPI;

namespace AntiDos
{
    [ApiVersion(2, 1)]
    public sealed class AntiDos : TerrariaPlugin
    {
        #region Information
        public override string Name => GetType().Name;

        public override string Author => "MistZZT";

        public override Version Version => GetType().Assembly.GetName().Version;

        public AntiDos(Main game) : base(game)
        {
            Order = 10;
        }
        #endregion
        
        private static readonly AddressChecker Checker;
        
        static AntiDos()
        {
            Checker = new AddressChecker("doslist.txt");
            Checker.ChangeIpList(Load());
        }

        public override void Initialize()
        {
            Hooks.Net.Socket.Create = () => new AntiDosLinuxTcpSocket();
            
            Commands.ChatCommands.Add(new Command("antidos.reload", ReloadAntiDos, "adreload"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        private static void ReloadAntiDos(CommandArgs args)
        {
            Checker.ChangeIpList(Load());
        }

        internal static bool CanAccept(TcpClient client)
        {
            var address = (IPEndPoint) client.Client.RemoteEndPoint;
            var addressString = address.Address.ToString();

            var status = Checker.Check(addressString);
            
            Console.WriteLine((status ? "连接：" : "拦截：") + addressString);

            return status;
        }

        private static IEnumerable<string> Load()
        {
            var list = new List<string>();

            try
            {
                var file = new FileStream("doslist.txt", FileMode.OpenOrCreate);
                using (var reader = new StreamReader(file))
                {
                    string r;
                    while (!string.IsNullOrWhiteSpace(r = reader.ReadLine()))
                    {
                        list.Add(r);
                    }
                }
            }
            catch
            {
                // ignored
            }

            Console.WriteLine("Banned IPs Loaded: {0}", list.Count);
            return list;
        }
    }
}