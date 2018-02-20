using System.ComponentModel.DataAnnotations;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Simple string KV store
	/// </summary>
    sealed class KeyValuePair
    {
		/// <summary>
		/// The key for the data
		/// </summary>
		[Key]
		public string Key { get; set; }
		/// <summary>
		/// The value of the data
		/// </summary>
		public string Value { get; set; }
    }
}
