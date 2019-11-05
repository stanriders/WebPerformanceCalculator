# WebPerformanceCalculator
https://newpp.stanr.info/

## API
[GET] `/GetTop?offset=0&limit=50&search=chocomint&order=desc&sort=localPP` - returns leaderboard page  
[GET] `/GetResults?jsonUsername={jsonName from GetTop}` - returns user data  
[GET] `/GetQueue` - returns current queue  
[POST] `/AddToQueue?jsonUsername={player name or user ID}` - returns current queue  

--  

Requires [osu-tools](https://github.com/stanriders/osu-tools) built in the same folder as worker.

.NET Core 3.0
