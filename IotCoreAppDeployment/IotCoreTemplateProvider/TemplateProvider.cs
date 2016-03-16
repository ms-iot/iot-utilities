// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Iot.IotCoreAppProjectExtensibility;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Iot.IotCoreTemplateProvider
{
    public class TemplateProvider : ITemplateProvider
    {
        public ReadOnlyCollection<ITemplate> GetSupportedTemplates()
        {
            var templates = new List<ITemplate>() { new CppBackgroundApplicationTemplate() };
            return new ReadOnlyCollection<ITemplate>(templates);
        }
    }
}
