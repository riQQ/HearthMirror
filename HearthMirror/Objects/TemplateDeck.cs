using System.Collections.Generic;

namespace HearthMirror.Objects
{
	public class TemplateDeck
	{
		public long DeckId { get; set; }
		public int Class { get; set; }
		public int SortOrder { get; set; }
		public List<Card> Cards { get; set; } = new List<Card>();
		public string Title { get; set; }
	}
}
