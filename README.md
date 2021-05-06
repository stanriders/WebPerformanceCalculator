# WebPerformanceCalculator
https://newpp.stanr.info/

## API
**[GET] `/api/version`**  
Get calculation module update date and commit `hash / name` 

---
**[GET] `/api/top?offset=0&limit=50&search=chocomint&order=desc&sort=localPp&country=KR`**  
Get leaderboard page  
Params:
* `search` - Player name or jsonname, doesn't work with `country`
* `sort` - Which field results should be sorted by
* `order` - Which order (`asc`/`desc`) should results be order by
* `country` - Country acronym to get a country leaderboard page, doesn't work with `search`
* `offset` - How many rows to skip
* `limit` - How many rows to return (max 500)  

---
**[GET] `/api/player/cookiezi`**  
Get user profile  
Params:
* `name` - Player user id or nickname 

---
**[GET] `/api/queue`**  
Get current queue  

---
**[POST] `/api/queue?player=nathan on osu`**  
Add player to queue and returns current queue  
Params (url encoded):
* `player` = Player nickname or ID  

---
**[POST] `/api/maps/calculate`**  
Calculate map pp values for 90-100 acc values  
Params (json):
* `Map` - Beatmap ID, can't be BeatmapSet ID
* `Mods` - Array of mod abbreviations

---
**[GET] `/api/maps/probabilitychart/129891?mods=HDDT`**  
Get miss probability data for calculated map  
Params:
* `mapId` - Beatmap ID
* `mods` - Joined string of mod abbreviations

---
**[GET] `/api/highscores`**  
Get current top scores sorted by local PP

---
**[GET] `/api/countries`**  
Get all known countries in the player database  

---

## Running
Requires [osu-tools](https://github.com/stanriders/osu-tools) built in the same folder as workers for pp calculation.  

### appsettings.json
```
{
  "Urls": "http://localhost:5001",
  "Key": "abcdefghjklmnopqrstuvwxyz0123456789",  // admin/worker endpoints access key

  "HighscoreThreshold": 699.5,  // pp threshold for /highscores/
  "CalcsPerHourPerUser": 15,  // how many profiles one user can calc in an hour

  "CalculatorPath": "/home/pp/osu-tools/bin/netcoreapp3.1",  // path to a PerformanceCalculator.dll for map calculation
  "CalculationModuleFileName": "osu.Game.Rulesets.Osu.dll",
  "CalculationModuleUpdateLink": "http://localhost/osu.Game.Rulesets.Osu.dll",  // URL from which workers should download calc updates

  "CommitHashFileName": "commithash"  // path to file with commit hash, can be relative
}

```
  
ASP.NET 5.0

