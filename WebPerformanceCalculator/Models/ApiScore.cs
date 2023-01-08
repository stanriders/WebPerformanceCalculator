using System;
using Newtonsoft.Json;

namespace WebPerformanceCalculator.Models;

public class ApiScore
{
    [JsonProperty("id")]
	public ulong? Id { get; set; }

    [JsonProperty("user_id")]
	public uint UserId { get; set; }

	[JsonProperty("beatmap")]
	public BeatmapShort BeatmapShort { get; set; }

	[JsonProperty("beatmapset")]
	public BeatmapSetShort BeatmapSet { get; set; }

	[JsonProperty("rank")]
	//[JsonConverter(typeof(StringEnumConverter))]
	public string? Grade { get; set; }

	[JsonProperty("pp")]
	public double? Pp { get; set; }

	private double accuracy = 0.0;
	[JsonProperty("accuracy")]
	public double Accuracy
	{
		get
		{
			if (accuracy <= 0.0)
			{
				/*
				 * Accuracy = Total points of hits / (Total number of hits * 300)
				 * Total points of hits  =  Number of 50s * 50 + Number of 100s * 100 + Number of 300s * 300
				 * Total number of hits  =  Number of misses + Number of 50's + Number of 100's + Number of 300's
				 */

				double totalPoints = Statistics.Count50 * 50 + Statistics.Count100 * 100 + Statistics.Count300 * 300;
				double totalHits = Statistics.CountMiss + Statistics.Count50 + Statistics.Count100 + Statistics.Count300;

				accuracy = totalPoints / (totalHits * 300) * 100;
			}

			return accuracy;
		}
		set => accuracy = value * 100.0;
	}

	[JsonProperty("max_combo")]
	public uint Combo { get; set; }

	[JsonProperty("mods")]
	public string[] Mods { get; set; }

	[JsonProperty("created_at")]
	public DateTime? Date { get; set; }

    [JsonProperty("statistics")]
	public ScoreStatistics Statistics { get; set; }

	public class ScoreStatistics
	{
		[JsonProperty("count_50")]
		public uint Count50 { get; set; }

		[JsonProperty("count_100")]
		public uint Count100 { get; set; }

		[JsonProperty("count_300")]
		public uint Count300 { get; set; }

		[JsonProperty("count_geki")]
		public uint CountGeki { get; set; }

		[JsonProperty("count_katu")]
		public uint CountKatu { get; set; }

		[JsonProperty("count_miss")]
		public uint CountMiss { get; set; }
	}
}

public class BeatmapShort
{
	[JsonProperty("id")]
	public int Id { get; set; }

	[JsonProperty("beatmapset_id")]
	public int BeatmapSetId { get; set; }

	[JsonProperty("version")]
	public string Version { get; set; }

    [JsonProperty("mode")]
	public string ModeName { get; set; }

	[JsonProperty("url")]
	public string Url { get; set; }

    [JsonProperty("max_combo")]
	public uint? MaxCombo { get; set; }

    [JsonProperty("user_id")]
    public uint CreatorId { get; set; }
}

public class BeatmapSetShort
{
    [JsonProperty("id")]
    public uint Id { get; set; }

    [JsonProperty("artist")]
    public string Artist { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("creator")]
    public string CreatorName { get; set; }

    [JsonProperty("user_id")]
    public uint CreatorId { get; set; }
}