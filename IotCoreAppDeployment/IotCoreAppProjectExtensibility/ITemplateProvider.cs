using System.Collections.ObjectModel;

namespace Microsoft
{
    namespace Iot
    {
        namespace IotCoreAppProjectExtensibility
        {
            public interface ITemplateProvider
            {
                ReadOnlyCollection<ITemplate> GetSupportedTemplates();
            }
        }
    }
}