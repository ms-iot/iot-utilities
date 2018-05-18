//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System.Xml.Linq;

namespace SystemFunctionalTest
{
    public class MenuItemConfigBase
    {
        #region Config: Main Context
        public const bool DEFAULT_AllowSimulation = false;
        public const uint DEFAULT_AutoFailTimeout = 0; // infinite
        public bool AllowSimulation { get; set; }
        public uint AutoFailTimeout { get; set; }

        public MenuItemConfigBase()
        {
            this.Reset();
        }

        public void Reset()
        { 
            AllowSimulation = DEFAULT_AllowSimulation; // default
            AutoFailTimeout = DEFAULT_AutoFailTimeout;
        }
        #endregion // Config: Main Context

        #region Config: Basic Defines
        // <MenuItem Name="Pen" 
        //         AllowSimulation="False" 
        //         AutoFailTimeout="0"
        // > 
        // </MenuItem>
        public static void LoadFromXml(XElement elMenu, MenuItemConfigBase baseConfig)
        {
            if (elMenu == null || baseConfig == null) return;

            foreach (XAttribute attr in elMenu.Attributes())
            {
                if (attr.Name.LocalName == "AllowSimulation")
                {
                    if (attr.Value != null && attr.Value.ToLower() == "true")
                    {
                        baseConfig.AllowSimulation = true;
                    }
                }
                else if (attr.Name.LocalName == "AutoFailTimeout")
                {
                    uint timeoutValue = 0;
                    if (attr.Value != null && uint.TryParse(attr.Value, out timeoutValue))
                    {
                        baseConfig.AutoFailTimeout = timeoutValue;
                    }
                }
            }
        }
        #endregion // Config: Basic Defines
    }
}