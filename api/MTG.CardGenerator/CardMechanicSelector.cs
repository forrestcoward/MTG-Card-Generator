using Newtonsoft.Json;

namespace MTG.CardGenerator
{
    public class CardMechanic
    {
        public string Keyword { get; set; }
        [JsonProperty("reminder_text")]
        public string ReminderText { get; set; }
        public string[] Examples { get; set; } = [];
        public string Notes { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// Handles parsing and random selection of Magic: The Gathering card mechanics.
    /// </summary>
    public static class CardMechanicSelector
    {
        private static readonly Random random = new();
        private static List<CardMechanic> evergreenKeywords;
        private static List<CardMechanic> actionKeywords;
        private static List<CardMechanic> cardMechanics;
        private static readonly string evergreenKeywordFileName = "mtg-evergreen-keywords.json";
        private static readonly string actionKeywordFileName = "mtg-keyword-actions.json";
        private static readonly string mechanicsFileName = "mtg-mechanics.json";
        private static bool initialized = false;

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            evergreenKeywords = LoadMechanicsFromFile(evergreenKeywordFileName);
            actionKeywords = LoadMechanicsFromFile(actionKeywordFileName);
            cardMechanics = LoadMechanicsFromFile(mechanicsFileName);
            initialized = true;
        }

        public static string GetRandomMechanicsAsPrompt(int evergreenCount, int keywordActionCount, int mechanicCount, out List<CardMechanic> selectedMechanics)
        {
            Initialize();
            var (evergreen, action, mechanics) = GetRandomMechanics(evergreenCount, keywordActionCount, mechanicCount);
            selectedMechanics = evergreen.Concat(action).Concat(mechanics).ToList();
            // Combine all mechanics into one list.
            var allMechanics = evergreen.Concat(action).Concat(mechanics).ToList();
            var prompt = "Please find creative ways to incorporate the following mechanics into the generated card. The examples provided in each mechanic are for illustration purposes. Incorporate these mechanics in a consistent and coherhent way with regards to the rest of the user prompt and card.\n";
            prompt += "Mechanics to incorporate into the generated card:\n";
            prompt += string.Join("\n\n", allMechanics.Select(m => JsonConvert.SerializeObject(m, Formatting.Indented)));
            return prompt;
        }

        public static (List<CardMechanic> EvergreenKeywords, List<CardMechanic> ActionKeywords, List<CardMechanic> Mechanics) GetRandomMechanics(int evergreenCount, int keywordActionCount, int mechanicCount)
        {
            Initialize();
            return (GetRandomItems(evergreenKeywords, evergreenCount), GetRandomItems(actionKeywords, keywordActionCount), GetRandomItems(cardMechanics, mechanicCount));
        }

        private static List<T> GetRandomItems<T>(List<T> sourceList, int count)
        {
            if (sourceList == null || !sourceList.Any())
            {
                return new List<T>();
            }

            count = Math.Min(count, sourceList.Count);
            if (count <= 0)
            {
                return new List<T>();
            }

            return sourceList.OrderBy(x => random.Next()).Take(count).ToList();
        }

        private static List<CardMechanic> LoadMechanicsFromFile(string filePath)
        {
            // Build the full path relative to the application base directory.
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            var json = File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<List<CardMechanic>>(json);
        }
    }
}