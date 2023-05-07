using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MTG.CardGenerator
{
    public static class Extensions
    {
        public static Dictionary<string, string> GetNamedGroupsMatches(this Regex regex, string input)
        {
            var namedGroupMatches = new Dictionary<string, string>();

            var match = regex.Match(input);
            if (match.Success)
            {
                foreach (string groupName in regex.GetGroupNames())
                {
                    if (int.TryParse(groupName, out _)) // Skip non-named groups (represented by numbers)
                    {
                        continue;
                    }

                    namedGroupMatches[groupName] = match.Groups[groupName].Value;
                }
            }

            return namedGroupMatches;
        }

        public static IEnumerable<string> GetAllMatches(this Regex regex, string input)
        {
            var stringMatches = new List<string>();
            var matches = new List<Match>();
            matches.AddRange(regex.Matches(input));

            foreach (var match in matches)
            {
                if (match.Success)
                {
                    foreach (string groupName in regex.GetGroupNames())
                    {
                        if (int.TryParse(groupName, out _)) // Skip non-named groups (represented by numbers)
                        {
                            continue;
                        }

                        stringMatches.Add(match.Groups[groupName].Value);
                    }
                }
            }

            return stringMatches;
        }

        // Add '{' and '}' around legal mana character symbols. This is used to correct improperly formatted mana costs in card titles and oracle text.
        public static string BracketManaSymbols(this string input)
        {
            // TODO: Define this somewhere more globally. May need to add other symbols too, like 'X' and 'T'.
            var validManaSymbols = new[] { 'R', 'W', 'G', 'U', 'B', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            var result = new StringBuilder();
            foreach (char c in input)
            {
                if (!validManaSymbols.Contains(c))
                {
                    // Do not bracket non mana symbols, just add them back to the output.
                    result.Append(c);
                    continue;
                }

                result.Append('{');
                result.Append(c);
                result.Append('}');
            }
            return result.ToString();
        }

        public static bool IsNumericOrWhitespace(this string input)
        {
            return new Regex(@"^[0-9\s]*$").IsMatch(input);
        }

        public static bool IsBracketed(this string input)
        {
            if (input.Length % 3 != 0)
            {
                return false;
            }

            for (int i = 0; i < input.Length; i += 3)
            {
                if (input[i] != '{' || input[i + 2] != '}')
                {
                    return false;
                }
            }

            return true;
        }

        public static string FirstCharToLowerCase(this string str)
        {
            if (!string.IsNullOrEmpty(str) && char.IsUpper(str[0]))
                return str.Length == 1 ? char.ToLower(str[0]).ToString() : char.ToLower(str[0]) + str[1..];

            return str;
        }
    }
}
