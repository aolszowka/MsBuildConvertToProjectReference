// -----------------------------------------------------------------------
// <copyright file="ProjectInformation.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MsBuildConvertToProjectReference
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    /// <summary>
    ///     Class to Convert MSBuild-Project Reference Tags to
    /// ProjectReference Tags when the project can be identified.
    /// </summary>
    /// <remarks>
    /// This operates on the assumption that each Project has a Distinct
    /// AssemblyName Property.
    /// 
    /// It first scans the given <c>lookupDirectories</c> for any MSBuild
    /// Project Style Project (Assuming it has an AssemblyName Tag) and
    /// extracts these into a Lookup Dictionary. This Dictionary is key'ed
    /// off of the AssemblyName (hence the need to be unique) the projects
    /// are then scanned for any Reference Element looking at the include
    /// stripping to JUST the AssemblyName (the Version is not considered)
    /// It then scans the dictionary to see if it is a "known" project. If
    /// so the Reference Element is removed and then replaced with a
    /// ProjectReference Element.
    /// 
    /// See https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items?view=vs-2017
    /// </remarks>
    internal static class ConvertToProjectReference
    {
        private static XNamespace msbuildNS = "http://schemas.microsoft.com/developer/msbuild/2003";

        /// <summary>
        ///     Performs the Convert of Reference Tags to ProjectReference
        /// Tags for all projects located in the given directory using the
        /// given Lookup Directories its source.
        /// </summary>
        /// <param name="targetDirectory">The directory for which to convert Projects.</param>
        /// <param name="lookupDirectories">An <see cref="IEnumerable{String}"/> of Lookup Directories</param>
        /// <param name="saveChanges">Indicates if changes to the project should be written back to the project file.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of Projects that were modified.</returns>
        /// <remarks>
        /// See <see cref="GetProjectsInDirectory(string)"/> to understand which projects will be modified.
        /// </remarks>
        internal static IEnumerable<string> ForDirectory(string targetDirectory, IEnumerable<string> lookupDirectories, bool saveChanges)
        {
            IDictionary<string, ProjectInformation> lookupDictionary = GetProjectLookupDictionary(lookupDirectories);
            IEnumerable<string> projectFilesToConvert = GetProjectsInDirectory(targetDirectory);

            foreach (string projFile in projectFilesToConvert)
            {
                if (ForSingleFile(projFile, lookupDictionary, saveChanges))
                {
                    yield return projFile;
                }
            }
        }

        /// <summary>
        /// Gets all Project Files that are understood by this
        /// tool from the given directory and all subdirectories.
        /// </summary>
        /// <param name="targetDirectory">The directory to scan for projects.</param>
        /// <returns>All projects that this tool supports.</returns>
        internal static IEnumerable<string> GetProjectsInDirectory(string targetDirectory)
        {
            string[] supportedFileExtensions = new string[] { ".csproj", ".vbproj", "dblnet.synproj" };

            return
                Directory
                .EnumerateFiles(targetDirectory, "*proj", SearchOption.AllDirectories)
                .Where(currentFile => supportedFileExtensions.Any(supportedFileExtension => currentFile.EndsWith(supportedFileExtension, StringComparison.InvariantCultureIgnoreCase)));
        }

        /// <summary>
        ///     Converts a Single MSBuild Style Project's Reference Elements
        /// to ProjectReference Elements if they exist in the Lookup Dictionary.
        /// </summary>
        /// <param name="projFile">An MSBuild Style Project File</param>
        /// <param name="lookupDictionary">A Lookup Dictionary Created By <see cref="GetProjectLookupDictionary(IEnumerable{string})"/></param>
        /// <param name="saveChanges">Indicates if changes to the project should be written back to the project file.</param>
        /// <returns><c>true</c> if any Reference were converted to ProjectReference; otherwise, returns <c>false</c>.</returns>
        internal static bool ForSingleFile(string projFile, IDictionary<string, ProjectInformation> lookupDictionary, bool saveChanges)
        {
            // Load up the project file
            XDocument projXml = XDocument.Load(projFile);

            // First get all of the Assembly References
            XElement[] referencedAssemblyElements =
                projXml
                .Descendants(msbuildNS + "Reference")
                .ToArray();

            // Now for each of these assemblies perform a lookup
            foreach (XElement referencedAssemblyElement in referencedAssemblyElements)
            {
                // First grab the Assembly Name
                string includeAssemblyNameValue = referencedAssemblyElement.Attribute("Include").Value;

                string referencedAssemblyName = string.Empty;

                try
                {
                    referencedAssemblyName =
                        new AssemblyName(includeAssemblyNameValue).Name.ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    string exceptionMessage = $"Failed to Get Assembly Name for Reference `{includeAssemblyNameValue}` in Project `{projFile}`.";
                    throw new InvalidOperationException(exceptionMessage, ex);
                }

                if (lookupDictionary.ContainsKey(referencedAssemblyName))
                {
                    // Create a Project Reference Element
                    XElement projectReferenceFragment = _GenerateProjectReferenceFragment(projFile, lookupDictionary[referencedAssemblyName]);

                    // Determine Where to Insert Project References
                    XElement projectReferenceItemGroup = _LocateOrCreateProjectReferenceItemGroup(projXml);

                    // Now Insert it
                    projectReferenceItemGroup.Add(projectReferenceFragment);

                    // Now remove the previous reference element
                    referencedAssemblyElement.Remove();
                }
            }

            // See if there were any changes to this file
            bool hasChanges = projXml.ToString() != XDocument.Load(projFile).ToString();

            if (hasChanges && saveChanges)
            {
                projXml.Save(projFile);
            }

            return hasChanges;
        }

        /// <summary>
        /// Locate or Create an ItemGroup Specifically for ProjectReferences
        /// </summary>
        /// <param name="projXml">An MSBuild Style Project File.</param>
        /// <returns>The ItemGroup Element for ProjectReference Elements.</returns>
        /// <remarks>
        /// This attempts to mimic what Visual Studio would do.
        /// </remarks>
        private static XElement _LocateOrCreateProjectReferenceItemGroup(XDocument projXml)
        {
            XElement result;

            // First see if any ProjectReference Items Exist; if so add them to that ItemGroup
            XElement existingItemGroup = projXml.Descendants(msbuildNS + "ProjectReference").FirstOrDefault()?.Parent;

            if (existingItemGroup != null)
            {
                result = existingItemGroup;
            }
            else
            {
                // We're going to create an ItemGroup attached right after the Reference ItemGroup
                XElement existingReferenceItemGroup = projXml.Descendants(msbuildNS + "Reference").LastOrDefault().Parent;
                XElement newItemGroup = new XElement(msbuildNS + "ItemGroup");
                existingReferenceItemGroup.AddAfterSelf(newItemGroup);
                result = newItemGroup;
            }

            return result;
        }

        /// <summary>
        /// Generates a well-formed ProjectReference element
        /// </summary>
        /// <param name="projFile">The path to the project file that will contain this element.</param>
        /// <param name="reference">A <see cref="ProjectInformation"/> for the Project to add a reference to.</param>
        /// <returns>A well-formed ProjectReference Element.</returns>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items?view=vs-2017
        /// for a description of what constitutes a well-formed ProjectReference Element.
        /// </remarks>
        private static XElement _GenerateProjectReferenceFragment(string projFile, ProjectInformation reference)
        {
            XElement projectReference =
                new XElement(
                    msbuildNS + "ProjectReference",
                    new XAttribute("Include", PathUtilities.GetRelativePath(projFile, reference.Path)),
                    new XElement(msbuildNS + "Project", reference.ProjectGuid),
                    new XElement(msbuildNS + "Name", Path.GetFileNameWithoutExtension(reference.Path)));

            return projectReference;
        }

        /// <summary>
        /// Creates a Lookup Dictionary that is Keyed off of the AssemblyName.
        /// </summary>
        /// <param name="targetDirectories">The Directory to Scan for Projects.</param>
        /// <returns>A <see cref="IDictionary{TKey, TValue}"/> where the Key is the AssemblyName (case-insensitive) and the Value is a <see cref="ProjectInformation"/> for the project.</returns>
        /// <remarks>
        /// See <see cref="GetProjectsInDirectory(string)"/> to learn which projects will be parsed.
        /// </remarks>
        public static IDictionary<string, ProjectInformation> GetProjectLookupDictionary(IEnumerable<string> targetDirectories)
        {
            ConcurrentDictionary<string, ProjectInformation> lookupDictionary = new ConcurrentDictionary<string, ProjectInformation>(StringComparer.InvariantCultureIgnoreCase);
            IEnumerable<string> projFiles = targetDirectories.SelectMany(targetDirectory => GetProjectsInDirectory(targetDirectory));


            Parallel.ForEach(projFiles, projFile =>
            {
                ProjectInformation pi = ProjectInformation.Parse(projFile);

                if (!lookupDictionary.TryAdd(pi.AssemblyName.ToLowerInvariant(), pi))
                {
                    string exceptionMessage = $"Name was duplicated (case-insensitive)! `{pi.AssemblyName}`";
                    throw new InvalidOperationException(exceptionMessage);
                }
            });

            return lookupDictionary;
        }
    }
}
