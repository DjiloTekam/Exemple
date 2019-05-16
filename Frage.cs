using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Exemple
{
	public class Frage
	{
		public AntwortInfo Antwort { get; set; }

		public List<string> Stichwort { get; set; }

	}
	public class AntwortInfo
	{
		public string Text { get; set; }
		public string Bild { get; set; }
	}
}
