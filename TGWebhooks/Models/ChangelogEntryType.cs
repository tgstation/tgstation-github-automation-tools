namespace TGWebhooks.Models
{
	/// <summary>
	/// Represents the type of a <see cref="ChangelogEntry"/>
	/// </summary>
	enum ChangelogEntryType
	{
		Fix,
		Tweak,
		SoundAdd,
		SoundDel,
		Add,
		Del,
		ImageAdd,
		ImageDel,
		Balance,
		Code,
		Config,
		Admin,
		Server,
		SpellCheck
	}
}