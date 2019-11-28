using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VibrantCode.HubQ.Web.Data
{
    public class EmojiTable
    {
        private readonly Regex _emojiDetector = new Regex(@":(?<alias>[a-zA-Z-_]+):");

        public EmojiTable(IReadOnlyList<EmojiDescription> emoji)
        {
            AllEmoji = emoji;

            var emojiDict = new Dictionary<string, EmojiDescription>();
            foreach (var emojiItem in emoji)
            {
                foreach (var alias in emojiItem.Aliases)
                {
                    emojiDict[alias] = emojiItem;
                }
            }
            EmojiByAlias = emojiDict;
        }

        public IReadOnlyDictionary<string, EmojiDescription> EmojiByAlias { get; private set; }
        public IReadOnlyList<EmojiDescription> AllEmoji { get; private set; }

        public bool TryGetEmoji(string alias, out EmojiDescription emoji) => EmojiByAlias.TryGetValue(alias, out emoji);

        public string ReplaceEmoji(string input)
        {
            return _emojiDetector.Replace(input, (match) =>
            {
                var alias = match.Groups["alias"].Value;
                if (TryGetEmoji(alias, out var emoji))
                {
                    return emoji.Emoji;
                }
                else
                {
                    return match.Value;
                }
            });
        }
        internal static async Task<EmojiTable> LoadAsync(Stream stream)
        {
            var descriptions = await JsonSerializer.DeserializeAsync<EmojiDescription[]>(stream,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                });
            return new EmojiTable(descriptions);
        }

    }

    public class EmojiDescription
    {
        public string Emoji { get; set; }
        public string[] Aliases { get; set; }
    }
}
