// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018-2020. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MsBuildConvertToProjectReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using MsBuildConvertToProjectReference.Properties;

    using NDesk.Options;

    class Program
    {
        static void Main(string[] args)
        {

            string targetDirectory = string.Empty;
            IDictionary<string, string> lookupDirectories = new Dictionary<string, string>();
            bool validateOnly = false;
            bool showHelp = false;

            OptionSet p = new OptionSet()
            {
                { "<>", Strings.TargetArgumentDescription, v => targetDirectory = v },
                { "validate", Strings.ValidateDescription, v => validateOnly = v != null },
                { "lookupdirectory={:}{/}|ld={:}{/}", Strings.LookupDirectoryArgumentDescription, (n,v) => lookupDirectories.Add(n, v) },
                { "?|h|help", Strings.HelpDescription, v => showHelp = v != null },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException)
            {
                Console.WriteLine(Strings.ShortUsageMessage);
                Console.WriteLine($"Try `{Strings.ProgramName} --help` for more information.");
                Environment.ExitCode = 21;
                return;
            }

            if (showHelp || string.IsNullOrEmpty(targetDirectory))
            {
                Environment.ExitCode = ShowUsage(p);
            }
            else if (lookupDirectories.Count == 0)
            {
                Environment.ExitCode = -1;
                Console.WriteLine(Strings.NotEnoughDirectoryArguments);
            }
            else
            {
                // First Ensure that all Directories are Valid
                if (IsValidDirectoryArgument(targetDirectory))
                {
                    bool allDirectoriesValid = true;

                    // Validate the Remaining Arguments
                    foreach (string directoryArgument in lookupDirectories.Values)
                    {
                        allDirectoriesValid = allDirectoriesValid && IsValidDirectoryArgument(directoryArgument);
                    }

                    if (allDirectoriesValid)
                    {
                        bool saveChanges = validateOnly == false;

                        Environment.ExitCode = PrintToConsole(targetDirectory, lookupDirectories.Values, saveChanges);

                        if (saveChanges)
                        {
                            // Always Return Zero
                            Environment.ExitCode = 0;
                        }
                    }
                    else
                    {
                        Environment.ExitCode = -1;
                        Console.WriteLine(Strings.OneOrMoreInvalidDirectories);
                    }
                }
                else
                {
                    Environment.ExitCode = -1;
                    Console.WriteLine(Strings.OneOrMoreInvalidDirectories);
                }
            }
        }

        private static int ShowUsage(OptionSet p)
        {
            Console.WriteLine(Strings.ShortUsageMessage);
            Console.WriteLine();
            Console.WriteLine(Strings.LongDescription);
            Console.WriteLine();
            Console.WriteLine($"               <>            {Strings.TargetArgumentDescription}");
            p.WriteOptionDescriptions(Console.Out);
            return 21;
        }

        private static bool IsValidDirectoryArgument(string directoryArgument)
        {
            bool isValidDirectory = true;

            if (!Directory.Exists(directoryArgument))
            {
                Console.WriteLine(Strings.InvalidDirectoryArgument, directoryArgument);
                isValidDirectory = false;
            }

            return isValidDirectory;
        }

        static int PrintToConsole(string targetDirectory, IEnumerable<string> lookupDirectories, bool saveChanges)
        {
            int convertedProjectCount = 0;

            IEnumerable<string> fixedProjects = ConvertToProjectReference.ForDirectory(targetDirectory, lookupDirectories, saveChanges);

            foreach (string fixedProject in fixedProjects)
            {
                convertedProjectCount++;
                Console.WriteLine(fixedProject);
            }

            return convertedProjectCount;
        }
    }
}
