using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public const string FilePath = "doslist.txt";

        private static readonly AddressChecker Checker;

        static AntiDos()
        {
            Checker = new AddressChecker(FilePath);
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
            var address = ((IPEndPoint) client.Client.RemoteEndPoint).Address;

            var status = Checker.Check(address);
            Console.WriteLine((status ? "连接：" : "拦截：") + address);

            return status;
        }

        private static IEnumerable<string> Load()
        {
            var list = new List<string>();

            try
            {
                var file = new FileStream(FilePath, FileMode.OpenOrCreate);
                using (var reader = new StreamReader(file))
                {
                    while (!reader.EndOfStream)
                    {
                        var r = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(r))
                            list.Add(r);
                    }
                }
            }
            catch
            {
                // ignored
            }

            Console.WriteLine("[AntiDos] {0, 5} IPs Loaded.", list.Count);
            return list.Distinct();
        }
    }
}