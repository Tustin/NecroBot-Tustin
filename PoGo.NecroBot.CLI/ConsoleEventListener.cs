#region using directives

using System;
using PoGo.NecroBot.Logic.Common;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using POGOProtos.Inventory.Item;
using POGOProtos.Networking.Responses;
using System.Threading;

#endregion

namespace PoGo.NecroBot.CLI
{
    public class ConsoleEventListener
    {
        public void HandleEvent(ProfileEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventProfileLogin,
                evt.Profile.PlayerData.Username ?? ""));
        }

        public void HandleEvent(ErrorEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Error);
        }

        public void HandleEvent(NoticeEvent evt, ISession session)
        {
            Logger.Write(evt.ToString());
        }

        public void HandleEvent(WarnEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Warning);

            if (evt.RequireInput)
            {
                Logger.Write(session.Translation.GetTranslation(TranslationString.RequireInputText));
                Console.ReadKey();
            }

        }

        public void HandleEvent(UseLuckyEggEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventUsedLuckyEgg, evt.Count), LogLevel.Egg);
        }

        public void HandleEvent(PokemonEvolveEvent evt, ISession session)
        {
            Logger.Write(evt.Result == EvolvePokemonResponse.Types.Result.Success
                ? session.Translation.GetTranslation(TranslationString.EventPokemonEvolvedSuccess, evt.Id, evt.nId, evt.Cp, evt.nCp, evt.Exp, evt.Result)
                : session.Translation.GetTranslation(TranslationString.EventPokemonEvolvedFailed, evt.Id, evt.Result,
                    evt.Id),
                LogLevel.Evolve);
        }

        public void HandleEvent(TransferPokemonEvent evt, ISession session)
        {
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventPokemonTransferred, evt.Id, evt.Cp,
                    evt.Perfection.ToString("0.00"), evt.BestCp, evt.BestPerfection.ToString("0.00"), evt.FamilyCandies),
                LogLevel.Transfer);
        }

        public void HandleEvent(ItemRecycledEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventItemRecycled, evt.Count, evt.Id.ToString().Substring(4)),
                LogLevel.Recycling);
        }

        public void HandleEvent(EggIncubatorStatusEvent evt, ISession session)
        {
            Logger.Write(evt.WasAddedNow
                ? session.Translation.GetTranslation(TranslationString.IncubatorPuttingEgg, evt.KmRemaining)
                : session.Translation.GetTranslation(TranslationString.IncubatorStatusUpdate, evt.KmRemaining), LogLevel.Egg);
        }

        public void HandleEvent(EggHatchedEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.IncubatorEggHatched, evt.PokemonId.ToString()), LogLevel.Egg);
        }

        public void HandleEvent(FortUsedEvent evt, ISession session)
        {
            string itemString;
            if (evt.inventoryFull)
            {
                itemString = session.Translation.GetTranslation(TranslationString.InvFullPokestopLooting);
            } else {
                itemString = evt.Items;
            }
                Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventFortUsed, evt.Name, evt.Exp, evt.Gems, itemString),
                LogLevel.Pokestop);
        }

        public void HandleEvent(FortFailedEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventFortFailed, evt.Name, evt.Try, evt.Max),
                LogLevel.Pokestop, ConsoleColor.DarkRed);
        }

        public void HandleEvent(FortTargetEvent evt, ISession session)
        {
            /*Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventFortTargeted, evt.Name, Math.Round(evt.Distance)),
                LogLevel.Info, ConsoleColor.DarkRed);*/
        }

        public void HandleEvent(PokemonCaptureEvent evt, ISession session)
        {
            Func<ItemId, string> returnRealBallName = a =>
            {
                switch (a)
                {
                    case ItemId.ItemPokeBall:
                        return session.Translation.GetTranslation(TranslationString.Pokeball);
                    case ItemId.ItemGreatBall:
                        return session.Translation.GetTranslation(TranslationString.GreatPokeball);
                    case ItemId.ItemUltraBall:
                        return session.Translation.GetTranslation(TranslationString.UltraPokeball);
                    case ItemId.ItemMasterBall:
                        return session.Translation.GetTranslation(TranslationString.MasterPokeball);
                    default:
                        return session.Translation.GetTranslation(TranslationString.CommonWordUnknown);
                }
            };

            var catchType = evt.CatchType;

            string strStatus;
            switch (evt.Status)
            {
                case CatchPokemonResponse.Types.CatchStatus.CatchError:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusError);
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchEscape:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusEscape);
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchFlee:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusFlee);
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchMissed:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusMissed);
                    break;
                case CatchPokemonResponse.Types.CatchStatus.CatchSuccess:
                    strStatus = session.Translation.GetTranslation(TranslationString.CatchStatusSuccess);
                    break;
                default:
                    strStatus = evt.Status.ToString();
                    break;
            }

            var catchStatus = evt.Attempt > 1
                ? session.Translation.GetTranslation(TranslationString.CatchStatusAttempt, strStatus, evt.Attempt)
                : session.Translation.GetTranslation(TranslationString.CatchStatus, strStatus);

            var familyCandies = evt.FamilyCandies > 0
                ? session.Translation.GetTranslation(TranslationString.Candies, evt.FamilyCandies)
                : "";
            if (evt.Id == POGOProtos.Enums.PokemonId.Snorlax        ||
                evt.Id == POGOProtos.Enums.PokemonId.Dragonite      ||
                evt.Id == POGOProtos.Enums.PokemonId.Venusaur       ||
                evt.Id == POGOProtos.Enums.PokemonId.Charizard      ||
                evt.Id == POGOProtos.Enums.PokemonId.Blastoise      ||
                evt.Id == POGOProtos.Enums.PokemonId.Kangaskhan     ||
                evt.Id == POGOProtos.Enums.PokemonId.Farfetchd      ||
                evt.Id == POGOProtos.Enums.PokemonId.MrMime         ||
                evt.Id == POGOProtos.Enums.PokemonId.Gyarados       ||
                evt.Id == POGOProtos.Enums.PokemonId.Lapras         ||
                evt.Id == POGOProtos.Enums.PokemonId.Ditto          ||
                evt.Id == POGOProtos.Enums.PokemonId.Vaporeon       ||
                evt.Id == POGOProtos.Enums.PokemonId.Jolteon        ||
                evt.Id == POGOProtos.Enums.PokemonId.Flareon        ||
                evt.Id == POGOProtos.Enums.PokemonId.Porygon        ||
                evt.Id == POGOProtos.Enums.PokemonId.Articuno       ||
                evt.Id == POGOProtos.Enums.PokemonId.Zapdos         ||
                evt.Id == POGOProtos.Enums.PokemonId.Moltres        ||
                evt.Id == POGOProtos.Enums.PokemonId.Mew            ||
                evt.Id == POGOProtos.Enums.PokemonId.Mewtwo         )
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (catchType == "Lure" || catchType == "Incense")
                Console.ForegroundColor = ConsoleColor.Green;
            else if (evt.Cp >= 2000)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (evt.Cp >= 1250)
                Console.ForegroundColor = ConsoleColor.Cyan;
            else Console.ForegroundColor = ConsoleColor.White;
            Logger.Write(
                session.Translation.GetTranslation(TranslationString.EventPokemonCapture, catchStatus, catchType, evt.Id,
                    evt.Level, evt.Cp, evt.MaxCp, evt.Perfection.ToString("0.00"), evt.Probability,
                    evt.Distance.ToString("F2"),
                    returnRealBallName(evt.Pokeball), evt.BallAmount, familyCandies), LogLevel.Caught);
        }

        public void HandleEvent(NoPokeballEvent evt, ISession session)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventNoPokeballs, evt.Id, evt.Cp),
                LogLevel.Caught);
        }

        public void HandleEvent(UseBerryEvent evt, ISession session)
        {
            Logger.Write(session.Translation.GetTranslation(TranslationString.EventNoPokeballs, evt.Count), LogLevel.Berry);
        }

        public void HandleEvent(DisplayHighestsPokemonEvent evt, ISession session)
        {
            string strHeader;
            //PokemonData | CP | IV | Level
            switch (evt.SortedBy)
            {
                case "Level":
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestsLevelHeader);
                    break;
                case "IV":
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestsPerfectHeader);
                    break;
                case "CP":
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestsCpHeader);
                    break;
                default:
                    strHeader = session.Translation.GetTranslation(TranslationString.DisplayHighestsHeader);
                    break;
            }
            var strPerfect = session.Translation.GetTranslation(TranslationString.CommonWordPerfect);
            var strName = session.Translation.GetTranslation(TranslationString.CommonWordName).ToUpper();

            Logger.Write($"====== {strHeader} ======", LogLevel.Info, ConsoleColor.Yellow);
            foreach (var pokemon in evt.PokemonList)
                Logger.Write(
                    $"# CP {pokemon.Item1.Cp.ToString().PadLeft(4, ' ')}/{pokemon.Item2.ToString().PadLeft(4, ' ')} | ({pokemon.Item3.ToString("0.00")}% {strPerfect})\t| Lvl {pokemon.Item4.ToString("00")}\t {strName}: '{pokemon.Item1.PokemonId}'",
                    LogLevel.Info, ConsoleColor.Yellow);
        }

        public void HandleEvent(UpdateEvent evt, ISession session)
        {
            Logger.Write(evt.ToString(), LogLevel.Update);
        }

        public void Listen(IEvent evt, ISession session)
        {
            dynamic eve = evt;

            try
            {
                HandleEvent(eve, session);
            }
                // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }
    }
}