using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Terraria;
using Terraria.Net;
using Terraria.Net.Sockets;
using TShockAPI;

namespace AntiDos.Sockets
{
    public class AntiDosLinuxTcpSocket : ISocket
    {
        private readonly TcpClient _connection;

        private TcpListener _listener;

        private SocketConnectionAccepted _listenerCallback;

        private RemoteAddress _remoteAddress;

        private bool _isListening;

        public AntiDosLinuxTcpSocket()
        {
            _connection = new TcpClient {NoDelay = true};
        }

        public AntiDosLinuxTcpSocket(TcpClient tcpClient)
        {
            _connection = tcpClient;
            _connection.NoDelay = true;
            var iPEndPoint = (IPEndPoint) tcpClient.Client.RemoteEndPoint;
            _remoteAddress = new TcpAddress(iPEndPoint.Address, iPEndPoint.Port);
        }

        void ISocket.Close()
        {
            _remoteAddress = null;
            _connection.Close();
        }

        bool ISocket.IsConnected()
        {
            return _connection?.Client != null && _connection.Connected;
        }

        void ISocket.Connect(RemoteAddress address)
        {
            var tcpAddress = (TcpAddress) address;
            _connection.Connect(tcpAddress.Address, tcpAddress.Port);
            _remoteAddress = address;
        }

        private void ReadCallback(IAsyncResult result)
        {
            var tuple = (Tuple<SocketReceiveCallback, object>) result.AsyncState;

            try
            {
                tuple.Item1(tuple.Item2, _connection.GetStream().EndRead(result));
            }
            catch (InvalidOperationException)
            {
                // This is common behaviour during client disconnects
                ((ISocket) this).Close();
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
            }
        }

        private void SendCallback(IAsyncResult result)
        {
            var expr_0B = (object[]) result.AsyncState;
            LegacyNetBufferPool.ReturnBuffer((byte[]) expr_0B[1]);
            var tuple = (Tuple<SocketSendCallback, object>) expr_0B[0];
            try
            {
                _connection.GetStream().EndWrite(result);
                tuple.Item1(tuple.Item2);
            }
            catch (Exception)
            {
                ((ISocket) this).Close();
            }
        }

        void ISocket.SendQueuedPackets()
        {
        }

        void ISocket.AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
        {
            var array = LegacyNetBufferPool.RequestBuffer(data, offset, size);
            _connection.GetStream().BeginWrite(array, 0, size, SendCallback, new object[]
            {
                new Tuple<SocketSendCallback, object>(callback, state),
                array
            });
        }

        void ISocket.AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state)
        {
            _connection.GetStream().BeginRead(data, offset, size, ReadCallback,
                new Tuple<SocketReceiveCallback, object>(callback, state));
        }

        bool ISocket.IsDataAvailable()
        {
            return _connection.GetStream().DataAvailable;
        }

        RemoteAddress ISocket.GetRemoteAddress()
        {
            return _remoteAddress;
        }

        bool ISocket.StartListening(SocketConnectionAccepted callback)
        {
            var any = IPAddress.Any;
            if (Program.LaunchParameters.TryGetValue("-ip", out var ipString) && !IPAddress.TryParse(ipString, out any))
            {
                any = IPAddress.Any;
            }
            _isListening = true;
            _listenerCallback = callback;
            if (_listener == null)
            {
                _listener = new TcpListener(any, Netplay.ListenPort);
            }
            try
            {
                _listener.Start();
            }
            catch (Exception)
            {
                return false;
            }
            ThreadPool.QueueUserWorkItem(ListenLoop);
            return true;
        }

        void ISocket.StopListening()
        {
            _isListening = false;
        }

        private void ListenLoop(object unused)
        {
            while (_isListening && !Netplay.disconnect)
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    if (!AntiDos.CanAccept(client))
                    {
                        client.Close();
                        continue;
                    }
                    
                    ISocket socket = new AntiDosLinuxTcpSocket(client);
                    _listenerCallback(socket);
                }
                catch
                {
                    // ignored
                }
            }
            _listener.Stop();

            // currently vanilla will stop listening when the slots are full, however it appears that this Netplay.IsListening
            // flag is still set, making the server loop beleive it's still listening when it's actually not.
            // clearing this flag when we actually have stopped will allow the ServerLoop to start listening again when
            // there are enough slots available.
            Netplay.IsListening = false;
        }
    }
}