using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace AntiDos
{
    public sealed class AddressChecker
    {
        private readonly string _fileName;

        private readonly HashSet<string> _bannedIp = new HashSet<string>();

        private readonly Queue<Record> _records = new Queue<Record>();

        private const int MaxKeepRecordTime = (int) T3Threshold + 1;

        private const double T1Threshold = 1;

        private const double T2Threshold = 10;

        private const double T3Threshold = 60;

        private const int T1Times = 2, T2Times = 5, T3Times = 10;

        public AddressChecker(string fileName)
        {
            _fileName = fileName;
        }

        public void ChangeIpList(IEnumerable<string> ips)
        {
            _bannedIp.Clear();

            foreach (var ip in ips)
            {
                _bannedIp.Add(ip);
            }
        }

        public bool Check(IPAddress address)
        {
            if (IPAddress.Loopback.Equals(address))
            {
                return true; // 跳过自检测
            }

            var now = DateTime.Now;

            var time = now - TimeSpan.FromSeconds(MaxKeepRecordTime);
            while (_records.Count != 0 && _records.Peek().LastConnection < time)
            {
                _records.Dequeue();
            }

            var ip = string.Intern(address.ToString());
            if (_bannedIp.Contains(ip))
            {
                return false;
            }

            int t1 = 1, t2 = 1, t3 = 1; // 包括这次出现次数
            foreach (var record in _records)
            {
                if (!ReferenceEquals(record.Ip, ip))
                {
                    continue;
                }

                var interval = (now - record.LastConnection).TotalSeconds;
                if (interval <= T3Threshold) t3++;
                if (interval <= T2Threshold) t2++;
                if (interval <= T1Threshold) t1++;

                if (t1 >= T1Times ||
                    t2 >= T2Times ||
                    t3 >= T3Times)
                {
                    Add(ip);
                    return false;
                }
            }

            _records.Enqueue(new Record {Ip = ip, LastConnection = now});
            return true;
        }

        private void Add(string ip)
        {
            _bannedIp.Add(ip);

            File.AppendAllText(_fileName, ip + Environment.NewLine);
            TShockAPI.TShock.Log.ConsoleInfo("IP banned: " + ip);
        }

        private struct Record
        {
            public string Ip;

            public DateTime LastConnection;
        }
    }
}