using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hubbup.MikLabelModel;
using Octokit;

namespace CreateMikLabelModel
{
    public class Program
    {
        private static readonly string Version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        private static readonly (string owner, string repo)[] Repos = new[]
        {
            ("aspnet", "AspNetCore"),
            ("aspnet", "Extensions"),
        };

        static async Task<int> Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: CreateMikLabelModel <GitHubToken>");
                return 1;
            }

            var token = args[0];

            foreach (var repo in Repos)
            {
                var tsvRawGitHubDataPath = $"{repo.owner}-{repo.repo}-issueData.tsv";
                var modelOutputDataPath = $"{repo.owner}-{repo.repo}-MikModel.zip";

                var github = CreateGitHubClient(token);

                await GetGitHubIssueData(github, repo.owner, repo.repo, outputPath: tsvRawGitHubDataPath);

                //This line re-trains the ML Model
                MLHelper.BuildAndTrainModel(
                    tsvRawGitHubDataPath,
                    modelOutputDataPath,
                    MyTrainerStrategy.OVAAveragedPerceptronTrainer);

                Console.WriteLine(new string('-', 80));
                Console.WriteLine();
            }

            Console.WriteLine($"Please remember to copy the ZIP files to the web site's ML folder");
            return 0;
        }

        private static async Task GetGitHubIssueData(GitHubClient github, string owner, string repo, string outputPath)
        {
            Console.WriteLine($"Getting all issues for {owner}/{repo}...");

            var stopWatch = Stopwatch.StartNew();

            var issuesOfInterest = new List<Issue>();

            var url = new Uri($"https://api.github.com/repos/{owner}/{repo}/issues");
            var parameters = new Dictionary<string, string>()
            {
                { "state", "all" },
                { "per_page", "100" }
            };

            var totalCount = 0;
            while (url != null)
            {
                var resp = await github.Connection.Get<IReadOnlyList<Issue>>(url, parameters, AcceptHeaders.StableVersion);
                url = resp.HttpResponse.ApiInfo.GetNextPageUrl();

                foreach (var issue in resp.Body)
                {
                    if (issue.PullRequest == null && issue.Labels.Any(l => l.Name.StartsWith("area-")))
                    {
                        issuesOfInterest.Add(issue);
                    }
                }

                totalCount += resp.Body.Count;
                Console.WriteLine($"Processed {totalCount} issues (Remaining Rate Limit: {resp.HttpResponse.ApiInfo.RateLimit.Remaining})...");
            }

            Console.WriteLine($"Found {issuesOfInterest.Count} issues of interest out of {totalCount} total issues.");

            Console.WriteLine($"Writing to output TSV file {outputPath}...");

            using (var outputWriter = new StreamWriter(outputPath))
            {
                outputWriter.WriteLine("ID\tArea\tTitle\tDescription");
                foreach (var issue in issuesOfInterest)
                {
                    // Create a row for each area, since the expectation is that the issue was equally relevant to both areas.
                    foreach (var area in issue.Labels.Where(l => l.Name.StartsWith("area-", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Exclude PRs since we label those using the labeller bot (at least for now)
                        var body = issue.Body.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
                        outputWriter.WriteLine($"{issue.Number}\t{area.Name}\t{issue.Title}\t{body}");
                    }
                }
            }

            stopWatch.Stop();
            Console.WriteLine($"Done writing TSV in {stopWatch.ElapsedMilliseconds}ms");
        }

        public static GitHubClient CreateGitHubClient(string token)
        {
            var connection = new Connection(new ProductHeaderValue("MikLabel", Version))
            {
                Credentials = new Credentials(token)
            };
            var ghc = new GitHubClient(connection);

            return ghc;
        }
    }
}
