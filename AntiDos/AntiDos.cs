using System;
using System.Collections.Generic;
using System.IO;
using OTAPI;
using Terraria;
using Terraria.Net;
using Terraria.Net.Sockets;
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
        }
        #endregion
        
        private Hooks.Net.Socket.AcceptedHandler _accepted;
        private static readonly AddressChecker Checker;
        
        static AntiDos()
        {
            Checker = new AddressChecker("doslist.txt");
            Checker.ChangeIpList(Load());
        }

        public override void Initialize()
        {
            _accepted = Hooks.Net.Socket.Accepted;
            Hooks.Net.Socket.Accepted = OnAccepted;

            Commands.ChatCommands.Add(new Command("antidos.reload", ReloadAntiDos, "adreload"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Hooks.Net.Socket.Accepted = _accepted;
            }
            base.Dispose(disposing);
        }

        private static void ReloadAntiDos(CommandArgs args)
        {
            Checker.ChangeIpList(Load());
        }

        private HookResult OnAccepted(ISocket socket)
        {
            var address = socket.GetRemoteAddress() as TcpAddress;
            if (address == null)
            {
                return _accepted(socket);
            }

            var addressString = address.Address.ToString();
            return Checker.Check(addressString) ? _accepted(socket) : HookResult.Cancel;
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