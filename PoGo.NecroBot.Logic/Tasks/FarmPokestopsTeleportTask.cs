#region using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeoCoordinatePortable;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using PokemonGo.RocketAPI.Extensions;
using POGOProtos.Map.Fort;
using POGOProtos.Networking.Responses;
using MoreLinq;

#endregion


namespace PoGo.NecroBot.Logic.Tasks
{
    class FarmPokestopsTeleportTask
    {
        public static async Task Execute(ISession session, CancellationToken cancellationToken)
        {
            FortData firstPokestop = null;
            var numberOfPokestopsVisited = 0;
            var returnToStart = DateTime.Now;
            var pokestopList = (await FarmPokestopsTask.GetPokeStops(session)).Where(t => t.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).ToList();
            while (true)
            {
                var isSniping = false;
                var loc = new KeyValuePair<double, double>();
                //if (_snipe.SnipeLocations.Count > 0)
                //{                
                //    if (_snipe.SnipeLocations.TryTake(out loc))
                //    {
                //        logger.Info($"Sniping pokemon at {loc.Key}, {loc.Value}");
                //        await _navigation.TeleportToLocation(loc.Key, loc.Value);
                //        isSniping = true;
                //    }
                //}
                //else if (returnToStart.AddMinutes(2) <= DateTime.Now)
                //{
                //    //var r = new Random((int)DateTime.Now.Ticks);
                //    //var nextLocation = _settings.LocationsToVisit.ElementAt(r.Next(_settings.LocationsToVisit.Count()));
                //    //await _navigation.TeleportToLocation(nextLocation.Key, nextLocation.Value);
                //    await _navigation.TeleportToPokestop(firstPokestop);
                //    returnToStart = DateTime.Now;
                //}
                await Navigation.TeleportToPokestop(session, firstPokestop);
                if (!pokestopList.Any())
                {
                    await Navigation.TeleportToPokestop(session, firstPokestop);
                    var oldPokestopList =
                        (await FarmPokestopsTask.GetPokeStops(session)).Where(
                            t => t.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).ToList();
                    if (oldPokestopList.Any())
                        pokestopList = oldPokestopList;
                }
                var newPokestopList = (await FarmPokestopsTask.GetPokeStops(session)).Where(t => t.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime()).ToList();
                if (newPokestopList.Any())
                    pokestopList = newPokestopList;
                if (!pokestopList.Any())
                    continue;

                var closestPokestop = pokestopList.OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude, session.Client.CurrentLongitude, i.Latitude, i.Longitude)).First();

                if (firstPokestop == null)
                    firstPokestop = closestPokestop;

                //if (_settings.Teleport)
                bool teleport = true;
                if (teleport)
                {
                    var distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude, session.Client.CurrentLongitude, closestPokestop.Latitude, closestPokestop.Longitude);

                    //var fortWithPokemon = (await _map.GetFortWithPokemon());
                    //var biggestFort = fortWithPokemon.MaxBy(x => x.GymPoints);
                    if (distance > 100)
                    {
                        var r = new Random((int)DateTime.Now.Ticks);
                        closestPokestop =
                            pokestopList.ElementAt(r.Next(pokestopList.Count));
                    }
                    await Navigation.TeleportToPokestop(session, closestPokestop);
                }
                else
                {
                    await
                        session.Navigation.HumanLikeWalking(
                            new GeoCoordinate(closestPokestop.Latitude, closestPokestop.Longitude),
                            session.LogicSettings.WalkingSpeedInKilometerPerHour,
                            async () =>
                            {
                                await CatchBurstPokemonTask.Search(session, cancellationToken);
                                return true;
                            },
                            cancellationToken);
                }

                //logger.Info("Moving to a pokestop");

                var pokestopBooty =
                    await session.Client.Fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
                if (pokestopBooty.ExperienceAwarded > 0)
                {
                    Logger.Write($"[{numberOfPokestopsVisited++}] Pokestop rewarded us with {pokestopBooty.ExperienceAwarded} exp. {pokestopBooty.GemsAwarded} gems. {StringUtils.GetSummedFriendlyNameOfItemAwardList(pokestopBooty.ItemsAwarded)}.", LogLevel.Info);
                    //_stats.ExperienceSinceStarted += pokestopBooty.ExperienceAwarded;
                    //_stats.
                }
                else
                {
                    await RemoveSoftBan(session, closestPokestop);
                }
                bool burstmode = true;
                if (isSniping)
                {
                    await RemoveSoftBan(session, closestPokestop);
                    Location savedLoc = new Location { Latitude = session.Client.CurrentLatitude, Longitude = session.Client.CurrentLongitude };
                    var burst = await CatchBurstPokemonTask.Execute(session, cancellationToken, new Location { Latitude = loc.Key, Longitude = loc.Value });
                    await session.Client.Player.UpdatePlayerLocation(savedLoc.Latitude, savedLoc.Longitude, 10);
                    burst.ForEach(a => a.Invoke());
                }
                else if (burstmode)
                {
                    await CatchBurstPokemonTask.Search(session, cancellationToken);
                    var lure = await CatchLurePokemonsTask.CatchLurePokemon(session, closestPokestop);
                    lure.Invoke();
                }
                else
                {
                    var task = (await CatchBurstPokemonTask.Execute(session, cancellationToken, new Location { Latitude = closestPokestop.Latitude, Longitude = closestPokestop.Longitude })).ToArray();
                    task.ForEach(x => x.Invoke());
                }

                await Task.Delay(100);
            }
        }

        private static async Task RemoveSoftBan(ISession session, FortData closestPokestop)
        {
            var pokestopBooty = await session.Client.Fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
            while (pokestopBooty.Result == FortSearchResponse.Types.Result.Success)
            {
                pokestopBooty = await session.Client.Fort.SearchFort(closestPokestop.Id, closestPokestop.Latitude, closestPokestop.Longitude);
            }
        }
    }
}
