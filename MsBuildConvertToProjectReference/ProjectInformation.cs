// -----------------------------------------------------------------------
// <copyright file="ProjectInformation.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MsBuildConvertToProjectReference
{
    using System;
    using System.Linq;
    using System.Xml.Linq;

    public class ProjectInformation
    {
        static XNamespace msbuildNS = "http://schemas.microsoft.com/developer/msbuild/2003";

        public static ProjectInformation Parse(string projectFile)
        {
            ProjectInformation result = new ProjectInformation();

            XDocument projXml = XDocument.Load(projectFile);

            result.Path = projectFile;
            result.AssemblyName = _GetAssemblyName(projXml);
            result.ProjectGuid = _GetProjectGuid(projXml);

            return result;
        }

        public string AssemblyName
        {
            get;
            set;
        }

        public string ProjectGuid
        {
            get;
            set;
        }

        public string Path
        {
            get;
            set;
        }

        private static string _GetAssemblyName(XDocument projXml)
        {
            string assemblyName = string.Empty;

            XElement assemblyNameElement = projXml.Descendants(msbuildNS + "AssemblyName").FirstOrDefault();

            if (assemblyNameElement == null)
            {
                string exceptionMessage = $"The Project `{projXml.BaseUri}` did not have an AssemblyName";
                throw new InvalidOperationException(exceptionMessage);
            }
            else
            {
                assemblyName = assemblyNameElement.Value;
            }

            return assemblyName;
        }

        private static string _GetProjectGuid(XDocument projXml)
        {
            string projectGuid = null;

            XElement projectGuidElement = projXml.Descendants(msbuildNS + "ProjectGuid").FirstOrDefault();

            if (projectGuidElement == null)
            {
                string exceptionMessage = string.Format("The Project {0} did not have an <ProjectGuid>", projXml.BaseUri);
                throw new InvalidOperationException(exceptionMessage);
            }
            else
            {
                projectGuid = projectGuidElement.Value;
            }

            return projectGuid;
        }
    }
}
