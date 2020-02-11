# WebPerformanceCalculator
https://newpp.stanr.info/

## API
[GET] `/api/GetCalcModuleUpdateDate`  
Get calculation module update date and commit hash / name  

---
[GET] `/api/GetTop?offset=0&limit=50&search=chocomint&order=desc&sort=localPP&country=KR`  
Get leaderboard page  
Params:
* `search` - Player name or jsonname, doesn't work with `country`
* `sort` - Which field results should be sorted by
* `order` - Which order (`asc`/`desc`) should results be order by
* `country` - Country acronym to get a country leaderboard page, doesn't work with `search`
* `offset` - How many rows to skip
* `limit` - How many rows to return  

---
[GET] `/api/GetResults?jsonUsername=cookiezi`  
Get user profile  
Params:
* `jsonUsername` - `jsonName` to use, can be obtained from GetTop  

---
[GET] `/api/GetQueue`  
Get current queue  

---
[POST] `/api/AddToQueue?jsonUsername=nathan on osu`  
Add player to queue and returns current queue  
Params (urlencoded):
* `jsonUsername` = Player nickname or ID

---
[POST] `/api/CalculateMap`  
Calculate map pp values for 90-100 acc values  
Params (json):
* `Map` - Beatmap ID, can't be BeatmapSet ID
* `Mods` - Array of mod abbreviations

---
[GET] `/api/GetProbabilityChart?mapId=129891&mods=HDDT`  
Get miss probability data for calculated map  
Params:
* `mapId` - Beatmap ID
* `mods` - Joined string of mod abbreviations

---
[GET] `/api/GetHighscores`  
Get current top scores sorted by local PP

---
[GET] `/api/GetCountries`  
Get all known countries in the player database  

---

Requires [osu-tools](https://github.com/stanriders/osu-tools) built in the same folder as workers for pp calculation and WebPerformanceCalculator for individual map calculation.  
  
ASP.NET Core 3.0

