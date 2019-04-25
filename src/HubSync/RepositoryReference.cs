using System.Text.RegularExpressions;

namespace VibrantCode.HubQ.SyncTool
{
    public struct RepositoryReference
    {
        private static readonly Regex _repoParser = new Regex(
            @"(https?://(www\.)?github.com/)?(?<owner>[a-zA-Z0-9-_]+)/(?<repo>[a-zA-Z0-9-_]+)/?");

        public string Owner { get; }
        public string Name { get; }
        public string Url => $"https://github.com/{Owner}/{Name}";

        public RepositoryReference(string owner, string name)
        {
            Owner = owner;
            Name = name;
        }

        public static bool TryParse(string reference, out RepositoryReference result)
        {
            var match = _repoParser.Match(reference);
            if (match.Success)
            {
                result = new RepositoryReference(
                    match.Groups["owner"].Value,
                    match.Groups["repo"].Value);
                return true;
            }

            result = default;
            return false;
        }
    }
}