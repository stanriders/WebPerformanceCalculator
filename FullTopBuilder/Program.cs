using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

// ReSharper disable StringLiteralTypo

namespace FullTopBuilder
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
            "stanr"
        };

        private const string website = "https://testpp.stanr.info/api";
        private const string user_page = website + "/getresults?player=";
        private const string test_auth = "Z2FzaGE6c2luZ2xldGFw";

        private const string user_template = "<tr><td style=\"text-align: center; width: 32px\">{0}</td>" +
            "<td style=\"text-align: center\">{6}<a href=\"/user/{1}_full\">{2}</a></td>" +
            "<td style=\"text-align: center\">{3}</td>" +
            "<td style=\"text-align: center\">{4}</td>" +
            "<td style=\"text-align: center\">{5}</td>" +
            "</tr>";

        private const string country_template = "<a href=\"/countrytop/{0}\"><img style=\"width: 32px; padding: 0 5px;\" src=\"https://osu.ppy.sh/images/flags/{0}.png\"></a>";

        private const int rank_separator = 50;

        private static HttpClient http = new HttpClient();

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", test_auth);

            var template = File.ReadAllText("template.html");

            var updateDate = DateTime.Parse(JToken.Parse(http.GetStringAsync(website + "/GetCalcModuleUpdateDate").Result)["date"].ToString());

            var players = new ConcurrentBag<PlayerModel>();
            Parallel.ForEach(pages, (player) =>
            {
                var playerLink = $"{user_page}{player}_full";
                var jsonString = http.GetStringAsync(playerLink).Result;
                if (!string.IsNullOrEmpty(jsonString))
                {
                    var json = JsonConvert.DeserializeObject<PlayerModel>(jsonString);

                    if (!string.IsNullOrEmpty(json.LocalPP))
                    {
                        var bracketIndex = json.LocalPP.IndexOf('(') + 1;
                        json.PPChange = json.LocalPP.Substring(bracketIndex, json.LocalPP.Length - bracketIndex - 1);
                        json.LivePP = json.LivePP.Substring(0, json.LivePP.IndexOf(' '));
                        json.SitePP = json.SitePP?.Substring(0, json.SitePP.IndexOf(' '));
                        json.LocalPP = json.LocalPP.Substring(0, json.LocalPP.IndexOf(' '));
                        json.LocalPPNumeric = Double.Parse(json.LocalPP, CultureInfo.InvariantCulture);
                        if (!string.IsNullOrEmpty(json.SitePP))
                            json.SitePPNumeric = Double.Parse(json.SitePP, CultureInfo.InvariantCulture);
                        
                        if (json.UpdateDate < updateDate)
                            json.Outdated = true;

                        players.Add(json);
                    }
                }
            });

            var orderedPlayers = players.OrderByDescending(x => x.LocalPPNumeric).ToArray();

            var sb = new StringBuilder();
            for (int i = 0; i < orderedPlayers.Length; i++)
            {
                var player = orderedPlayers[i];

                if (i == rank_separator)
                    sb.AppendLine(string.Format(user_template, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty));

                var ppChange = player.PPChange;
                if (player.SitePPNumeric != null)
                    ppChange = (player.LocalPPNumeric - player.SitePPNumeric)?.ToString("#.##");

                sb.AppendLine(string.Format(user_template, 
                    i < rank_separator ? (i + 1).ToString() : "-", 
                    player.Username.ToLower(), 
                    player.Username + (player.Outdated ? " (outdated)" : string.Empty), 
                    player.LivePP,
                    player.LocalPP,
                    ppChange,
                    string.Format(country_template, player.UserCountry)));
            }

            var updateMonth = DateTime.Today.AddDays(-DateTime.Today.Day + 1);
            File.WriteAllText("top_full.html", template.Replace("{updatedate}", updateMonth.ToString(CultureInfo.InvariantCulture))
                                                                    .Replace("{players}", sb.ToString()));
        }
    }
}
