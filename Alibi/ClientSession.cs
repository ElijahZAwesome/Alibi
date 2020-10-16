﻿using Alibi.Helpers;
using Alibi.Plugins.API;
using Alibi.Protocol;
using NetCoreServer;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Alibi
{
    public class ClientSession : TcpSession
    {
        public Client Client { get; private set; }

        private string LastSentPacket;
        private DateTime BanCheckTime;
        private int PacketCount;

        public ClientSession(TcpServer server) : base(server)
        {
        }

        protected override void OnConnected()
        {
            if (((Server)Server).ConnectedPlayers >= Alibi.Server.ServerConfiguration.MaxPlayers)
            {
                Send(new AOPacket("BD", "Max players has been reached."));
                Task.Delay(500);
                Disconnect();
            }

            BanCheckTime = DateTime.Now;

            var ip = ((IPEndPoint)Socket.RemoteEndPoint).Address;
            if (Alibi.Server.ServerConfiguration.Advertise && ip.Equals(Alibi.Server.MasterServerIp))
                Alibi.Server.Logger.Log(LogSeverity.Info, " Probed by master server.", true);
            Client = new Client(Server as Server, this, ip);
            Client.LastAlive = DateTime.Now;
            Client.KickIfBanned();

            // fuck fantaencrypt
            SendAsync(new AOPacket("decryptor", "NOENCRYPT"));
        }

        protected override void OnDisconnected()
        {
            ((Server)Server).ClientsConnected.Remove(Client);
            if (Client.Connected)
            {
                ((Server)Server).ConnectedPlayers--;
                ((Area)Client.Area)!.PlayerCount--;
                Client.Connected = false;
                if (Client.Character != null)
                    Client.Area.TakenCharacters[(int)Client.Character] = false;
                Client.Area.UpdateTakenCharacters();
                Client.Area.CurrentCaseManagers.Remove(Client);
                Client.Area.AreaUpdate(AreaUpdateType.PlayerCount);
                Client.Area.AreaUpdate(AreaUpdateType.CourtManager);
            }
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            string msg = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            string[] dissallowedRequests = "GET;HEAD;POST;PUT;DELETE;TRACE;OPTIONS;CONNECT;PATCH".Split(';');
            if (dissallowedRequests.Any(r => msg.StartsWith(r)))
                return;
            string[] packets = msg.Split("%", StringSplitOptions.RemoveEmptyEntries);
            foreach (var packet in packets)
            {
                if (Client.HardwareId == null && !packet.StartsWith("HI#"))
                    return;
                if (DateTime.Now.CompareTo(BanCheckTime.AddSeconds
                    (Alibi.Server.ServerConfiguration.RateLimitResetTime)) >= 0)
                {
                    PacketCount = 0;
                    BanCheckTime = DateTime.Now;
                }
                if (PacketCount >= Alibi.Server.ServerConfiguration.RateLimit)
                    Client.BanIp("You have been rate limited.", Alibi.Server.ServerConfiguration.RateLimitBanLength);
                PacketCount++;
                LastSentPacket = packet;
                MessageHandler.HandleMessage(Client, AOPacket.FromMessage(packet));
            }
            Client.LastAlive = DateTime.Now;
        }
    }
}
