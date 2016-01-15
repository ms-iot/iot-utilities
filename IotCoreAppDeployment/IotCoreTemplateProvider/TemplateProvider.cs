using IotCoreAppProjectExtensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
