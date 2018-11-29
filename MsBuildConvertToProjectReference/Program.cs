// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MsBuildConvertToProjectReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using MsBuildConvertToProjectReference.Properties;

    class Program
    {
        static void Main(string[] args)
        {
            int errorCode = 0;

            if (args.Any())
            {
                string command = args.First().ToLowerInvariant();

                if (command.Equals("-?") || command.Equals("/?") || command.Equals("-help") || command.Equals("/help"))
                {
                    errorCode = ShowUsage();
                }
                else if (command.Equals("validatedirectory"))
                {
                    if (args.Length < 3)
                    {
                        errorCode = 1;
                        Console.WriteLine(StringResources.NotEnoughDirectoryArguments);
                    }
                    else
                    {
                        bool allDirectoriesValid = true;

                        // Validate the Remaining Arguments
                        foreach (string directoryArgument in args.Skip(1))
                        {
                            allDirectoriesValid = allDirectoriesValid && IsValidDirectoryArgument(directoryArgument);
                        }

                        if (allDirectoriesValid)
                        {
                            // The Error Code should be the number of projects that would be modified
                            errorCode = PrintToConsole(args.Skip(1), false);
                        }
                        else
                        {
                            errorCode = 9009;
                            Console.WriteLine(StringResources.OneOrMoreInvalidDirectories);
                        }
                    }
                }
                else
                {
                    if (args.Length < 2)
                    {
                        errorCode = 1;
                        Console.WriteLine(StringResources.NotEnoughDirectoryArguments);
                    }
                    else
                    {
                        bool allDirectoriesValid = true;

                        // Validate the Remaining Arguments
                        foreach (string directoryArgument in args)
                        {
                            allDirectoriesValid = allDirectoriesValid && IsValidDirectoryArgument(directoryArgument);
                        }

                        if (allDirectoriesValid)
                        {
                            PrintToConsole(args, true);

                            // If we're modifying we ALWAYS Return Error Code of Zero
                            errorCode = 0;
                        }
                        else
                        {
                            errorCode = 9009;
                            Console.WriteLine(StringResources.OneOrMoreInvalidDirectories);
                        }
                    }
                }
            }
            else
            {
                // This was a bad command
                errorCode = ShowUsage();
            }

            Environment.Exit(errorCode);
        }

        private static bool IsValidDirectoryArgument(string directoryArgument)
        {
            bool isValidDirectory = true;

            if (!Directory.Exists(directoryArgument))
            {
                Console.WriteLine(StringResources.InvalidDirectoryArgument, directoryArgument);
                isValidDirectory = false;
            }

            return isValidDirectory;
        }

        private static int ShowUsage()
        {
            Console.WriteLine(StringResources.HelpTextMessage);
            return 21;
        }

        static int PrintToConsole(IEnumerable<string> directoryArguments, bool saveChanges)
        {
            int convertedProjectCount = 0;

            IEnumerable<string> fixedProjects = ConvertToProjectReference.ForDirectory(directoryArguments.First(), directoryArguments.Skip(1), saveChanges);

            foreach (string fixedProject in fixedProjects)
            {
                convertedProjectCount++;
                Console.WriteLine(fixedProject);
            }

            return convertedProjectCount;
        }
    }
}
