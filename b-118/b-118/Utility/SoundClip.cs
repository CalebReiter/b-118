using System;
using System.Collections.Generic;
using System.IO;

namespace b_118.Utility
{
    class SoundClip
    {
        private string _directory;
        private string _regex;
        private string _extension;

        public SoundClip(string directory, string regex, string extension = ".mp3")
        {
            _directory = directory;
            _regex = regex;
            _extension = extension;
        }

        public FileInfo LoadClip(string filename)
        {
            return new FileInfo($"{_directory}/{filename}{_extension}");
        }

        public bool Verify(FileInfo fileInfo)
        {
            bool success = System.Text.RegularExpressions.Regex.Match(fileInfo.FullName, _regex).Success;
            return fileInfo.Exists && success && fileInfo.Extension == _extension;
        }

        public string[] ListDirectories()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(_directory);
            List<string> names = new List<string>();
            foreach (DirectoryInfo d in directoryInfo.GetDirectories())
            {
                names.Add(d.Name);
            }
            return names.ToArray();
        }

        public string[] ListFileNames(string directory)
        {
            bool success = System.Text.RegularExpressions.Regex.Match(directory, "^[a-zA-Z0-9_]*$").Success;
            if (!success)
            {
                throw new InvalidOperationException("Invalid board name.");
            }
            DirectoryInfo directoryInfo = new DirectoryInfo($"{_directory}/{directory}");
            if (!directoryInfo.Exists)
            {
                throw new InvalidOperationException("Board doesn't exist.");
            }
            List<string> names = new List<string>();
            foreach (FileInfo f in directoryInfo.GetFiles())
            {
                names.Add(f.Name.Split(f.Extension)[0]);
            }
            return names.ToArray();
        }

    }
}
