using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Represents a changelog in the body of a <see cref="PullRequest"/>
	/// </summary>
    sealed class Changelog
    {
		/// <summary>
		/// The titled author(s) of the <see cref="Changelog"/>
		/// </summary>
		public string Author => author;
		/// <summary>
		/// The <see cref="ChangelogEntry"/>s in the <see cref="Changelog"/>
		/// </summary>
		public IReadOnlyList<ChangelogEntry> Changes => changes;

		/// <summary>
		/// Backing field for <see cref="Author"/>
		/// </summary>
		readonly string author;
		/// <summary>
		/// Backing field for <see cref="Changes"/>
		/// </summary>
		readonly List<ChangelogEntry> changes;

		public static Changelog GetChangelog(PullRequest pullRequest, out bool malformed)
		{
			string author = null;
			List<ChangelogEntry> entries = null;
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (pullRequest.Body == null)
			{
				malformed = false;
				return null;
			}
			foreach (var line in pullRequest.Body.Split('\n') ?? throw new ArgumentNullException(nameof(pullRequest)))
			{
				if (String.IsNullOrWhiteSpace(line))
					continue;
				const StringComparison ComparisonCulture = StringComparison.InvariantCulture;	//could potentially ignore case here
				var foundStartTag = line.StartsWith(":cl:", ComparisonCulture) || line.StartsWith("🆑", ComparisonCulture);
				var foundEndTag = line.StartsWith("/:cl:", ComparisonCulture) || line.StartsWith("/🆑", ComparisonCulture);
				if (author == null)
				{
					if (foundEndTag)
					{
						malformed = true;
						return null;
					}
					if (foundStartTag)
					{
						author = line.Replace(":cl:", String.Empty, ComparisonCulture).Replace("🆑", String.Empty, ComparisonCulture).Trim();
						if (String.IsNullOrWhiteSpace(author))
							author = pullRequest.User.Login;
						else if (author == "optional name here")
						{
							malformed = true;
							return null;
						}
						entries = new List<ChangelogEntry>();
					}
					continue;
				}
				if (foundStartTag)
				{
					malformed = true;
					return null;
				}
				if (foundEndTag)
				{
					if (entries.Count == 0)
					{
						malformed = true;
						return null;
					}
					malformed = false;
					return new Changelog(author, entries);
				}
				var firstColon = line.IndexOf(':');
				if (firstColon == -1)
				{
					malformed = true;
					return null;
				}

				var header = line.Substring(0, firstColon).ToUpperInvariant();
				var body = line.Substring(firstColon + 1, line.Length - firstColon - 1).Trim();
				ChangelogEntryType entryType;
				switch (header)
				{
					case "FIX":
					case "FIXES":
					case "BUGFIX":
						entryType = ChangelogEntryType.BugFix;
						break;
					case "RSCTWEAK":
					case "TWEAKS":
					case "TWEAK":
						entryType = ChangelogEntryType.Tweak;
						break;
					case "SOUNDADD":
						entryType = ChangelogEntryType.SoundAdd;
						break;
					case "SOUNDDEL":
						entryType = ChangelogEntryType.SoundDel;
						break;
					case "ADD":
					case "ADDS":
					case "RSCADD":
						entryType = ChangelogEntryType.RscAdd;
						break;
					case "IMAGEADD":
						entryType = ChangelogEntryType.ImageAdd;
						break;
					case "IMAGEDEL":
						entryType = ChangelogEntryType.ImageDel;
						break;
					case "TYPE":
					case "SPELLCHECK":
						entryType = ChangelogEntryType.SpellCheck;
						break;
					case "BALANCE":
					case "REBALANCE":
						entryType = ChangelogEntryType.Balance;
						break;
					case "CODE_IMP":
					case "CODE":
						entryType = ChangelogEntryType.Code_Imp;
						break;
					case "CONFIG":
						entryType = ChangelogEntryType.Config;
						break;
					case "ADMIN":
						entryType = ChangelogEntryType.Admin;
						break;
					case "SERVER":
						entryType = ChangelogEntryType.Server;
						break;
					case "REFACTOR":
						entryType = ChangelogEntryType.Refactor;
						break;
					default:
						//attempt to add it to the last as a new line
						var last = entries.LastOrDefault();
						if (last != null)
						{
							entries[entries.Count - 1] = new ChangelogEntry(String.Concat(last.Text, Environment.NewLine, body), last.Type);
							continue;
						}
						malformed = true;
						return null;
				}
				entries.Add(new ChangelogEntry(body, entryType));
			}
			malformed = false;
			return null;
		}

		/// <summary>
		/// Construct a <see cref="Changelog"/>
		/// </summary>
		/// <param name="author">The value of <see cref="Author"/></param>
		/// <param name="changes">The value of <see cref="Changes"/></param>
		Changelog(string author, List<ChangelogEntry> changes)
		{
			this.author = author;
			this.changes = changes;
		}
    }
}
