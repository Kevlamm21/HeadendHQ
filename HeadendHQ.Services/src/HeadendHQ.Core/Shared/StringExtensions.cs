using System.Text.RegularExpressions;

namespace HeadendHQ.Core;

public static class StringExtensions
{
	public static string ToTitleCase(this string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return string.Empty;
		}

		return string
			.Join(" ", Regex.Split(input, "(?=[A-Z])")
			.Where(word => !string.IsNullOrEmpty(word))
			.Select(word =>
			{
				if (word.Length == 0)
				{
					return string.Empty;
				}

				if (word.Length == 1)
				{
					return char.ToUpper(word[0]).ToString();
				}

				return char.ToUpper(word[0]) + word[1..];
			}));
	}

	public static string? Normalize(string? s)
	{
		return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
	}
}
