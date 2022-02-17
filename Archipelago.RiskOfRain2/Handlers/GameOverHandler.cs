using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using RoR2;

namespace Archipelago.RiskOfRain2.Handlers
{
    internal class GameOverHandler : IHandleSomething
    {
        private readonly ArchipelagoSocketHelper socket;

        public GameOverHandler(ArchipelagoSocketHelper socket)
        {
            this.socket = socket;
        }

        public void Hook()
        {
            On.RoR2.Run.BeginGameOver += Run_BeginGameOver;
        }

        public void Unhook()
        {
            On.RoR2.Run.BeginGameOver -= Run_BeginGameOver;
        }

        private void Run_BeginGameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef)
        {
            var acceptableEndings = new[] { RoR2Content.GameEndings.MainEnding, RoR2Content.GameEndings.ObliterationEnding, RoR2Content.GameEndings.LimboEnding };
            var isAcceptableEnding = acceptableEndings.Contains(gameEndingDef) ||
                                    (gameEndingDef == RoR2Content.GameEndings.StandardLoss && Stage.instance.sceneDef.baseSceneName.StartsWith("moon")) ||
                                    (gameEndingDef == RoR2Content.GameEndings.StandardLoss && Stage.instance.sceneDef.baseSceneName.StartsWith("limbo"));

            // Are we in commencement or have we obliterated or have we finished (or lost in) 'a moment, whole'?
            if (isAcceptableEnding)
            {
                var packet = new StatusUpdatePacket();
                packet.Status = ArchipelagoClientState.ClientGoal;
                socket.SendPacket(packet);
            }

            orig(self, gameEndingDef);
        }
    }
}
