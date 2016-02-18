using IotCoreAppProjectExtensibility;
using System.Collections.Generic;

namespace IotCoreTemplateProvider
{
    public class TemplateProvider : ITemplateProvider
    {
        public List<ITemplate> GetSupportedTemplates()
        {
            var templates = new List<ITemplate>();
            templates.Add(new CppBackgroundApplicationTemplate());
            return templates;
        }
    }
}
