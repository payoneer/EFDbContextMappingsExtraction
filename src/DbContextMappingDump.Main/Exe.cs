using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DbContextMappingDump
{
    internal static class Exe
    {
        public static (int exitCode, string output) Run(
            string executable,
            IReadOnlyList<string> args,
            string workingDirectory = null,
            string terminationText = "")
        {
            var arguments = ToArguments(args);


            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            if (workingDirectory != null)
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            Process process = null;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (System.Exception ex)
            {
                throw new Exception($"Error while launching {executable} with workingDir {workingDirectory} with args: {arguments}", ex);
            }



            string line;
            StringBuilder sb = new StringBuilder();
            while ((line = process.StandardOutput.ReadLine()) != null && !process.HasExited)
            {
                System.Console.WriteLine(line);
                sb.Append(line);
                if (!string.IsNullOrEmpty(terminationText) && sb.ToString().Contains(terminationText))
                {
                    try { process.Kill(); } catch { }
                }
            }
            sb.Append(process.StandardOutput.ReadToEnd());
            process.WaitForExit();


            var output = sb.ToString();
            return (process.ExitCode, output);
        }

        private static string ToArguments(IReadOnlyList<string> args)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < args.Count; i++)
            {
                if (i != 0)
                {
                    builder.Append(" ");
                }

                if (args[i].IndexOf(' ') == -1)
                {
                    builder.Append(args[i]);

                    continue;
                }

                builder.Append("\"");

                var pendingBackslashes = 0;
                for (var j = 0; j < args[i].Length; j++)
                {
                    switch (args[i][j])
                    {
                        case '\"':
                            if (pendingBackslashes != 0)
                            {
                                builder.Append('\\', pendingBackslashes * 2);
                                pendingBackslashes = 0;
                            }

                            builder.Append("\\\"");
                            break;

                        case '\\':
                            pendingBackslashes++;
                            break;

                        default:
                            if (pendingBackslashes != 0)
                            {
                                if (pendingBackslashes == 1)
                                {
                                    builder.Append("\\");
                                }
                                else
                                {
                                    builder.Append('\\', pendingBackslashes * 2);
                                }

                                pendingBackslashes = 0;
                            }

                            builder.Append(args[i][j]);
                            break;
                    }
                }

                if (pendingBackslashes != 0)
                {
                    builder.Append('\\', pendingBackslashes * 2);
                }

                builder.Append("\"");
            }

            return builder.ToString();
        }
    }
}
