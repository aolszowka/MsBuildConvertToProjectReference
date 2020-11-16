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
    using System.Linq;

    using MsBuildConvertToProjectReference.Properties;

    using NDesk.Options;

    public class Program
    {
        static void Main(string[] args)
        {
            MSBCTPROptions options = ParseForOptions(args);

            if (!options.LookupDirectories.Any())
            {
                Environment.ExitCode = -1;
                Console.WriteLine(Strings.NotEnoughDirectoryArguments);
            }
            else
            {
                // First Ensure that all Directories are Valid
                if (IsValidDirectoryArgument(options.TargetDirectory))
                {
                    bool allDirectoriesValid = true;

                    // Validate the Remaining Arguments
                    foreach (string directoryArgument in options.LookupDirectories)
                    {
                        allDirectoriesValid = allDirectoriesValid && IsValidDirectoryArgument(directoryArgument);
                    }

                    if (allDirectoriesValid)
                    {
                        bool saveChanges = options.Validate == false;

                        Environment.ExitCode = PrintToConsole(options.TargetDirectory, options.LookupDirectories, saveChanges);

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

        public static MSBCTPROptions ParseForOptions(string[] args)
        {
            List<string> lookupDirectories = new List<string>();

            MSBCTPROptions options = new MSBCTPROptions();

            OptionSet p = new OptionSet()
            {
                { "<>", Strings.TargetArgumentDescription, v => options.TargetDirectory = v },
                { "validate", Strings.ValidateDescription, v => options.Validate = v != null },
                { "lookupdirectory=|ld=", Strings.LookupDirectoryArgumentDescription, v => lookupDirectories.Add(v) },
                { "?|h|help", Strings.HelpDescription, v => options.ShowHelp = v != null },
            };

            try
            {
                p.Parse(args);
                options.LookupDirectories = lookupDirectories;
            }
            catch (OptionException)
            {
                Console.WriteLine(Strings.ShortUsageMessage);
                Console.WriteLine($"Try `--help` for more information.");
                Environment.Exit(160);
            }

            if (options.ShowHelp || string.IsNullOrEmpty(options.TargetDirectory))
            {
                int exitCode = ShowUsage(p);
                Environment.Exit(exitCode);
            }

            return options;
        }

        private static int ShowUsage(OptionSet p)
        {
            Console.WriteLine(Strings.ShortUsageMessage);
            Console.WriteLine();
            Console.WriteLine(Strings.LongDescription);
            Console.WriteLine();
            Console.WriteLine($"               <>            {Strings.TargetArgumentDescription}");
            p.WriteOptionDescriptions(Console.Out);
            return 160;
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
