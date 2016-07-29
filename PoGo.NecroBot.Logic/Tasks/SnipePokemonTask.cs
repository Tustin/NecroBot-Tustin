using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using PoGo.NecroBot.Logic.Logging;
using POGOProtos.Enums;
using POGOProtos.Networking.Responses;

namespace PoGo.NecroBot.Logic.Tasks
{
    public static class SnipePokemonTask
    {
        public static async Task Execute(ISession session, bool startup = false)
        {
            List<Snipe.SnipeResult> contents = Snipe.ReadFile(Directory.GetCurrentDirectory() + "\\Config\\snipe.txt");
            if (contents.Count > 0)
            {
                foreach (Snipe.SnipeResult s in contents)
                {
                    await SnipePokemonTask.SnipePokemon(session, s.location, s.pokemonId);
                }
                File.WriteAllText(Directory.GetCurrentDirectory() + "\\Config\\snipe.txt", string.Empty);
            }
            else if (startup)
                Logger.Write("Skipping snipe because Config/snipe.txt is empty or doesn't exist.", Logging.LogLevel.Warning);
        }

        public static async Task SnipePokemon(ISession session, Location snipeLocation, PokemonId pokemonId)
        {
            Tuple<double, double, double> curCoords = new Tuple<double, double, double>(session.Client.CurrentLatitude, session.Client.CurrentLongitude, session.Client.CurrentAltitude);
            await session.Client.Player.UpdatePlayerLocation(snipeLocation.Latitude, snipeLocation.Longitude, session.Settings.DefaultAltitude);
            Logger.Write($"Trying to snipe a { pokemonId } @ { snipeLocation.Latitude } : { snipeLocation.Longitude }", LogLevel.Snipe);
            var pokemons = await CatchNearbyPokemonsTask.GetNearbyPokemons(session);
            var pokemon = pokemons.Where(i => i.PokemonId == pokemonId);
            if (pokemon == null || pokemon.Count() == 0)
            {
                Logger.Write($"Unable to find { pokemonId } at location... Returning to original spot.", LogLevel.Snipe);
                await session.Client.Player.UpdatePlayerLocation(curCoords.Item1, curCoords.Item2, curCoords.Item3);
                return;
            }

            Logger.Write($"Found { pokemon.Count() } { pokemonId }'s at location. Trying to catch them ALL!", LogLevel.Snipe);
            foreach (var p in pokemon)
            {
                var encounter = session.Client.Encounter.EncounterPokemon(p.EncounterId, p.SpawnPointId).Result;
                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    await session.Client.Player.UpdatePlayerLocation(curCoords.Item1, curCoords.Item2, curCoords.Item3);
                    await CatchPokemonTask.Execute(session, encounter, p);
                }
                else if (encounter.Status == EncounterResponse.Types.Status.EncounterNotInRange)
                {
                    Logger.Write($"{pokemonId } is out of range. Moving closer....", LogLevel.Snipe);
                    await session.Client.Player.UpdatePlayerLocation(p.Latitude, p.Longitude, curCoords.Item3);
                    encounter = session.Client.Encounter.EncounterPokemon(p.EncounterId, p.SpawnPointId).Result;
                    if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                    {
                        await session.Client.Player.UpdatePlayerLocation(curCoords.Item1, curCoords.Item2, curCoords.Item3);
                        await CatchPokemonTask.Execute(session, encounter, p);
                    }
                    else
                    {
                        await session.Client.Player.UpdatePlayerLocation(curCoords.Item1, curCoords.Item2, curCoords.Item3);
                        Logger.Write($"Failed to catch again ({ encounter.Status }). Moving back to original coords.", LogLevel.Snipe);
                        return;
                    }
                }
                else
                {
                    await session.Client.Player.UpdatePlayerLocation(curCoords.Item1, curCoords.Item2, curCoords.Item3);
                    Logger.Write($"Error occurred when entering encounter: { encounter.Status }.", LogLevel.Snipe);
                    return;
                }
            }
        }
    }
}
