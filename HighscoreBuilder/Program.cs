
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace HighscoreBuilder
{
    class Program
    {
        private static readonly List<string> pages = new List<string>()
        {
            "whitecat",
            "vaxei",
            "idke",
            "karthy",
            "xli",
            "chocomint",
            "aricin",
            "varvalian",
            "flyingtuna",
            "rafis",
            "mathi",
            "dustice",
            "alumetri",
            "micca",
            "merami",
            "freddie benson",
            "spare",
            "okinamo",
            "mightydoc",
            "danyl",
            "dereban",
            "arnold24x24",
            "moeyandere",
            "filsdelama",
            "azr8",
            "fieryrage",
            "firebat92",
            "morgn",
            "abyssal",
            "umbre",
            "a_blue",
            "asecretbox",
            "andros",
            "beasttrollmc",
            "mrekk",
            "badeu",
            "shiroha",
            "uyghti",
            "vinno",
            "bartek22830",
            "jpeg",
            "vamhi",
            "toy",
            "wubwoofwolf",
            "informous",
            "bubbleman",
            "aireu",
            "-duckleader-",
            "doki",
            "fgsky",
            "mouseeasy",
            // ---
            "im a fancy lad",
            "rohulk",
            "adamqs",
            "jordan the bear",
            "xilver15",
            "unko",
            "red_pixel",
            "epiphany",
            "woey",
            "heaven punisher",
            "gfmrt",
            "monk the don",
            "-gn",
            "rrtyui",
            "mismagius",
            "ajt",
            "digitalhypno",
            "ekoro",
            "riviclia",
            "ming3012",
            "stanr"
        };

        private const string user_page = "https://newpp.stanr.info/api/getresults?player=";

        private const string score_template = "<tr><td style=\"text-align: center; width: 32px\">{0}</td>" +
            "<td style=\"text-align: center\"><a href=\"/user/{1}_full\">{2}</a></td>" +
            "<td style=\"text-align: center\">{3}</td>" +
            "<td style=\"text-align: center\">{4}</td>" +
            "</tr>";

        private const double pp_cutoff = 699.5;

        private static HttpClient http = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var template = File.ReadAllText("template.html");

            var scores = new ConcurrentBag<ScoreModel>();
            Parallel.ForEach(pages, (player) =>
            {
                var playerLink = $"{user_page}{player}_full";
                var jsonString = http.GetStringAsync(playerLink).Result;
                if (!string.IsNullOrEmpty(jsonString))
                {
                    var json = JObject.Parse(jsonString);
                    var playerCapitalized = json["username"].ToString();
                    var playerScores = json["beatmaps"].ToObject<ScoreModel[]>();

                    foreach (var score in playerScores)
                    {
                        if (score.LocalPP > pp_cutoff)
                        {
                            score.Player = playerCapitalized;
                            scores.Add(score);
                        }
                    }
                }
            });

            var orderedScores = scores.OrderByDescending(x => x.LocalPP).ToArray();

            var sb = new StringBuilder();
            for (int i = 0; i < orderedScores.Length; i++)
            {
                var score = orderedScores[i];

                sb.AppendLine(string.Format(score_template,
                    (i + 1).ToString(),
                    score.Player.ToLower(),
                    score.Player,
                    formatBeatmap(score.Beatmap),
                    score.LocalPP));
            }

            File.WriteAllText("highscores_full.html", template.Replace("{scores}", sb.ToString()));
        }

        private static string formatBeatmap(string beatmap)
        {
            var modsAndAccuracyIndex = beatmap.LastIndexOf("] ") + 1;
            var modsAndAccuracySplit = beatmap.Substring(modsAndAccuracyIndex + 1).Split(" (");

            var mods = string.Join(" ", modsAndAccuracySplit[0].Replace("+", "")
                .Split(", ")
                .OrderBy(x => x)
                .Select(item => $"<span class=\"badge badge-light\">{item}</span> "));

            var accuracy = string.Empty;
            if (modsAndAccuracySplit.Length > 1)
                accuracy = '(' + modsAndAccuracySplit[1];

            beatmap = beatmap.Substring(0, modsAndAccuracyIndex);
            var split = beatmap.Split(" - ").ToList();
            var beatmapId = split[0];
            split.RemoveAt(0);

            return $"<a href=\"https://osu.ppy.sh/b/{beatmapId}\">{string.Join(" - ", split)}</a> {mods}{accuracy}";
        }
    }
}
