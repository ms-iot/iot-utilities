using System.Collections.Generic;

namespace IotCoreAppProjectExtensibility
{
    public interface ITemplateProvider
    {
        List<ITemplate> GetSupportedTemplates();
    }
}
