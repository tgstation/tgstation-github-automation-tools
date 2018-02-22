using System;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Represents a line in a <see cref="Changelog"/>
	/// </summary>
    sealed class ChangelogEntry
    {
		/// <summary>
		/// The <see cref="ChangelogEntryType"/> of the <see cref="ChangelogEntry"/>
		/// </summary>
		public ChangelogEntryType Type => type;
		/// <summary>
		/// The body of the <see cref="ChangelogEntry"/>
		/// </summary>
		public string Text => text;
		
		/// <summary>
		/// Backing field for <see cref="Text"/>
		/// </summary>
		readonly string text;
		/// <summary>
		/// Backing field for <see cref="Type"/>
		/// </summary>
		readonly ChangelogEntryType type;

		/// <summary>
		/// Construct a <see cref="ChangelogEntry"/>
		/// </summary>
		/// <param name="text">The value of <see cref="Text"/></param>
		/// <param name="type">The value of <see cref="Type"/></param>
		public ChangelogEntry(string text, ChangelogEntryType type)
		{
			this.text = text ?? throw new ArgumentNullException(nameof(text));
			this.type = type;
		}
    }
}
