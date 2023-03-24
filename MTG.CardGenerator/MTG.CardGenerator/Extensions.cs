using System.Collections.Generic;
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
    }
}
