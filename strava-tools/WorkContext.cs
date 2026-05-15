using System;
using System.IO;
using System.Threading;

namespace StravaTools
{
    public class WorkContext
    {
        public DateTime timeCreated { get; set; } = DateTime.MinValue;

        public bool HelpOrVersion { get; set; }

        public DirectoryInfo localappdata_dir { get; set; } = null;

        public DirectoryInfo backup_dir { get; set; } = null;
        public DirectoryInfo original_backup_dir { get; set; } = null;
        public DirectoryInfo modified_backup_dir { get; set; } = null;

        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public CancellationToken Token { get { return CancellationTokenSource.Token; } }

        public string ClientID { get; set; } = null;
        public string ClientSecret { get; set; } = null;
    }
}
