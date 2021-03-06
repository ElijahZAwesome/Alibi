﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Alibi.Plugins.API;
using Newtonsoft.Json;

namespace Alibi
{
    public class Area : IArea
    {
        [JsonIgnore] internal Server Server = null!;

        public int EvidenceModifications { get; set; } = 0;
        public string Name { get; set; } = "AreaName";
        public string Background { get; set; } = "gs4";
        public bool CanLock { get; set; } = true;
        public bool BackgroundLocked { get; set; } = false;
        public bool IniSwappingAllowed { get; set; } = true;
        public string Status { get; set; } = "IDLE";

        [JsonIgnore] public string Locked { get; set; } = "FREE";

        [JsonIgnore] public int PlayerCount { get; set; } = 0;

        [JsonIgnore] public List<IClient> CurrentCaseManagers { get; } = new List<IClient>();

        [JsonIgnore] public string? Document { get; set; }

        [JsonIgnore] public int DefendantHp { get; set; } = 10;

        [JsonIgnore] public int ProsecutorHp { get; set; } = 10;

        [JsonIgnore] public bool[] TakenCharacters { get; set; } = null!;

        [JsonIgnore] public List<IEvidence> EvidenceList { get; } = new List<IEvidence>();

        public void Broadcast(AOPacket packet)
        {
            var clientQueue = new Queue<IClient>(Server.ClientsConnected);
            while (clientQueue.Any())
            {
                var client = clientQueue.Dequeue();
                if (client is {Connected: true} && client.Area == this)
                    client.Send(packet);
            }
        }

        public void BroadcastOocMessage(string message, string? sender = null)
        {
            Broadcast(new AOPacket("CT", sender ?? "Server", message, "1")); // TODO: Why the fuck is there a 1 here
        }

        public void AreaUpdate(AreaUpdateType type) => AreaUpdate(type, null);

        public void AreaUpdate(AreaUpdateType type, IClient? client)
        {
            var updateData = new List<string> {((int) type).ToString()};
            foreach (var area in Server.Areas)
                switch (type)
                {
                    case AreaUpdateType.PlayerCount:
                        updateData.Add(area.PlayerCount.ToString());
                        break;
                    case AreaUpdateType.Status:
                        updateData.Add(area.Status);
                        break;
                    case AreaUpdateType.CourtManager:
                        updateData.Add(area.CurrentCaseManagers.Count <= 0
                            ? "FREE"
                            : string.Join(',', area.CurrentCaseManagers.Select(c => c.CharacterName)));
                        break;
                    case AreaUpdateType.Locked:
                        updateData.Add(area.Locked);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }

            if (client == null)
                Server.Broadcast(new AOPacket("ARUP", updateData.ToArray()));
            else
                client.Send(new AOPacket("ARUP", updateData.ToArray()));
        }

        public void FullUpdate() => FullUpdate(null);

        public void FullUpdate(IClient? client)
        {
            AreaUpdate(AreaUpdateType.PlayerCount, client);
            AreaUpdate(AreaUpdateType.Status, client);
            AreaUpdate(AreaUpdateType.CourtManager, client);
            AreaUpdate(AreaUpdateType.Locked, client);
        }

        public bool IsClientCM(IClient client) => CurrentCaseManagers.Contains(client);

        public void UpdateTakenCharacters()
        {
            var takenData = new List<string>(Server.CharactersList.Length);
            for (var i = 0; i < Server.CharactersList.Length; i++) takenData.Add(TakenCharacters[i] ? "-1" : "0");

            Broadcast(new AOPacket("CharsCheck", takenData.ToArray()));
        }
    }
}