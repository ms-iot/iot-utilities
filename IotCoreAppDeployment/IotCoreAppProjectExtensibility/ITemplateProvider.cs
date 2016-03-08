using System.Collections.ObjectModel;

namespace Microsoft.Iot.IotCoreAppProjectExtensibility
{
    public interface ITemplateProvider
    {
        ReadOnlyCollection<ITemplate> GetSupportedTemplates();
    }
}