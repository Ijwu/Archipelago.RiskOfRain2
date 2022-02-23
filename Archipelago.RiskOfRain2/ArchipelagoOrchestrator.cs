using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.RiskOfRain2.Enums;
using Archipelago.RiskOfRain2.Handlers;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace Archipelago.RiskOfRain2
{
    internal class ArchipelagoOrchestrator
    {
        public delegate void ClientDisconnected(ushort code, string reason, bool wasClean);
        public event ClientDisconnected OnClientDisconnect;

        public ArchipelagoSession Session { get; private set; }
        public ReceivedItemsHandler Items { get; private set; }
        public LocationChecksHandler Locations { get; private set; }
        public UIModuleHandler UI { get; private set; }
        public DeathLinkHandler DeathLink { get; private set; }
        public GameOverHandler GameOver { get; set; }
        public Color AccentColor { get; private set; }
        public bool ClientSideMode { get; private set; }

        private bool enableDeathLink;
        private DeathLinkDifficulty deathlinkDifficulty;
        private DeathLinkService deathLinkService;

        public ArchipelagoOrchestrator()
        {
            UI = new UIModuleHandler(this);
        }

        public void SetupClientsideMode()
        {
            ClientSideMode = true;
            Session = null;
            Items = null;
            Locations = null;
            GameOver = null;

            UI.Hook();
        }

        public void TeardownClientsideMode()
        {
            ClientSideMode = false;
            UI.Unhook();
        }

        public bool Login(string hostname, int port, string slotName, string password = null, List<string> tags = null)
        {
            Log.LogDebug($"Attempting connection to new session. Host: '{hostname}:{port}' Slot: '{slotName}'");
            TeardownClientsideMode();
            Session = ArchipelagoSessionFactory.CreateSession(hostname, port);
            Items = new ReceivedItemsHandler(Session.Items);
            Locations = new LocationChecksHandler(Session.Locations);
            GameOver = new GameOverHandler(Session.Socket);
            Session.Socket.SocketClosed += Socket_SocketClosed;
            Session.Socket.PacketReceived += Socket_PacketReceived;

            if (enableDeathLink)
            {
                tags = tags ?? new List<string>();
                tags.Add("DeathLink");
            }

            var loginResult = Session.TryConnectAndLogin("Risk of Rain 2", slotName, new Version(0, 2, 0), ItemsHandlingFlags.AllItems, tags, Guid.NewGuid().ToString(), password);
            if (!loginResult.Successful)
            {
                ChatMessage.SendColored($"Failed to connect to Archipelago at {hostname}:{port} for slot {slotName}. Restart your run to try again. (Sorry)", Color.red);
                return false;
            }

            HandleLoginSuccessful(loginResult as LoginSuccessful);

            Log.LogDebug($"Connection successful. DeathLink? '{enableDeathLink}'");
            if (enableDeathLink)
            {
                deathLinkService = Session.CreateDeathLinkServiceAndEnable();
                DeathLink = new DeathLinkHandler(deathLinkService, deathlinkDifficulty);
            }

            ChatMessage.SendColored($"Succesfully connected to Archipelago at {hostname}:{port} for slot {slotName}.", Color.green);
            return true;
        }

        public void HookEverything()
        {
            Items.Hook();
            Locations.Hook();
            UI.Hook();
            GameOver.Hook();
            DeathLink.Hook();
        }

        public void Disconnect()
        {
            Log.LogDebug("ArchipelagoClient.Disconnect() was executed.");
            Session.Socket.Disconnect();
            UnhookEverything();
            Session.Socket.SocketClosed -= Socket_SocketClosed;
            Session.Socket.PacketReceived -= Socket_PacketReceived;
        }

        public void EnableDeathLink(DeathLinkDifficulty difficulty)
        {
            enableDeathLink = true;
            deathlinkDifficulty = difficulty;
        }

        public void SetAccentColor(Color accentColor)
        {
            AccentColor = accentColor;

            Log.LogDebug($"Accent Color set to: '{accentColor.r} {accentColor.g} {accentColor.b} {accentColor.a}'");
        }

        private void HandleLoginSuccessful(LoginSuccessful loginSuccessful)
        {
            var itemPickupStep = Convert.ToInt32(loginSuccessful.SlotData["itemPickupStep"]) + 1;
            var totalChecks = loginSuccessful.LocationsChecked.Length + loginSuccessful.MissingChecks.Length;
            var completedChecks = loginSuccessful.LocationsChecked;
            var missingChecks = loginSuccessful.MissingChecks;

            PreGameController.instance.runSeed = ulong.Parse(loginSuccessful.SlotData["seed"].ToString());

            Locations.SetCheckCounts(totalChecks, itemPickupStep, completedChecks, missingChecks);
        }

        private void Socket_SocketClosed(WebSocketSharp.CloseEventArgs e)
        {
            Log.LogDebug($"Socket was disconnected. '({e.Code}) {e.Reason} (Clean? {e.WasClean})'");

            if (OnClientDisconnect != null)
            {
                OnClientDisconnect(e.Code, e.Reason, e.WasClean);
            }

            UnhookEverything();
        }

        private void UnhookEverything()
        {
            Items?.Unhook();
            Locations?.Unhook();
            UI?.Unhook();
            DeathLink?.Unhook();
            GameOver?.Unhook();
        }

        private void Socket_PacketReceived(ArchipelagoPacketBase packet)
        {
            Log.LogDebug($"Received a packet of type: '{packet.PacketType}'");
            switch (packet)
            {
                case PrintPacket printPacket:
                {
                    ChatMessage.Send(printPacket.Text);
                    break;
                }
                case PrintJsonPacket printJsonPacket:
                {
                    string text = "";
                    foreach (var part in printJsonPacket.Data)
                    {
                        switch (part.Type)
                        {
                            case JsonMessagePartType.PlayerId:
                            {
                                int playerId = int.Parse(part.Text);
                                text += Session.Players.GetPlayerAlias(playerId);
                                break;
                            }
                            case JsonMessagePartType.ItemId:
                            {
                                int itemId = int.Parse(part.Text);
                                text += Items.GetItemNameFromId(itemId);
                                break;
                            }
                            case JsonMessagePartType.LocationId:
                            {
                                int locationId = int.Parse(part.Text);
                                text += Locations.GetLocationNameFromId(locationId);
                                break;
                            }
                            default:
                            {
                                text += part.Text;
                                break;
                            }
                        }
                    }
                    ChatMessage.Send(text);
                    break;
                }
            }
        }
    }
}