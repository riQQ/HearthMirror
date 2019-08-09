using HearthMirror.Util;
using System.Collections.Generic;

namespace HearthMirror.Objects
{
	public class TemplateDeck
	{
		private int _deckId;

		public TemplateDeck(int deckId)
		{
			_deckId = deckId;
		}

		public int DeckId { get; set; }
		public int Class { get; set; }
		public int SortOrder { get; set; }
		public string Title { get; set; }
		public string Event { get; set; }
		public List<int> Cards => DeckCache.GetTemplateDeckCards(_deckId);
	}
}
