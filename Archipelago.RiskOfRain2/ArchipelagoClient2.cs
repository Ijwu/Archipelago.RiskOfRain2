using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Extensions;
using Archipelago.RiskOfRain2.Handlers;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archipelago.RiskOfRain2
{
    internal class ArchipelagoClient2
    {
        public delegate void ClientDisconnected(ushort code, string reason, bool wasClean);
        public event ClientDisconnected OnClientDisconnect;

        public ArchipelagoSession Session { get; private set; }
        public ReceivedItemsHandler Items { get; private set; }
        public LocationChecksHandler Locations { get; private set; }
        public UIModuleHandler UI { get; private set; }
        public Color AccentColor { get; private set; }

        private bool enableDeathLink;
        private DeathLinkDifficulty deathlinkDifficulty;
        private DeathLinkService deathLinkService;

        public void Connect(string hostname, int port, string slotName, string password = null, List<string> tags = null)
        {
            Session = ArchipelagoSessionFactory.CreateSession(hostname, port);
            Items = new ReceivedItemsHandler(Session.Items);
            Locations = new LocationChecksHandler(Session.Locations);
            UI = new UIModuleHandler(this);
            Session.Socket.SocketClosed += Socket_SocketClosed;
            Session.Socket.PacketReceived += Socket_PacketReceived;

            if (enableDeathLink)
            {
                tags.Add("DeathLink");
            }

            Session.AttemptConnectAndLogin("Risk of Rain 2", slotName, new Version(0, 2, 0), tags, Guid.NewGuid().ToString(), password);

            if (enableDeathLink)
            {
                deathLinkService = Session.CreateDeathLinkServiceAndEnable();
            }

            Items.Hook();
            Locations.Hook();
            UI.Hook();

            if (Session.Socket.Connected)
            {
                ChatMessage.SendColored($"Succesfully connected to Archipelago at {hostname}:{port} for slot {slotName}.", Color.green);
            }
            else
            {
                ChatMessage.SendColored($"Failed to connect to Archipelago at {hostname}:{port} for slot {slotName}. Restart your run to try again. (Sorry)", Color.red);
            }
        }

        public void Disconnect()
        {
            Session.Socket.DisconnectAsync();
        }

        public void EnableDeathLink(DeathLinkDifficulty difficulty)
        {
            enableDeathLink = true;
            deathlinkDifficulty = difficulty;
        }

        public void SetAccentColor(Color accentColor)
        {
            AccentColor = accentColor;

            Log.LogDebug($"Accent Color set to: {accentColor.r} {accentColor.g} {accentColor.b} {accentColor.a}");
        }

        private void Socket_SocketClosed(WebSocketSharp.CloseEventArgs e)
        {
            Log.LogDebug($"Socket was disconnected. ({e.Code}) {e.Reason} (Clean? {e.WasClean})");

            Items.Unhook();
            Locations.Unhook();
            UI.Unhook();

            if (OnClientDisconnect != null)
            {
                OnClientDisconnect(e.Code, e.Reason, e.WasClean);
            }
        }

        private void Socket_PacketReceived(ArchipelagoPacketBase packet)
        {
            Log.LogDebug($"Received a packet of type: {packet.PacketType}");
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.Connected:
                {
                    var connectedPacket = (ConnectedPacket)packet;
                    var itemPickupStep = Convert.ToInt32(connectedPacket.SlotData["itemPickupStep"]) + 1;
                    var totalChecks = connectedPacket.LocationsChecked.Count + connectedPacket.MissingChecks.Count;
                    var currentChecks = connectedPacket.LocationsChecked.Count;

                    Locations.SetCheckCounts(totalChecks, itemPickupStep, currentChecks);
                    break;
                }
            }
        }
    }
}