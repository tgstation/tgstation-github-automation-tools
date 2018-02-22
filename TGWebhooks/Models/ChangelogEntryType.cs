namespace TGWebhooks.Models
{
	/// <summary>
	/// Represents the type of a <see cref="ChangelogEntry"/>. Must match ss13_genchangelog.py
	/// </summary>
	enum ChangelogEntryType
	{
		BugFix,
		Tweak,
		SoundAdd,
		SoundDel,
		RscAdd,
		RscDel,
		ImageAdd,
		ImageDel,
		Balance,
		Code_Imp,
		Config,
		Admin,
		Server,
		SpellCheck,
		Refactor,
	}
}