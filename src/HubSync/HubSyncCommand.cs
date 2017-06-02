using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HubSync
{
    public class HubSyncCommand
    {
        private readonly string _userName;
        private readonly string _token;
        private readonly string _sqlConnectionString;
        private readonly IList<string> _repositories;
        private readonly TextWriter _stdout;
        private readonly TextWriter _stderr;

        public HubSyncCommand(string userName, string token, string sqlConnectionString, IList<string> repositories, TextWriter stdout, TextWriter stderr)
        {
            _userName = userName;
            _token = token;
            _sqlConnectionString = sqlConnectionString;
            _repositories = repositories;
            _stdout = stdout;
            _stderr = stderr;

            if (repositories.Count == 0)
            {
                throw new CommandLineException("At least one repository must be specified");
            }
        }

        public Task<int> ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
