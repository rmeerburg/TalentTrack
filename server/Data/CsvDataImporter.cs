using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Server.Models;

namespace Server.Data
{
    class CsvDataImporter
    {
        private readonly string _seedFilesDirectory;
        private readonly TalentTrackContext _dbContext;
        public CsvDataImporter(TalentTrackContext dbContext, string seedFilesDirectory)
        {
            _seedFilesDirectory = seedFilesDirectory;
            _dbContext = dbContext;
        }

        public async Task SeedCategoriesAndLevels()
        {
            var matchLevelRegex = new Regex(@"(?<teamCategory>.*?);(?<category>.*?);(?<shortDescription>.*?);(?<longDescription>.*?);(?<teamCollectionScore>.*?);(?<score>.*?);");
            var levels = await File.ReadAllLinesAsync($"{_seedFilesDirectory}\\levels.csv");

            foreach (var line in levels.Skip(1))
            {
                var match = matchLevelRegex.Match(line);
                var category = _dbContext.ReviewCategories.Local.FirstOrDefault(cat => cat.Name == match.Groups["category"].Value);
                if (category == null)
                    _dbContext.ReviewCategories.Add(category = new ReviewCategory { Name = match.Groups["category"].Value, });

                _dbContext.Levels.Add(new Level { Category = category, ShortDescription = match.Groups["shortDescription"].Value, Description = match.Groups["longDescription"].Value, Score = int.Parse(match.Groups["score"].Value), TeamCategory = match.Groups["teamCategory"].Value, });
            }
        }

        public async Task SeedPlayers()
        {
            var matchPlayerRegex = new Regex(@"(?<name>.*?);(?<id>.*?);(?<dob>.*?);(?<gender>.*?);");
            foreach (var player in (await File.ReadAllLinesAsync($"{_seedFilesDirectory}\\players2.csv")).Skip(1))
            {
                var match = matchPlayerRegex.Match(player);
                _dbContext.Players.Add(new Player { Name = match.Groups["name"].Value, RegistrationId = match.Groups["id"].Value, Gender = match.Groups["gender"].Value == "M" ? Gender.Male : Gender.Female, Dob = DateTime.Parse(match.Groups["dob"].Value, CultureInfo.GetCultureInfo("nl-NL")) });
            }
        }

        public async Task SeedParticipations()
        {
            var matchTeamRegex = new Regex(@"(?<playerId>.*?);(?<teamId>.*?);.*?");
            var playerTeamSeasons = await File.ReadAllLinesAsync($"{_seedFilesDirectory}\\players2.csv");
            var seasonDescriptions = Regex.Matches(playerTeamSeasons.First(), @"(?<seasonId>\d+-\d+)").Select(m => m.Groups["seasonId"].Value).ToList();
            foreach (var season in seasonDescriptions)
                _dbContext.Seasons.Add(new Season { Description = season, StartDate = new DateTime(int.Parse($"20{season.Substring(0, 2)}"), 1, 1), EndDate = new DateTime(int.Parse($"20{season.Substring(0, 2)}"), 12, 31), });

            int i = 0;
            foreach (var teamLine in playerTeamSeasons.Skip(1))
            {
                var parts = teamLine.Split(";");
                var player = _dbContext.Players.Local.FirstOrDefault(p => p.RegistrationId == parts[1]);

                i = 0;
                foreach (var teamName in parts.Skip(4))
                {
                    if (teamName == "-")
                    {
                        i++;
                        continue;
                    }

                    Team team = _dbContext.Teams.Local.FirstOrDefault(t => t.Name == teamName);
                    if (team == null)
                        _dbContext.Teams.Add(team = new Team { Name = teamName, });

                    _dbContext.Participations.Add(new Participation { PlayerId = player.PlayerId, TeamId = team.TeamId, StartDate = DateTime.Today, SeasonId = _dbContext.Seasons.Local.First(s => s.Description == seasonDescriptions[i]).SeasonId });
                    i++;
                }
            }
        }
    }
}