using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using WebPerformanceCalculator.DbConvert.New;
using WebPerformanceCalculator.DbConvert.New.Types;
using WebPerformanceCalculator.DbConvert.Old;
using WebPerformanceCalculator.DbConvert.Old.Types;
using Player = WebPerformanceCalculator.DbConvert.New.Types.Player;
using Score = WebPerformanceCalculator.DbConvert.New.Types.Score;

var scoreRegex = new Regex("(\\d+) - (.+]) \\+?(.+)? \\((\\d+(?>\\.\\d+)?)%, (\\d+)\\/(\\d+)x(?>, (\\d+) \\w+)?\\)");
var livePpRegex = new Regex("(\\d+.\\d+) \\(\\D+(\\d+.\\d+)");

using var oldDb = new OldDbContext();
using var newDb = new NewDbContext();

var oldPlayers = oldDb.Players
                .OrderByDescending(x=> x.LocalPp)
                .Where(x=> x.LocalPp > 100.0) // skip players with very low pp
                .ToArray();

var playerCount = oldPlayers.Length;

List<int> playerList; // stupid hack but its much lighter than checking db for map existence
playerList = newDb.Players.Select(x => x.Id).ToList();
var addedPlayers = 0;

List<int> mapList; // stupid hack but its much lighter than checking db for map existence
mapList = newDb.Maps.Select(x=> x.Id).ToList();

Console.WriteLine($"Players: {playerCount}");

for (int i = 0; i < playerCount; i++)
{
    var oldPlayer = oldPlayers[i];

    var path = $"players/{oldPlayer.JsonName}.json";
    if (!File.Exists(path))
        continue;

    var updateDate = File.GetLastWriteTime(path).ToUniversalTime();

    // skip VERY outdated profiles 
    if (updateDate.AddMonths(18) < DateTime.UtcNow)
        continue;

    // skip malformed json
    var oldPlayerJson = JsonConvert.DeserializeObject<JsonPlayer>(File.ReadAllText(path));
    if (oldPlayerJson == null)
        continue;

    // ideally this should append scores but i dont want to bother yet
    if (playerList.Any(x => x == oldPlayerJson.UserID))
        continue;

    foreach (var score in oldPlayerJson.Beatmaps)
    {
        var regexMatches = scoreRegex.Match(score.Beatmap);
        if (!regexMatches.Success)
            continue;

        var mapId = int.Parse(regexMatches.Groups[1].Value);
        var mapName = regexMatches.Groups[2].Value;
        var mods = regexMatches.Groups[3].Value;
        var accuracy = double.Parse(regexMatches.Groups[4].Value);
        var combo = int.Parse(regexMatches.Groups[5].Value);
        var maxCombo = int.Parse(regexMatches.Groups[6].Value);
        var misses = 0;
        if (regexMatches.Groups[7].Success)
            misses = int.Parse(regexMatches.Groups[7].Value);

        if (score.PositionChange == "-")
            score.PositionChange = "0";

        if (!mapList.Contains(mapId))
        {
            mapList.Add(mapId);
            newDb.Maps.Add(new Map
            {
                Id = mapId,
                Name = mapName,
                MaxCombo = maxCombo
            });
        }

        newDb.Scores.Add(new Score
        {
            //Id = 0, // will autoupdate
            LocalPp = double.Parse(score.LocalPP),
            LivePp = double.Parse(score.LivePP),
            PlayerId = oldPlayerJson.UserID,
            UpdateTime = updateDate,
            MapId = mapId,
            Mods = mods,
            Accuracy = accuracy,
            Combo = combo,
            Misses = misses,
            AdditionalPpData = $"aimPP = {score.AimPP}, tapPP = {score.TapPP}, accPP = {score.AccPP}",
            PositionChange = int.Parse(score.PositionChange)
        });
    }

    var ppRegexMatches = livePpRegex.Match(oldPlayerJson.LivePP).Groups;

    newDb.Players.Add(new Player
    {
        Id = oldPlayerJson.UserID,
        Country = oldPlayerJson.UserCountry,
        Name = oldPlayerJson.Username,
        LocalPp = double.Parse(oldPlayerJson.LocalPP.Split(' ')[0]),
        LivePp = double.Parse(ppRegexMatches[1].Value),
        PlaycountPp = double.Parse(ppRegexMatches[2].Value),
        UpdateTime = updateDate
    });

    addedPlayers++;

    if (addedPlayers % 500 == 0)
    {
        Console.WriteLine($"\t {i} out of {playerCount}");
        newDb.SaveChanges();

        GC.Collect();
    }
}

newDb.SaveChanges();