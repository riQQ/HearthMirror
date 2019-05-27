using System.Collections.Generic;

namespace HearthMirror.Util
{
	internal static class DeckCache
	{
		private static Dictionary<int, List<int>> _cache = new Dictionary<int, List<int>>();
		public static List<int> Get(int id)
		{
			if(id == 0)
				return null;
			if(!_cache.ContainsKey(id))
				_cache[id] = Reflection.GetDungeonDeck(id);
			return _cache[id];
		}
	}
}
