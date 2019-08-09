using System.Collections.Generic;

namespace HearthMirror.Util
{
	internal static class DeckCache
	{
		private static Dictionary<int, List<int>> _dungeonDeckCache = new Dictionary<int, List<int>>();
		public static List<int> GetDungeonDeck(int id)
		{
			if(id == 0)
				return null;
			if(!_dungeonDeckCache.ContainsKey(id))
				_dungeonDeckCache[id] = Reflection.GetDungeonDeck(id);
			return _dungeonDeckCache[id];
		}

		private static Dictionary<int, List<int>> _templateDeckCache = new Dictionary<int, List<int>>();
		public static List<int> GetTemplateDeckCards(int id)
		{
			if (id == 0)
				return null;
				if (!_templateDeckCache.ContainsKey(id))
				_templateDeckCache[id] = Reflection.GetDbfDeckCards(id);
			return _templateDeckCache[id];
		}
	}
}
