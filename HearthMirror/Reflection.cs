using System;
using System.Collections.Generic;
using System.Linq;
using HearthMirror.Enums;
using HearthMirror.Objects;
using HearthMirror.Util;

namespace HearthMirror
{
	public class Reflection
	{
		private static readonly Lazy<Mirror> LazyMirror = new Lazy<Mirror>(() => new Mirror {ImageName = "Hearthstone"});
		private static Mirror Mirror => LazyMirror.Value;

		public static event Action<Exception> Exception;

		private static T TryGetInternal<T>(Func<T> action, bool clearCache = true)
		{
			try
			{
				var proc = Mirror.Proc;
				if(proc == null)
					return default(T);
				if(proc.HasExited)
					Mirror.Clean();
				if(clearCache)
					Mirror.View?.ClearCache();
				return action.Invoke();
			}
			catch(Exception e)
			{
				Mirror.Clean();
				try
				{
					return action.Invoke();
				}
				catch(Exception e2)
				{
					Exception?.Invoke(e2);
					return default(T);
				}
			}
		}

		public static void Reinitialize() => Mirror.Clean();

		private static dynamic GetService(string name)
		{
			var dynServices = Mirror.Root?["HearthstoneServices"]["s_dynamicServices"];
			var staticServices = Mirror.Root?["HearthstoneServices"]["s_services"];
			var hsServices = dynServices != null ? dynServices : staticServices;

			var serviceItems = hsServices["m_services"]["_items"];

			foreach(var service in serviceItems)
			{
				if (service?["<ServiceTypeName>k__BackingField"] == name)
					return service["<Service>k__BackingField"];
			}

			return null;
		}

		public static List<Card> GetCollection() => TryGetInternal(() => GetCollectionInternal()?.Cards.ToList());

		public static Collection GetFullCollection() => TryGetInternal(GetCollectionInternal);

		private static Collection GetCollectionInternal()
		{
			var collection = new Collection();

			var collectibleCards = Mirror.Root?["CollectionManager"]["s_instance"]?["m_collectibleCards"];
			if(collectibleCards != null)
			{
				var items = collectibleCards["_items"];
				int size = collectibleCards["_size"];
				for(var i = 0; i < size; i++)
				{
					string cardId = items[i]["m_EntityDef"]["m_cardIdInternal"];
					if(string.IsNullOrEmpty(cardId))
						continue;
					int count = items[i]["<OwnedCount>k__BackingField"];
					int premium = items[i]["m_PremiumType"];
					collection.Cards.Add(new Card(cardId, count, premium > 0));
				}
			}

			var netCacheValues = GetService("NetCache")?["m_netCache"]["valueSlots"];
			if(netCacheValues != null)
			{
				foreach(var val in netCacheValues)
				{
					if(val == null)
						continue;
					if(val.Class.Name == "NetCacheArcaneDustBalance")
						collection.Dust = (int)val["<Balance>k__BackingField"];
					else if(val.Class.Name == "NetCacheGoldBalance")
						collection.Gold = (int)val["<CappedBalance>k__BackingField"] + (int)val["<BonusBalance>k__BackingField"];
					else if(val.Class.Name == "NetCacheCardBacks")
					{
						var cardBacks = val["<CardBacks>k__BackingField"];
						var slots = cardBacks["slots"];
						for(var i = 0; i < slots.Length; i++)
						{
							if(!collection.CardBacks.Contains(slots[i])) 
								collection.CardBacks.Add(slots[i]);
						}
						collection.FavoriteCardBack = (int)val["<FavoriteCardBack>k__BackingField"];
					}
					else if(val.Class.Name == "NetCacheFavoriteHeroes")
					{
						var keys = val["<FavoriteHeroes>k__BackingField"]["keySlots"];
						var values = val["<FavoriteHeroes>k__BackingField"]["valueSlots"];
						for(var i = 0; i < keys.Length; i++)
						{
							if(values[i]?.Class.Name == "CardDefinition")
							{
								var cardId = values[i]["<Name>k__BackingField"];
								var premium = values[i]["<Premium>k__BackingField"];
								collection.FavoriteHeroes[(int)keys[i]["value__"]] = new Card(cardId, 1, premium > 0);
							}
						}
					}
				}
			}

			collection.Dust += Mirror.Root?["CraftingManager"]["s_instance"]?["m_unCommitedArcaneDustAdjustments"] ?? 0;

			return collection;
		}

		public static List<Deck> GetDecks() => TryGetInternal(() => InternalGetDecks().ToList());

		private static IEnumerable<Deck> InternalGetDecks()
		{
			var values = Mirror.Root?["CollectionManager"]["s_instance"]?["m_decks"]?["valueSlots"];
			if(values == null)
				yield break; 
			foreach(var val in values)
			{
				if(val == null || val.Class.Name != "CollectionDeck")
					continue;
				var deck = GetDeck(val);
				if(deck != null)
					yield return deck;
			}
		}

		public static List<TemplateDeck> GetTemplateDecks() => TryGetInternal(() => InternalGetTemplateDecks().ToList());

		private static IEnumerable<TemplateDeck> InternalGetTemplateDecks()
		{
			var templateDecks = GetService("GameDbf")?["DeckTemplate"]?["m_records"];
			if(templateDecks == null)
				yield break; 
			foreach(var template in templateDecks["_items"])
			{
				if(template == null)
					continue;
				TemplateDeck deck = GetTemplateDeck(template);
				if(deck != null && string.IsNullOrEmpty(deck.Event))
					yield return deck;
			}
		}

		public static GameServerInfo GetServerInfo() => TryGetInternal(InternalGetServerInfo);
		private static GameServerInfo InternalGetServerInfo()
		{
			var serverInfo = GetService("Network")?["m_state"]?["<LastGameServerInfo>k__BackingField"];
			if(serverInfo == null)
				return null;
			return new GameServerInfo
			{
				Address = serverInfo["<Address>k__BackingField"],
				AuroraPassword = serverInfo["<AuroraPassword>k__BackingField"],
				ClientHandle = serverInfo["<ClientHandle>k__BackingField"],
				GameHandle = serverInfo["<GameHandle>k__BackingField"],
				Mission = serverInfo["<Mission>k__BackingField"],
				Port = serverInfo["<Port>k__BackingField"],
				Resumable = serverInfo["<Resumable>k__BackingField"],
				SpectatorMode = serverInfo["<SpectatorMode>k__BackingField"],
				SpectatorPassword = serverInfo["<SpectatorPassword>k__BackingField"],
				Version = serverInfo["<Version>k__BackingField"],
			};
		}

		public static int GetGameType() => TryGetInternal(InternalGetGameType);
		private static int InternalGetGameType() => (int) GetService("GameMgr")?["m_gameType"];

		public static bool IsSpectating() => TryGetInternal(() => GetService("GameMgr")?["m_spectator"]) ?? false;

		public static long GetSelectedDeckInMenu() => TryGetInternal(() => (long)(Mirror.Root?["DeckPickerTrayDisplay"]["s_instance"]?["m_selectedCustomDeckBox"]?["m_deckID"] ?? 0));

		public static MedalInfo GetMedalInfo() => TryGetInternal(GetMedalInfoInternal);
		private static MedalInfo GetMedalInfoInternal()
		{
			var netCacheValues = GetService("NetCache")?["m_netCache"]?["valueSlots"];
			if(netCacheValues == null)
				return null;
			dynamic netCacheMedalInfo = null;
			foreach(var netCache in netCacheValues)
			{
				if(netCache?.Class.Name != "NetCacheMedalInfo")
					continue;
				netCacheMedalInfo = netCache;
				break;
			}
			if(netCacheMedalInfo == null)
				return null;
			var standard = netCacheMedalInfo["<Standard>k__BackingField"];
			var wild = netCacheMedalInfo["<Wild>k__BackingField"];
			MedalInfoData GetMedalInfoData(dynamic source)
			{
				if(source == null)
					return null;
				return new MedalInfoData
				{
					BestStarLevel = source["_BestStarLevel"],
					CanLoseLevel = source["<CanLoseLevel>k__BackingField"],
					CanLoseStars = source["_CanLoseStars"],
					LegendRank = source["_LegendRank"],
					LevelEnd = source["<LevelEnd>k__BackingField"],
					LevelStart = source["<LevelStart>k__BackingField"],
					SeasonWins = source["<SeasonWins>k__BackingField"],
					StarLevel = source["<StarLevel>k__BackingField"],
					Stars = source["<Stars>k__BackingField"],
					Streak = source["<Streak>k__BackingField"]
				};
			}
			return new MedalInfo
			{
				Standard = GetMedalInfoData(standard),
				Wild = GetMedalInfoData(wild),
			};
		}

		private static MatchInfo.MedalInfo GetMatchMedalInfo(dynamic medalInfo)
		{
			var starLevel = medalInfo?["starLevel"];
			var leagueId = medalInfo?["leagueId"];
			var rankConfig = starLevel != null && leagueId != null ? GetLeagueRankRecord(leagueId, starLevel) : null;
			return new MatchInfo.MedalInfo
			{
				Stars = medalInfo?["earnedStars"],
				MaxStars = rankConfig?["m_stars"],
				StarMultipier = medalInfo?["starsPerWin"],
				StarLevel = starLevel,
				LegendRank = medalInfo?["legendIndex"],
				LeagueId = leagueId,
			};
		}

		private static dynamic GetLeagueRankRecord(int leagueId, int starLevel)
		{
			var rankMgr = Mirror.Root?["RankMgr"]["s_instance"];
			if(rankMgr == null)
				return null;
			var leagueMap = rankMgr["m_rankConfigByLeagueAndStarLevel"];
			if(leagueMap == null)
				return null;
			var leagueKeys = leagueMap["keySlots"];
			var leagueValues = leagueMap["valueSlots"];
			for(var i = 0; i < leagueKeys.Length; i++)
			{
				if(leagueKeys[i] != leagueId)
					continue;
				var starLevelMap = leagueValues[i];
				if(starLevelMap == null)
					return null;
				var starLevelKeys = starLevelMap["keySlots"];
				var starLevelValues = starLevelMap["valueSlots"];
				for(var j = 0; j < starLevelKeys.Length; j++)
				{
					if(starLevelKeys[j] != starLevel)
						continue;
					return starLevelValues[j];
				}
			}
			return null;
		}

		private static int GetRankValue(dynamic medalInfo)
		{
			if (medalInfo == null)
				return 0;
			var leagueId = medalInfo["leagueId"];
			var starLevel = medalInfo["starLevel"];
			var leagueRankRecord = GetLeagueRankRecord(leagueId, starLevel);
			if(leagueRankRecord == null)
				return 0;
			var locValues = leagueRankRecord["m_medalText"]["m_locValues"]["_items"];
			foreach(var value in locValues)
			{
				if(value == null)
					continue;
				if(int.TryParse(value, out int rank))
					return rank;
			}
			return 0;
		}

		public static MatchInfo GetMatchInfo() => TryGetInternal(GetMatchInfoInternal);
		private static MatchInfo GetMatchInfoInternal()
		{
			var matchInfo = new MatchInfo();
			var gameState = Mirror.Root?["GameState"]["s_instance"];
			var netCacheValues = GetService("NetCache")?["m_netCache"]?["valueSlots"];
			if(gameState != null)
			{
				var playerIds = gameState["m_playerMap"]["keySlots"];
				var players = gameState["m_playerMap"]["valueSlots"];
				for(var i = 0; i < playerIds.Length; i++)
				{
					var player = players[i];
					if(player?.Class.Name != "Player")
						continue;

					var medalInfo = player["m_medalInfo"];
					var sMedalInfo = medalInfo?["m_currMedalInfo"];
					var wMedalInfo = medalInfo?["m_currWildMedalInfo"];

					var account = player["m_gameAccountId"];
					var accountId = new AccountId {Hi = account?["m_hi"] ?? 0, Lo = account?["m_lo"] ?? 0};

					var matchInfoPlayer = new MatchInfo.Player
					{
						Id = playerIds[i],
						Name = player["m_name"],
						Standard = GetMatchMedalInfo(sMedalInfo),
						StandardRank = GetRankValue(sMedalInfo),
						Wild = GetMatchMedalInfo(wMedalInfo),
						WildRank = GetRankValue(wMedalInfo),
						CardBackId = player["m_cardBackId"],
						AccountId = accountId,
						BattleTag = GetBattleTag(accountId),
					};


					var side = (Side)player["m_side"];
					if (side == Side.FRIENDLY)
						matchInfo.LocalPlayer = matchInfoPlayer;
					else if (side == Side.OPPOSING)
						matchInfo.OpposingPlayer = matchInfoPlayer;
				}
			}
			if(matchInfo.LocalPlayer == null || matchInfo.OpposingPlayer == null)
				return null;
			var gameMgr = GetService("GameMgr");
			if(gameMgr != null)
			{
				matchInfo.MissionId = gameMgr["m_missionId"];
				matchInfo.GameType = gameMgr["m_gameType"];
				matchInfo.FormatType = gameMgr["m_formatType"];

				var brawlGameTypes = new[] {16, 17, 18};
				if(brawlGameTypes.Contains(matchInfo.GameType))
				{
					var mission = GetCurrentBrawlMission();
					matchInfo.BrawlSeasonId = mission?["<tavernBrawlSpec>k__BackingField"]?["<GameContentSeason>k__BackingField"]?["<SeasonId>k__BackingField"];
				}
			}
			if(netCacheValues != null)
			{
				foreach(var netCache in netCacheValues)
				{
					if(netCache?.Class.Name != "NetCacheRewardProgress")
						continue;
					matchInfo.RankedSeasonId = netCache["<Season>k__BackingField"];
					break;
				}
			}
			return matchInfo;
		}

		private static BattleTag GetBattleTag(AccountId accountId)
		{
			var gameAccounts = Mirror.Root?["BnetPresenceMgr"]["s_instance"]?["m_gameAccounts"];
			if(gameAccounts == null)
				return null;
			var keys = gameAccounts["keySlots"];
			for(var i = 0; i < keys.Length; i++)
			{
				if(keys[i]?["m_hi"] != accountId.Hi || keys[i]?["m_lo"] != accountId.Lo)
					continue;
				var bTag = gameAccounts["valueSlots"][i]["m_battleTag"];
				return new BattleTag
				{
					Name = bTag["m_name"],
					Number = bTag["m_number"]
				};
			}
			return null;
		}

		private static dynamic GetCurrentBrawlMission()
		{
			var missions = GetService("TavernBrawlManager")?["m_missions"];
			if(missions == null) 
				return null;
			foreach(var mission in missions)
			{
				if(mission?.Class.Name != "TavernBrawlMission") 
					continue;
				return mission;
			}
			return null;
		}

		public static ArenaInfo GetArenaDeck() => TryGetInternal(GetArenaDeckInternal);

		private static ArenaInfo GetArenaDeckInternal()
		{
			var draftManager = GetService("DraftManager");
			if(draftManager == null)
				return null;
			var deck = GetDeck(draftManager["m_draftDeck"]);
			if(deck == null)
				return null;

			var season = draftManager["m_currentSeason"]?["_Season"]?["<GameContentSeason>k__BackingField"]?["<SeasonId>k__BackingField"];

			return new ArenaInfo {
				Wins = draftManager["m_wins"],
				Losses = draftManager["m_losses"],
				CurrentSlot = draftManager["m_currentSlot"],
				Deck = deck,
				Rewards = RewardDataParser.Parse(draftManager["m_chest"]?["<Rewards>k__BackingField"]?["_items"]),
				Season = season
			};
		}

		public static List<Card> GetArenaDraftChoices() => TryGetInternal(() => GetArenaDraftChoicesInternal().ToList());

		private static IEnumerable<Card> GetArenaDraftChoicesInternal()
		{
			var choicesList =  Mirror.Root?["DraftDisplay"]["s_instance"]["m_choices"];
			var choices = choicesList["_items"];
			int size = choicesList["_size"];
			for(var i = 0; i < size; i++)
			{
				if(choices[i] != null)
					yield return new Card(choices[i]["m_actor"]["m_entityDef"]["m_cardIdInternal"], 1, false);
			}
		}

		private static TemplateDeck GetTemplateDeck(dynamic deckObj)
		{
			if(deckObj == null)
				return null;
			var deckId = deckObj["m_deckId"];
			DeckDbfRecord dbfDeck = GetDbfDeckTopCard(deckId);
			if (dbfDeck == null)
				return null;
			return new TemplateDeck(dbfDeck.TopCardId)
			{
				DeckId = deckId,
				Class = deckObj["m_classId"],
				SortOrder = deckObj["m_sortOrder"],
				Title = dbfDeck.Name,
				Event = deckObj["m_event"],
			};
		}

		private static Deck GetDeck(dynamic deckObj)
		{
			if(deckObj == null)
				return null;
			var deck = new Deck
			{
				Id = deckObj["ID"],
				Name = deckObj["m_name"],
				Hero = deckObj["HeroCardID"],
				IsWild = deckObj["m_isWild"],
				Type = deckObj["Type"],
				SeasonId = deckObj["SeasonId"],
				CardBackId = deckObj["CardBackID"],
				HeroPremium = deckObj["HeroPremium"],
				SourceType = deckObj["SourceType"],
				CreateDate = deckObj["CreateDate"]
			};
			var cardList = deckObj["m_slots"];
			var cards = cardList["_items"];
			int size = cardList["_size"];
			for(var i = 0; i < size; i++)
			{
				var card = cards[i];
				string cardId = card["m_cardId"];

				var count = 0;
				var counts = card["m_count"];
				for(var j = 0; j < counts["_size"]; j++)
					count += (int)counts["_items"][j];

				var existingCard = deck.Cards.FirstOrDefault(x => x.Id == cardId);
				if(existingCard != null)
					existingCard.Count++;
				else
					deck.Cards.Add(new Card(cardId, count, false));
			}
			return deck;
		}

		public static int GetFormat() => TryGetInternal(() => (int)GetService("GameMgr")?["m_formatType"]);

		public static Deck GetEditedDeck() => TryGetInternal(GetEditedDeckInternal);
		private static Deck GetEditedDeckInternal()
		{
			var taggedDecks = Mirror.Root?["CollectionManager"]["s_instance"]["m_taggedDecks"];
			var tags = taggedDecks["keySlots"];
			var decks = taggedDecks["valueSlots"];
			for (var i = 0; i < tags.Length; i++)
			{
				if(tags[i] == null || decks[i] == null)
					continue;
				if(tags[i]["value__"] == 0)
					return GetDeck(decks[i]);
			}
			return null;
		}

		public static bool IsFriendlyChallengeDialogVisible() =>
			TryGetInternal(IsFriendlyChallengeDialogVisibleInternal);

		private static bool IsFriendlyChallengeDialogVisibleInternal()
		{
			var dialog = Mirror.Root?["DialogManager"]?["s_instance"]?["m_currentDialog"];
			if(dialog == null)
				return false;
			return dialog.Class.Name == "FriendlyChallengeDialog" && dialog["m_shown"];
		}

		public static bool IsFriendsListVisible() => TryGetInternal(() => Mirror.Root?["ChatMgr"]?["s_instance"]?["m_friendListFrame"]) != null;

		public static bool IsGameMenuVisible() => TryGetInternal(() => Mirror.Root?["GameMenu"]?["s_instance"]?["m_isShown"]) ?? false;

		public static bool IsOptionsMenuVisible() => TryGetInternal(() => Mirror.Root?["OptionsMenu"]?["s_instance"]?["m_isShown"]) ?? false;

		public static bool IsMulligan() => TryGetInternal(() => Mirror.Root?["MulliganManager"]?["s_instance"]?["mulliganChooseBanner"]) != null;

		public static int GetNumMulliganCards() => TryGetInternal(() => Mirror.Root?["MulliganManager"]?["s_instance"]?["m_startingCards"]?["_size"]) ?? 0;

		public static bool IsChoosingCard() => (TryGetInternal(() => Mirror.Root?["ChoiceCardMgr"]?["s_instance"]?["m_subOptionState"]) != null)
				|| ((int)(TryGetInternal(() => Mirror.Root?["ChoiceCardMgr"]?["s_instance"]?["m_choiceStateMap"]?["count"]) ?? 0) > 0);

		public static int GetNumChoiceCards() => TryGetInternal(() => Mirror.Root?["ChoiceCardMgr"]?["s_instance"]?["m_lastShownChoices"]?["_size"]) ?? 0;

		public static bool IsPlayerEmotesVisible() => TryGetInternal(() => Mirror.Root?["EmoteHandler"]?["s_instance"]?["m_emotesShown"]) ?? false;

		public static bool IsEnemyEmotesVisible() => TryGetInternal(() => Mirror.Root?["EnemyEmoteHandler"]?["s_instance"]?["m_emotesShown"]) ?? false;

		public static bool IsInBattlecryEffect() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_isInBattleCryEffect"]) ?? false;

		public static bool IsDragging() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_dragging"]) ?? false;

		public static bool IsTargetingHeroPower() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_targettingHeroPower"]) ?? false;

		public static int GetBattlecrySourceCardZonePosition() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_battlecrySourceCard"]?["m_zonePosition"]) ?? 0;

		public static bool IsHoldingCard() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_heldCard"]) != null;

		public static bool IsTargetReticleActive() => TryGetInternal(() => Mirror.Root?["TargetReticleManager"]?["s_instance"]?["m_isActive"]) ?? false;

		public static bool IsEnemyTargeting() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_isEnemyArrow"]) ?? false;

		public static bool IsGameOver() => TryGetInternal(() => Mirror.Root?["GameState"]?["s_instance"]?["m_gameOver"]) ?? false;

		public static bool WasRestartRequested() => TryGetInternal(() => Mirror.Root?["GameState"]?["s_instance"]?["m_restartRequested"]) ?? false;

		public static bool IsInMainMenu() => (int)(TryGetInternal(() => Mirror.Root?["Box"]?["s_instance"]?["m_state"]) ?? -1) == (int)BoxState.HUB_WITH_DRAWER;

		public static UI_WINDOW GetShownUiWindowId() => (UI_WINDOW)(TryGetInternal(() => Mirror.Root?["ShownUIMgr"]?["s_instance"]?["m_shownUI"]) ?? UI_WINDOW.NONE);

		public static bool IsPlayerHandZoneUpdatingLayout() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_myHandZone"]?["m_updatingLayout"]) ?? false;

		public static bool IsPlayerPlayZoneUpdatingLayout() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_myPlayZone"]?["m_updatingLayout"]) ?? false;

		public static SceneMode GetCurrentSceneMode() => (SceneMode)(TryGetInternal(() => GetService("SceneMgr")?["m_mode"]) ?? SceneMode.INVALID);

		public static int GetNumCardsPlayerHand() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_myHandZone"]?["m_cards"]?["_size"]) ?? 0;

		public static int GetNumCardsPlayerBoard() => TryGetInternal(() => Mirror.Root?["InputManager"]?["s_instance"]?["m_myPlayZone"]?["m_cards"]?["_size"]) ?? 0;

		public static int GetNavigationHistorySize() => TryGetInternal(() => Mirror.Root?["Navigation"]?["history"]?["_size"]) ?? 0;

		public static int GetCurrentManaFilter() => TryGetInternal(() => (int)Mirror.Root?["CollectionManagerDisplay"]["s_instance"]["m_manaTabManager"]["m_currentFilterValue"]);

		public static SetFilterItem GetCurrentSetFilter() => TryGetInternal(GetCurrentSetFilterInternal);

		private static SetFilterItem GetCurrentSetFilterInternal()
		{
			var item = Mirror.Root?["CollectionManagerDisplay"]["s_instance"]["m_setFilterTray"]["m_selected"];
			return new SetFilterItem()
			{
				IsAllStandard =  (bool)item["m_isAllStandard"],
				IsWild = (bool)item["m_isWild"]
			};
		}

		public static BattleTag GetBattleTag() => TryGetInternal(GetBattleTagInternal);

		private static BattleTag GetBattleTagInternal()
		{
			var bTag = Mirror.Root?["BnetPresenceMgr"]["s_instance"]?["m_myPlayer"]?["m_account"]?["m_battleTag"];
			if(bTag == null)
				return null;
			return new BattleTag
			{
				Name = bTag["m_name"],
				Number = bTag["m_number"]
			};
		}

		public static List<Card> GetPackCards() => TryGetInternal(() => GetPackCardsInternal().ToList());

		private static IEnumerable<Card> GetPackCardsInternal()
		{
			var cards = Mirror.Root?["PackOpening"]["s_instance"]["m_director"]?["m_hiddenCards"]?["_items"];
			if(cards == null)
				yield break;
			foreach(var card in cards)
			{
				if(card?.Class.Name != "PackOpeningCard")
					continue;
				var def = card["m_boosterCard"]?["<Def>k__BackingField"];
				if(def == null)
					continue;
				yield return new Card((string)def["<Name>k__BackingField"], 1, (int)def["<Premium>k__BackingField"] > 0);
			}
		}

		public static List<RewardData> GetArenaRewards() => TryGetInternal(() => GetArenaRewardsInternal().ToList());

		private static IEnumerable<RewardData> GetArenaRewardsInternal()
		{
			var rewards = GetService("DraftManager")?["m_chest"]?["<Rewards>k__BackingField"]?["_items"];
			return RewardDataParser.Parse(rewards);
		}

		public static SeasonEndInfo GetSeasonEndInfo() => TryGetInternal(GetSeasonEndInfoInternal);

		private static SeasonEndInfo GetSeasonEndInfoInternal()
		{
			var dialog = Mirror.Root?["DialogManager"]["s_instance"]["m_currentDialog"];
			if(dialog?.Class.Name != "SeasonEndDialog" || !dialog["m_shown"])
				return null;
			var info = dialog["m_seasonEndInfo"];
			var rewards = RewardDataParser.Parse(info["m_rankedRewards"]["_items"]);
			return new SeasonEndInfo(
				(int)info["m_bonusStars"],
				(int)info["m_boostedRank"],
				(int)info["m_chestRank"],
				(bool)info["m_isWild"],
				(int)info["m_legendIndex"],
				(int)info["m_rank"],
				(int)info["m_seasonID"],
				rewards);
		}

		public static int GetLastOpenedBoosterId() => (int)(TryGetInternal(() => Mirror.Root?["PackOpening"]?["s_instance"]?["m_lastOpenedBoosterId"]) ?? 0);

		public static AccountId GetAccountId() => TryGetInternal(GetAccountIdInternal);

		private static AccountId GetAccountIdInternal()
		{
			var accId = Mirror.Root?["BnetPresenceMgr"]?["s_instance"]?["m_myGameAccountId"];
			return accId == null ? null : new AccountId {Hi = accId["m_hi"], Lo = accId["m_lo"]};
		}

		public static BrawlInfo GetBrawlInfo() => TryGetInternal(GetBrawlInfoInternal);

		private static BrawlInfo GetBrawlInfoInternal()
		{
			var mission = GetCurrentBrawlMission();
			if(mission == null)
				return null;

			var brawlInfo = new BrawlInfo
			{
				MaxWins = mission["tavernBrawlSpec"]?["<GameContentSeason>k__BackingField"]?["_MaxWins"],
				MaxLosses = mission["tavernBrawlSpec"]?["<GameContentSeason>k__BackingField"]?["_MaxLosses"]
			};

			var records = GetService("TavernBrawlManager")?["m_playerRecords"];
			if(records == null)
				return null;

			dynamic record = null;
			foreach(var r in records)
			{
				if(r?.Class.Name != "TavernBrawlPlayerRecord")
					continue;
				record = r;
			}
			if(record == null)
				return null;

			brawlInfo.GamesPlayed = record["_GamesPlayed"];
			brawlInfo.WinStreak = record["_WinStreak"];
			if(brawlInfo.IsSessionBased)
			{
				if(!(bool)record["HasSession"])
					return brawlInfo;
				var session = record["_Session"];
				brawlInfo.Wins = session["<Wins>k__BackingField"];
				brawlInfo.Losses = session["<Losses>k__BackingField"];
			}
			else
			{
				brawlInfo.Wins = record["<GamesWon>k__BackingField"];
				brawlInfo.Losses = brawlInfo.GamesPlayed - brawlInfo.Wins;
			}
			return brawlInfo;
		}

		public static DungeonInfo[] GetDungeonInfo() => TryGetInternal(GetDungeonInfoInternal);

		private static DungeonInfo[] GetDungeonInfoInternal()
		{
			var dataMap = Mirror.Root?["GameSaveDataManager"]?["s_instance"]?["m_gameSaveDataMapByKey"];
			if(dataMap == null)
				return null;
			var lootIndex = GetKeyIndex(dataMap, (int)GameSaveKeyId.ADVENTURE_DATA_LOOT);
			var gilIndex = GetKeyIndex(dataMap, (int)GameSaveKeyId.ADVENTURE_DATA_GIL);
			var trlIndex = GetKeyIndex(dataMap, (int)GameSaveKeyId.ADVENTURE_DATA_TRL);
			var dalaranIndex = GetKeyIndex(dataMap, (int)GameSaveKeyId.ADVENTURE_DATA_DALARAN);
			var dalaranHeroicIndex = GetKeyIndex(dataMap, (int)GameSaveKeyId.ADVENTURE_DATA_DALARAN_HEROIC);
			var uldumIndex = GetKeyIndex(dataMap, (int)GameSaveKeyId.ADVENTURE_DATA_SERVER_ULDUM);
			var uldumHeroicIndex = GetKeyIndex(dataMap, (int)GameSaveKeyId.ADVENTURE_DATA_SERVER_ULDUM_HEROIC);
			var data = dataMap["valueSlots"];
			return new DungeonInfo[]
			{
				lootIndex == -1 ? null : new DungeonInfoParser(1004, data[lootIndex]),
				gilIndex == -1 ? null : new DungeonInfoParser(1125, data[gilIndex]),
				trlIndex == -1 ? null : new DungeonInfoParser(1129, data[trlIndex]),
				dalaranIndex == -1 ? null : new DungeonInfoParser(1130, data[dalaranIndex]),
				dalaranHeroicIndex == -1 ? null : new DungeonInfoParser(1130, data[dalaranHeroicIndex]),
				uldumIndex == -1 ? null : new DungeonInfoParser(1158, data[uldumIndex]),
				uldumHeroicIndex == -1 ? null : new DungeonInfoParser(1158, data[uldumHeroicIndex])
			};
		}

		public static BattlegroundRatingInfo GetBattlegroundRatingInfo() => TryGetInternal(GetBattlegroundRatingInfoInternal);

		private static BattlegroundRatingInfo GetBattlegroundRatingInfoInternal()
		{
			var netCacheValues = GetService("NetCache")?["m_netCache"]?["valueSlots"];
			if (netCacheValues == null)
				return null;

			BattlegroundRatingInfo ratingInfo = null;

			foreach(var netCache in netCacheValues)
			{
				if(netCache?.Class.Name != "NetCacheBaconRatingInfo")
					continue;
				ratingInfo = new BattlegroundRatingInfo() 
				{
					Rating = netCache["<Rating>k__BackingField"],
					PrevRating = netCache["<PreviousBaconRatingInfo>k__BackingField"]?["<Rating>k__BackingField"]
				};
				break;
			}

			return ratingInfo;
		}

		internal static int GetKeyIndex(dynamic map, int key)
		{
			var keys = map["keySlots"];
			for(var i = 0; i < keys.Length; i++)
			{
				if(keys[i]["value__"] == key)
					return i;
			}
			return -1;
		}

		public static List<int> GetDungeonDeck(int deckDbfRecord) => TryGetInternal(() => GetDungeonDeckInternal(deckDbfRecord));

		private static List<int> GetDungeonDeckInternal(int deckDbfRecord)
		{
			var deck = GetDbfDeckTopCard(deckDbfRecord);
			if(deck == null)
				return null;
			return GetDbfDeckCards(deck.TopCardId);
		}

		public static List<int> GetDbfDeckCards(int topCardId)
		{
			var cards = new List<int>();
			dynamic cardDbfRecord = GetDeckCardDbfRecord(topCardId);
			while (cardDbfRecord != null)
			{
				cards.Add(cardDbfRecord["m_cardId"]);
				var next = cardDbfRecord["m_nextCardId"];
				cardDbfRecord = next == 0 ? null : GetDeckCardDbfRecord(next);
			}
			return cards;

		}

		private static DeckDbfRecord GetDbfDeckTopCard(int deckDbfRecord)
		{
			var decks = GetService("GameDbf")?["Deck"]?["m_records"];
			if(decks == null)
				return null;
			var items = decks["_items"];
			for(var i = 0; i < items.Length; i++)
			{
				var id = items[i]["m_ID"];
				if(id == deckDbfRecord)
					return new DeckDbfRecord()
					{
						TopCardId = items[i]["m_topCardId"],
						Name = items[i]["m_name"]["m_locValues"]["_items"][1],
					};
			}
			return null;
		}

		private static dynamic GetDeckCardDbfRecord(int cardId)
		{
			var cards = GetService("GameDbf")?["DeckCard"]?["m_records"];
			if(cards == null)
				return null;
			var items = cards["_items"];
			for(var i = 0; i < items.Length; i++)
			{
				var id = items[i]["m_ID"];
				if(id == cardId)
					return items[i];
			}
			return null;
		}

		public static string GetCardIdFromCardDbId(int dbId) => TryGetInternal(() => GetCardIdFromCardDbIdInternal(dbId));

		private static string GetCardIdFromCardDbIdInternal(int dbId)
		{
			var cards = GetService("GameDbf")?["Card"]?["m_records"];
			if (cards == null)
				return null;
			var items = cards["_items"];
			for (var i = 0; i < items.Length; i++)
			{
				var id = items[i]["m_ID"];
				if (id == dbId)
					return items[i]["m_noteMiniGuid"];
			}
			return null;
		}

		public static bool IsLogEnabled(string name) => TryGetInternal(() => IsLogEnabledInternal(name));

		private static bool IsLogEnabledInternal(string name)
		{
			var logs = Mirror.Root?["Log"]?["s_instance"]?["m_logInfos"]?["valueSlots"];
			if(logs == null)
				return false;
			for(var i = 0; i < logs.Length; i++)
			{
				if(logs[i]?["m_name"] == name)
					return true;
			}
			return false;
		}

#if(DEBUG)
		public static void DebugHelper()
		{
			var matchInfo = GetMatchInfo();
			var netCache = GetService("NetCache");
			var draftManager = GetService("draftManager");
			var gameMgr = GetService("GameMgr");
			var network = GetService("Network");
			var gameDbf = GetService("GameDbf");
			var data = new[]
			{
				"GameState", "Log", "TavernBrawlManager", "TavernBrawlDisplay", "BnetPresenceMgr",
				"PackOpening", "CollectionManagerDisplay", "DraftManager", "DraftDisplay", "CollectionManager",
				"RankMgr", "GameSaveDataManager"
			}.Select(x => Mirror.Root?[x]?["s_instance"]).ToList();
			System.Diagnostics.Debugger.Break();
		}
#endif
	}
}
