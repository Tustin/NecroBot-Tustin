#region using directives

using System.Linq;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.PoGoUtils;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using POGOProtos.Enums;
using System.Collections.Generic;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class TransferDuplicatePokemonTask
    {
        public static async Task Execute(ISession session)
        {
            var duplicatePokemons =
                await
                    session.Inventory.GetDuplicatePokemonToTransfer(session.LogicSettings.KeepPokemonsThatCanEvolve,
                        session.LogicSettings.PrioritizeIvOverCp,
                        session.LogicSettings.PokemonsNotToTransfer);

            var pokemonSettings = await session.Inventory.GetPokemonSettings();
            var pokemonFamilies = await session.Inventory.GetPokemonFamilies();

            foreach (var duplicatePokemon in duplicatePokemons)
            {
                if (duplicatePokemon.Cp >= session.Inventory.GetPokemonTransferFilter(duplicatePokemon.PokemonId).KeepMinCp ||
                    PokemonInfo.CalculatePokemonPerfection(duplicatePokemon) >
                    session.Inventory.GetPokemonTransferFilter(duplicatePokemon.PokemonId).KeepMinIvPercentage)
                {
                    continue;
                }

                await session.Client.Inventory.TransferPokemon(duplicatePokemon.Id);
                await session.Inventory.DeletePokemonFromInvById(duplicatePokemon.Id);

                var bestPokemonOfType = (session.LogicSettings.PrioritizeIvOverCp
                    ? await session.Inventory.GetHighestPokemonOfTypeByIv(duplicatePokemon)
                    : await session.Inventory.GetHighestPokemonOfTypeByCp(duplicatePokemon)) ?? duplicatePokemon;

                var setting = pokemonSettings.Single(q => q.PokemonId == duplicatePokemon.PokemonId);
                var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);

                family.Candy++;

                session.EventDispatcher.Send(new TransferPokemonEvent
                {
                    Id = duplicatePokemon.PokemonId,
                    Perfection = PokemonInfo.CalculatePokemonPerfection(duplicatePokemon),
                    Cp = duplicatePokemon.Cp,
                    BestCp = bestPokemonOfType.Cp,
                    BestPerfection = PokemonInfo.CalculatePokemonPerfection(bestPokemonOfType),
                    FamilyCandies = family.Candy
                });

                DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 0);
            }
            if (session.LogicSettings.UsePokemonsToAlwaysDelete)
            {
                var alwaysDelete = await session.Inventory.GetAlwaysDeletePokemon(session.LogicSettings.PokemonsToAlwaysDelete);

                foreach (var deleteme in alwaysDelete)
                {
                    await session.Client.Inventory.TransferPokemon(deleteme.Id);
                    await session.Inventory.DeletePokemonFromInvById(deleteme.Id);

                    var setting = pokemonSettings.Single(q => q.PokemonId == deleteme.PokemonId);
                    var family = pokemonFamilies.First(q => q.FamilyId == setting.FamilyId);

                    family.Candy++;

                    session.EventDispatcher.Send(new TransferPokemonEvent
                    {
                        Id = deleteme.PokemonId,
                        Perfection = PokemonInfo.CalculatePokemonPerfection(deleteme),
                        Cp = deleteme.Cp,
                        BestCp = deleteme.Cp,
                        BestPerfection = PokemonInfo.CalculatePokemonPerfection(deleteme),
                        FamilyCandies = family.Candy
                    });

                    DelayingUtils.Delay(session.LogicSettings.DelayBetweenPlayerActions, 0);
                }
            }
        }
    }
}