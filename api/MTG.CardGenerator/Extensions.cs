using System.Collections.Generic;
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

        public static string AddBracketsAroundCharacters(this string input)
        {
            var result = new StringBuilder();
            foreach (char c in input)
            {
                result.Append('{');
                result.Append(c);
                result.Append('}');
            }
            return result.ToString();
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
    }
}
