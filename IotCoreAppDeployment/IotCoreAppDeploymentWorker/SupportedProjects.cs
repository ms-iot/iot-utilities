using IotCoreAppProjectExtensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IotCoreAppDeployment
{
    class SupportedProjects
    {
        private void DoGetProviders<T>(Type providerType, List<T> providerList)
        {
            var assemblyIncludedTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => (!p.IsInterface && providerType.IsAssignableFrom(p)));
            foreach (var impl in assemblyIncludedTypes)
            {
                try
                {
                    T provider = (T)impl.GetConstructor(new Type[0]).Invoke(new Object[0]);
                    providerList.Add(provider);
                }
                catch (Exception e)
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
                    var nextAssembly = Assembly.LoadFrom(file.FullName);
                    var typesFromNextAssembly = nextAssembly.GetTypes().Where(p => (!p.IsInterface && providerType.IsAssignableFrom(p)));
                    foreach (var impl in typesFromNextAssembly)
                    {
                        try
                        {
                            T provider = (T)impl.GetConstructor(new Type[0]).Invoke(new Object[0]);
                            providerList.Add(provider);
                        }
                        catch (Exception e)
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

        private List<IProjectProvider> _ProjectProviders;
        public List<IProjectProvider> ProjectProviders
        {
            get
            {
                if (_ProjectProviders == null)
                {
                    var providerType = typeof(IProjectProvider);
                    _ProjectProviders = new List<IProjectProvider>();
                    DoGetProviders(providerType, _ProjectProviders);
                }

                return _ProjectProviders;
            }
        }
        private List<IProject> _Projects;
        public List<IProject> Projects
        {
            get
            {
                if (_Projects == null)
                {
                    _Projects = new List<IProject>();
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

        public IProject FindProject(String source)
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



        private List<ITemplateProvider> _TemplateProviders;
        public List<ITemplateProvider> TemplateProviders
        {
            get
            {
                if (_TemplateProviders == null)
                {
                    var providerType = typeof(ITemplateProvider);
                    _TemplateProviders = new List<ITemplateProvider>();
                    DoGetProviders(providerType, _TemplateProviders);
                }

                return _TemplateProviders;
            }
        }
        private List<ITemplate> _Templates;
        public List<ITemplate> Templates
        {
            get
            {
                if (_Templates == null)
                {
                    _Templates = new List<ITemplate>();
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

        public ITemplate FindTemplate(IBaseProjectTypes type)
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

        private List<IDependencyProvider> _DependencyProviders;
        public List<IDependencyProvider> DependencyProviders
        {
            get
            {
                if (_DependencyProviders == null)
                {
                    var providerType = typeof(IDependencyProvider);
                    _DependencyProviders = new List<IDependencyProvider>();
                    DoGetProviders(providerType, _DependencyProviders);
                }

                return _DependencyProviders;
            }
        }

    }
}
