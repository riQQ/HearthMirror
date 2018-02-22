using System.Collections.Generic;

namespace HearthMirror.Objects
{
	public class Collection
	{
		public List<Card> Cards { get; } = new List<Card>();
		public Dictionary<int, Card> FavoriteHeroes { get; } = new Dictionary<int, Card>();
		public List<int> CardBacks { get; } = new List<int>();
		public int FavoriteCardBack { get; internal set; }
		public int Dust { get; internal set; }
		public int Gold { get; internal set; }
	}
}
