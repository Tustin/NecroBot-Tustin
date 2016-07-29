using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using POGOProtos.Enums;
using PoGo.NecroBot.Logic.Logging;

namespace PoGo.NecroBot.Logic.Utils
{
    public class Snipe
    {
        public class SnipeResult
        {
            public Location location { get; set; }
            public PokemonId pokemonId { get; set; }
        }

        public static List<SnipeResult> ReadFile(string dir)
        {
            List<string> contents = File.Exists(dir) ? File.ReadAllLines(dir).ToList() : new List<string>();
            List<SnipeResult> ret = new List<SnipeResult>();
            if (contents.Count > 0)
            {
                foreach (string snipe in contents)
                {
                    double lat, longitude;
                    PokemonId pokemonId;
                    string long_tmp = snipe.Split(',')[1];
                    long_tmp = long_tmp.Substring(0, long_tmp.IndexOf(':'));
                    if (double.TryParse(snipe.Split(',')[0], out lat) && double.TryParse(long_tmp, out longitude) && Enum.TryParse(snipe.Split(':')[1], out pokemonId))
                        if (Math.Abs(lat) <= 90 && Math.Abs(longitude) <= 180)
                            ret.Add(new SnipeResult() { location = new Location(lat, longitude), pokemonId = pokemonId });
                        else
                            Logger.Write($"Invalid coordinates for { pokemonId} snipe", Logging.LogLevel.Snipe);
                    else
                        Logger.Write($"Error parsing line in config/snipe.txt: { snipe }", Logging.LogLevel.Snipe);
                }
            }
            return ret;
        }
    }
}