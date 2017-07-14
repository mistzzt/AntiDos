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
        public override string Name => GetType().Name;

        public override string Author => "MistZZT";

        public override Version Version => GetType().Assembly.GetName().Version;

        private Hooks.Net.Socket.AcceptedHandler _accepted;

        private static readonly Dictionary<string, DateTime> Ips = new Dictionary<string, DateTime>();

        private static readonly List<string> BannedIp;

        static AntiDos()
        {
            BannedIp = Load();
        }

        public AntiDos(Main game) : base(game)
        {
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
            BannedIp.Clear();
            BannedIp.AddRange(Load());
        }

        private HookResult OnAccepted(ISocket socket)
        {
            var address = socket.GetRemoteAddress() as TcpAddress;

            if (address == null)
            {
                return _accepted(socket);
            }

            var ip = string.Intern(address.Address.ToString());
            if (BannedIp.Contains(ip))
            {
                return HookResult.Cancel;
            }

            var now = DateTime.Now;
            if (Ips.TryGetValue(ip, out DateTime time) && (now - time).Seconds < 1)
            {
                Add(ip);
                return HookResult.Cancel;
            }
            Ips[ip] = DateTime.Now;


            return _accepted(socket);
        }

        private static void Add(string ip)
        {
            try
            {
                File.AppendAllText("doslist.txt", ip + Environment.NewLine);
            }
            catch
            {
                // ignored
            }

            BannedIp.Add(ip);

            Console.WriteLine($"Banned: {ip}");
        }

        private static List<string> Load()
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