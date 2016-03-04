using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.Iot.IotCoreAppDeployment
{
    class SupportedProjects
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFile")]
        private static void DoGetProviders<T>(Type providerType, Collection<T> providerList)
        {
            var assemblyIncludedTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => (!p.IsInterface && providerType.IsAssignableFrom(p)));
            foreach (var impl in assemblyIncludedTypes)
            {
                try
                {
                    var provider = (T)impl.GetConstructor(new Type[0]).Invoke(new object[0]);
                    providerList.Add(provider);
                }
                catch (Exception e) when (e is ArgumentNullException || e is ArgumentException || e is NullReferenceException)
                {
                    // TODO: handle error ... for now, skip
                }
            }

            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var di = new DirectoryInfo(assemblyLocation);
            foreach (var file in di.GetFiles("*.dll"))
            {
                try
                {
                    var nextAssembly = Assembly.LoadFile(file.FullName);
                    var typesFromNextAssembly = nextAssembly.GetTypes().Where(p => (!p.IsInterface && providerType.IsAssignableFrom(p)));
                    foreach (var impl in typesFromNextAssembly)
                    {
                        try
                        {
                            T provider = (T)impl.GetConstructor(new Type[0]).Invoke(new Object[0]);
                            providerList.Add(provider);
                        }
                        catch (ArgumentException)
                        {
                            // TODO: handle error ... for now, skip
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                    // Not a .net assembly  - ignore
                }
            }
        }

        private static Collection<IProjectProvider> _ProjectProviders;
        public static Collection<IProjectProvider> ProjectProviders
        {
            get
            {
                if (_ProjectProviders == null)
                {
                    var providerType = typeof(IProjectProvider);
                    _ProjectProviders = new Collection<IProjectProvider>();
                    DoGetProviders(providerType, _ProjectProviders);
                }

                return _ProjectProviders;
            }
        }
        private static Collection<IProject> _Projects;
        public static Collection<IProject> Projects
        {
            get
            {
                if (_Projects == null)
                {
                    _Projects = new Collection<IProject>();
                    foreach (var provider in ProjectProviders)
                    {
                        var projects = provider.GetSupportedProjects();
                        foreach (var project in projects)
                        {
                            _Projects.Add(project);
                        }
                    }
                }
                return _Projects;
            }
        }

        public static IProject FindProject(string source)
        {
            foreach (var project in Projects)
            {
                if (project.IsSourceSupported(source))
                {
                    return project;
                }
            }
            return null;
        }



        private static Collection<ITemplateProvider> _TemplateProviders;
        public static Collection<ITemplateProvider> TemplateProviders
        {
            get
            {
                if (_TemplateProviders == null)
                {
                    var providerType = typeof(ITemplateProvider);
                    _TemplateProviders = new Collection<ITemplateProvider>();
                    DoGetProviders(providerType, _TemplateProviders);
                }

                return _TemplateProviders;
            }
        }
        private static Collection<ITemplate> _Templates;
        public static Collection<ITemplate> Templates
        {
            get
            {
                if (_Templates == null)
                {
                    _Templates = new Collection<ITemplate>();
                    foreach (var provider in TemplateProviders)
                    {
                        var templates = provider.GetSupportedTemplates();
                        foreach (var template in templates)
                        {
                            _Templates.Add(template);
                        }
                    }
                }
                return _Templates;
            }
        }

        public static ITemplate FindTemplate(IBaseProjectTypes type)
        {
            foreach (var template in Templates)
            {
                if (type == template.GetBaseProjectType())
                {
                    return template;
                }
            }
            return null;
        }

        private static Collection<IDependencyProvider> _DependencyProviders;
        public static Collection<IDependencyProvider> DependencyProviders
        {
            get
            {
                if (_DependencyProviders == null)
                {
                    var providerType = typeof(IDependencyProvider);
                    _DependencyProviders = new Collection<IDependencyProvider>();
                    DoGetProviders(providerType, _DependencyProviders);
                }

                return _DependencyProviders;
            }
        }

    }
}