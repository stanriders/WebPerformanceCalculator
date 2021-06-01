
using System.Linq;
using System.Text.RegularExpressions;

namespace WebPerformanceCalculator.Parsers
{
    public class BeatmapLinkData
    {
        public uint Id { get; set; }
        public bool IsBeatmapset { get; set; }
    }

    public static class BeatmapLinkParser
    {
        private static readonly Regex linkRegex =
            new(@"(?>https?:\/\/)?(?>osu|old)\.ppy\.sh\/([b,s]|(?>beatmaps)|(?>beatmapsets))\/(\d+)\/?\#?(?>\w+)?\/?(\d+)?\/?(?>[&,?].+=\w+)?",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static BeatmapLinkData? Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var regexMatch = linkRegex.Match(text);
            if (regexMatch.Groups.Count > 1)
            {
                // Groups:
                // 0 - full match
                // 1 - link type (b/s/beatmapsets/beatmaps)
                // 2 - beatmapset id (beatmap id if link type is 'beatmaps/b/s')
                // 3 - beatmap id
                var regexGroups = regexMatch.Groups.Values.Where(x => x.Success).ToArray();

                bool isNew = regexGroups[1].Value != "b" && regexGroups[1].Value != "s"; // are we using new website or not
                bool isSet = (regexGroups[1].Value == "beatmapsets" && regexGroups.Length < 4) || regexGroups[1].Value == "s";

                if (isNew)
                {
                    if (isSet)
                    {
                        return new BeatmapLinkData
                        {
                            Id = uint.Parse(regexGroups[2].Value),
                            IsBeatmapset = isSet
                        };
                    }

                    return new BeatmapLinkData
                    {
                        // 'beatmaps' case is stupid and since it's literally one of a kind we're accounting for it here
                        Id = regexGroups[1].Value == "beatmaps" ? uint.Parse(regexGroups[2].Value) : uint.Parse(regexGroups[3].Value),
                        IsBeatmapset = false
                    };
                }

                return new BeatmapLinkData
                {
                    Id = uint.Parse(regexGroups[2].Value),
                    IsBeatmapset = isSet
                };
            }

            return null;
        }
    }
}
