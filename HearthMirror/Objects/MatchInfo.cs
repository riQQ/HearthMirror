namespace HearthMirror.Objects
{
	public class MatchInfo
	{
		public Player LocalPlayer { get; set; }

		public Player OpposingPlayer { get; set; }
		public int BrawlSeasonId { get; set; }
		public int MissionId { get; set; }
		public int RankedSeasonId { get; set; }
		public int GameType { get; set; }
		public int FormatType { get; set; }
		public bool Spectator { get; set; }

		public class Player
		{
			public string Name { get; set; }
			public int Id { get; set; }
			public int StandardRank { get; set; }
			public int WildRank { get; set; }
			public int CardBackId { get; set; }
			public MedalInfo Standard { get; set; }
			public MedalInfo Wild { get; set; }
			public AccountId AccountId { get; set; }
			public BattleTag BattleTag { get; set; }
		}

		public class MedalInfo
		{
			public int? LeagueId { get; set; }
			public int? Stars { get; set; }
			public int? MaxStars { get; set; }
			public int? StarMultipier { get; set; }
			public int? StarLevel { get; set; }
			public int? LegendRank { get; set; }
		}
	}
}