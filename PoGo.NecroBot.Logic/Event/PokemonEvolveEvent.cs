#region using directives

using POGOProtos.Enums;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.NecroBot.Logic.Event
{
    public class PokemonEvolveEvent : IEvent
    {
        public int Exp;
        public PokemonId Id;
        public PokemonId nId;
        public EvolvePokemonResponse.Types.Result Result;
        public int Cp;
        public int nCp;
    }
}