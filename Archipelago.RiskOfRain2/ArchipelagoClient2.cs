using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Extensions;
using RoR2;
using System;
using System.Collections.Generic;

namespace Archipelago.RiskOfRain2
{
    internal class ArchipelagoClient2
    {
        public delegate void ClientDisconnected(ushort code, string reason, bool wasClean);
        public event ClientDisconnected OnClientDisconnect;

        private ArchipelagoSession session;
        private ReceivedItemsHandler items;
        private LocationChecksHandler locations;
        private bool enableDeathLink;
        private DeathLinkDifficulty deathlinkDifficulty;
        private DeathLinkService deathLinkService;

        private void Socket_SocketClosed(WebSocketSharp.CloseEventArgs e)
        {
            Log.LogDebug($"Socket was disconnected. ({e.Code}) {e.Reason} (Clean? {e.WasClean})");

            items.Unhook();
            locations.Unhook();

            if (OnClientDisconnect != null)
            {
                OnClientDisconnect(e.Code, e.Reason, e.WasClean);
            }
        }

        public void Connect(string hostname, int port, string slotName, string password = null, List<string> tags = null)
        {
            session = ArchipelagoSessionFactory.CreateSession(hostname, port);
            items = new ReceivedItemsHandler(session.Items);
            locations = new LocationChecksHandler(session.Locations);
            session.Socket.SocketClosed += Socket_SocketClosed;

            if (enableDeathLink)
            {
                tags.Add("DeathLink");
            }

            session.AttemptConnectAndLogin("Risk of Rain 2", slotName, new Version(0, 2), tags, Guid.NewGuid().ToString(), password);

            if (enableDeathLink)
            {
                deathLinkService = session.CreateDeathLinkServiceAndEnable();
            }

            items.Hook();
            locations.Hook();
        }

        public void EnableDeathLink(DeathLinkDifficulty difficulty)
        {
            enableDeathLink = true;
            deathlinkDifficulty = difficulty;
        }
    }
}