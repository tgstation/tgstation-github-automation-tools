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
			foreach (var line in pullRequest?.Body?.Split('\n') ?? throw new ArgumentNullException(nameof(pullRequest)))
			{
				if (String.IsNullOrWhiteSpace(line))
					continue;
				var foundStartTag = line.StartsWith(":cl:") || line.StartsWith("🆑");
				var foundEndTag = line.StartsWith("/:cl:") || line.StartsWith("/🆑");
				if (author == null)
				{
					if (foundEndTag)
					{
						malformed = true;
						return null;
					}
					if (foundStartTag)
					{
						author = line.Replace(":cl:", String.Empty).Replace("🆑", String.Empty).Trim();
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

				var header = line.Substring(0, firstColon).ToLowerInvariant();
				var body = line.Substring(firstColon + 1, line.Length - firstColon - 1).Trim();
				ChangelogEntryType entryType;
				switch (header)
				{
					case "fix":
					case "fixes":
					case "bugfix":
						entryType = ChangelogEntryType.BugFix;
						break;
					case "rsctweak":
					case "tweaks":
					case "tweak":
						entryType = ChangelogEntryType.Tweak;
						break;
					case "soundadd":
						entryType = ChangelogEntryType.SoundAdd;
						break;
					case "sounddel":
						entryType = ChangelogEntryType.SoundDel;
						break;
					case "add":
					case "adds":
					case "rscadd":
						entryType = ChangelogEntryType.RscAdd;
						break;
					case "imageadd":
						entryType = ChangelogEntryType.ImageAdd;
						break;
					case "imagedel":
						entryType = ChangelogEntryType.ImageDel;
						break;
					case "type":
					case "spellcheck":
						entryType = ChangelogEntryType.SpellCheck;
						break;
					case "balance":
					case "rebalance":
						entryType = ChangelogEntryType.Balance;
						break;
					case "code_imp":
					case "code":
						entryType = ChangelogEntryType.Code_Imp;
						break;
					case "config":
						entryType = ChangelogEntryType.Config;
						break;
					case "admin":
						entryType = ChangelogEntryType.Admin;
						break;
					case "server":
						entryType = ChangelogEntryType.Server;
						break;
					case "refactor":
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
