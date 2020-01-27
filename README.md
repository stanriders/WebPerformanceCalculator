# WebPerformanceCalculator
https://newpp.stanr.info/

## API (json)
[GET] `/api/GetCalcModuleUpdateDate` - returns calculation module update date and commit hash  
[GET] `/api/GetTop?offset=0&limit=50&search={player name}&order={desc/asc}&sort=localPP&country={country if country top}` - returns leaderboard page  
[GET] `/api/GetResults?jsonUsername={jsonName from GetTop}` - returns user data  
[GET] `/api/GetQueue` - returns current queue  
[POST] `/api/AddToQueue?jsonUsername={player name or user ID}` - urlencoded - adds player to queue and returns current queue  
  
[POST] `/api/CalculateMap` - json `{ Map = "{mapId}", Mods = [{mod abbreviations}] }` - calculates map pp values for 90-100 acc values  
[GET] `/api/GetProbabilityChart={map id}` - gets miss probability data for calculated map  
  
[GET] `/api/GetHighscores` - returns sorted topscores  
[GET] `/api/GetCountries` - get all known countries in the player database  
--  

Requires [osu-tools](https://github.com/stanriders/osu-tools) built in the same folder as workers for pp calculation and WebPerformanceCalculator for individual map calculation.  
  
ASP.NET Core 3.0
