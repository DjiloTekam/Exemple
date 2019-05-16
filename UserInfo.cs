using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


/// <summary>
/// User state information.
/// </summary>
public class UserInfo
{
	public WillkommenInfo Willkommen { get; set; }

	public BeschwerdeInfo Beschwerde { get; set; }

	public TerminInfo Termin { get; set; }

	public FrageInfo Frage { get; set; }

}

/// <summary>
/// State information associated with the termin dialog.
/// </summary>
public class WillkommenInfo
{
	public string Name { get; set; }
}
/// <summary>
/// State information associated with the termin dialog.
/// </summary>

public class TerminInfo
{
	public string Ansprechpartner { get; set; }

	public string Location { get; set; }

	public string Date { get; set; }
}


/// <summary>
/// State information associated with the beschwerde dialog.
/// </summary>

public class BeschwerdeInfo
{
	public string Email { get; set; }

	public string Anklage { get; set; }

	


}

/// <summary>
/// State information associated with the frage call dialog.
/// </summary>
public class FrageInfo
{
	public string Question { get; set; }

}

