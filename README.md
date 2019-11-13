# WebPerformanceCalculator
https://newpp.stanr.info/

## API (json)
[GET] `/GetTop?offset=0&limit=50&search=chocomint&order=desc&sort=localPP` - returns leaderboard page  
[GET] `/GetResults?jsonUsername={jsonName from GetTop}` - returns user data  
[GET] `/GetQueue` - returns current queue  
[POST] `/AddToQueue?jsonUsername={player name or user ID}` - returns current queue  
  
[GET] `/CalculateMap?map={map link or id}&mods=HD&mods=HR&mods=...` - calculates map pp values for 90-100 acc values  
[GET] `/GetProbabilityChart={map id}` - gets miss probability data for calculated map  
  
[GET] `/GetHighscores` - returns sorted topscores  

--  

Requires [osu-tools](https://github.com/stanriders/osu-tools) built in the same folder as worker AND WebPerformanceCalculator.

.NET Core 3.0
