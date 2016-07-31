using PoGo.NecroBot.Logic.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;
using POGOProtos.Networking.Responses;
using System.Collections;
using PoGo.NecroBot.Logic.Logging;

namespace PoGo.NecroBot.Logic.Tasks
{
    class CatchBurstPokemonTask
    {
        public static async Task<IEnumerable<Action>> Execute(ISession session, CancellationToken cancellationToken, Location loc)
        {
            var actionList = new List<Action>();
            await session.Client.Player.UpdatePlayerLocation(loc.Latitude, loc.Longitude, 10);

            var pokemon = (await CatchPokemonTask.GetNearbyPokemonClosestFirst(session)).DistinctBy(i => i.SpawnPointId).ToList();
            foreach (var mapPokemon in pokemon)
            {
                if (session.LogicSettings.UsePokemonToNotCatchFilter && session.LogicSettings.PokemonsNotToCatch.Contains(mapPokemon.PokemonId))
                {
                    continue;
                }
                var encounter = await session.Client.Encounter.EncounterPokemon(mapPokemon.EncounterId, mapPokemon.SpawnPointId);
                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    actionList.Add(async () =>
                    {
                        try
                        {
                            await CatchPokemonTask.Execute(session, encounter, mapPokemon);
                        }
                        catch (Exception ex)
                        {
                            Logger.Write($"Unable to burst catch pokemon", LogLevel.Error, ConsoleColor.Red);
                        }
                    });
                }
            }
            return actionList;
        }

        public static async Task Search(ISession session, CancellationToken cancellationToken)
        {
            Location currentLoc = new Location { Latitude = session.Client.CurrentLatitude, Longitude = session.Client.CurrentLongitude };
            var basePokemon = (await CatchBurstPokemonTask.Execute(session, cancellationToken, currentLoc));
            await session.Client.Player.UpdatePlayerLocation(currentLoc.Latitude, currentLoc.Longitude, 10);
            basePokemon.ForEach(x => x.Invoke());
            //basePokemon.ForEach(x => x.Start());
            //Task.WaitAll(basePokemon.ToArray());

            var tl = await CatchBurstPokemonTask.Execute(session, cancellationToken, new Location { Latitude = currentLoc.Latitude + .002, Longitude = currentLoc.Longitude + .002 });
            var bl = await CatchBurstPokemonTask.Execute(session, cancellationToken, new Location { Latitude = currentLoc.Latitude - .002, Longitude = currentLoc.Longitude + .002 });
            var tr = await CatchBurstPokemonTask.Execute(session, cancellationToken, new Location { Latitude = currentLoc.Latitude + .002, Longitude = currentLoc.Longitude - .002 });
            var br = await CatchBurstPokemonTask.Execute(session, cancellationToken, new Location { Latitude = currentLoc.Latitude - .002, Longitude = currentLoc.Longitude - .002 });
            await session.Client.Player.UpdatePlayerLocation(currentLoc.Latitude, currentLoc.Longitude, 10);

            tl.ForEach(x => x.Invoke());
            bl.ForEach(x => x.Invoke());
            tr.ForEach(x => x.Invoke());
            br.ForEach(x => x.Invoke());

        }
    }
}
