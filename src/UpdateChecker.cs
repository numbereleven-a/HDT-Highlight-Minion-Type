using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HDTHighlightMinionType
{
	internal sealed class UpdateResult
	{
		public bool IsNewer { get; set; }
		public string Latest { get; set; } = "";
		public string Message { get; set; } = "";
	}

	internal static class UpdateChecker
	{
		private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(12);
		private const string ApiUrl = "https://api.github.com/repos/numbereleven-a/HDT-Highlight-Minion-Type/releases/latest";
		public const string ReleasesUrl = "https://github.com/numbereleven-a/HDT-Highlight-Minion-Type/releases";

		public static async Task<UpdateResult> CheckAsync(string current)
		{
			try
			{
				var request = WebRequest.CreateHttp(ApiUrl);
				request.UserAgent = "HDT-Highlight-Minion-Type/" + current;
				request.Accept = "application/vnd.github+json";
				request.Timeout = (int)RequestTimeout.TotalMilliseconds;
				request.ReadWriteTimeout = (int)RequestTimeout.TotalMilliseconds;
				var responseTask = request.GetResponseAsync();
				if(await Task.WhenAny(responseTask, Task.Delay(RequestTimeout)).ConfigureAwait(false) != responseTask)
				{
					request.Abort();
					try { await responseTask.ConfigureAwait(false); } catch { }
					return new UpdateResult { Message = "Update check timed out. Please try again." };
				}
				using(var response = await responseTask.ConfigureAwait(false))
				using(var stream = response.GetResponseStream())
				{
					var release = (Release?)new DataContractJsonSerializer(typeof(Release)).ReadObject(stream);
					var latest = release?.TagName?.TrimStart('v', 'V') ?? "";
					if(string.IsNullOrWhiteSpace(latest)) return new UpdateResult { Message = "The latest release has no version tag." };
					var newer = Compare(latest, current) > 0;
					return new UpdateResult { IsNewer = newer, Latest = latest, Message = newer ? "Version " + latest + " is available." : "You are up to date." };
				}
			}
			catch(WebException ex) when((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
			{
				return new UpdateResult { Message = "No GitHub release has been published yet." };
			}
			catch(Exception ex) { return new UpdateResult { Message = "Update check failed: " + ex.Message }; }
		}

		internal static int Compare(string left, string right)
		{
			var a = Parse(left); var b = Parse(right);
			for(var i = 0; i < Math.Max(a.numbers.Length, b.numbers.Length); i++)
			{
				var av = i < a.numbers.Length ? a.numbers[i] : 0;
				var bv = i < b.numbers.Length ? b.numbers[i] : 0;
				if(av != bv) return av.CompareTo(bv);
			}
			if(a.beta != b.beta) return a.beta ? -1 : 1;
			return a.betaNumber.CompareTo(b.betaNumber);
		}

		private static (int[] numbers, bool beta, int betaNumber) Parse(string value)
		{
			var normalized = value ?? "";
			var core = Regex.Match(normalized, @"^\s*[vV]?(?<version>\d+(?:\.\d+)*)");
			var matches = Regex.Matches(core.Success ? core.Groups["version"].Value : normalized, "\\d+");
			var values = new int[matches.Count];
			for(var i = 0; i < matches.Count; i++) int.TryParse(matches[i].Value, out values[i]);
			var suffix = core.Success ? normalized.Substring(core.Length) : normalized;
			var beta = Regex.IsMatch(suffix, @"(?:^|[.\-_])(?:beta|b)(?:\d|[.\-_]|$)", RegexOptions.IgnoreCase)
				|| Regex.IsMatch(suffix, @"^(?:beta|b)(?:\d|[.\-_]|$)", RegexOptions.IgnoreCase);
			var betaMatch = Regex.Match(suffix, @"(?:beta|b)[.\-_]?(?<number>\d+)", RegexOptions.IgnoreCase);
			var betaNumber = 0;
			if(betaMatch.Success) int.TryParse(betaMatch.Groups["number"].Value, out betaNumber);
			return (values, beta, betaNumber);
		}

		[DataContract]
		private sealed class Release
		{
			[DataMember(Name = "tag_name")]
			public string? TagName { get; set; }
		}
	}
}
