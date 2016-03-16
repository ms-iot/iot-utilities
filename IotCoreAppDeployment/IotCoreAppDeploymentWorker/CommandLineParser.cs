// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Iot.IotCoreAppDeployment
{
    public class CommandLineParser
    {
        public class ArgumentHelper
        {
            public string MatchString { get; private set; }
            public Regex ArgumentMatcher { get; private set; }
            public Action<DeploymentWorker, string> Handler { get; set; }
            public string HelpString { get; private set; }
            public bool ConsumesNextArg { get; private set; }
            public bool Required { get; private set; }
            public bool IsHelp { get; set; }

            public ArgumentHelper(string argName, string helpMsg, bool consumesNextArg, bool required)
            {
                Init(new string[] { argName }, helpMsg, consumesNextArg, required);
            }
            public ArgumentHelper(string[] argNames, string helpMsg, bool consumesNextArg, bool required)
            {
                Init(argNames, helpMsg, consumesNextArg, required);
            }
            private void Init(string[] argNames, string helpMsg, bool consumesNextArg, bool required)
            {
                var args = new StringBuilder();
                var argMatcher = new StringBuilder();
                for (var i = 0; i < argNames.Length; i++)
                {
                    if (i != 0)
                    {
                        args.Append("|");
                        argMatcher.Append("|");
                    }
                    args.Append("-");
                    args.Append(argNames[i]);
                    argMatcher.Append(@"(^[-/]");
                    argMatcher.Append(argNames[i]);
                    argMatcher.Append("$)");
                }
                MatchString = args.ToString();
                ArgumentMatcher = new Regex(argMatcher.ToString(), RegexOptions.IgnoreCase);
                HelpString = helpMsg;
                ConsumesNextArg = consumesNextArg;
                Required = required;
            }
        }

        private readonly List<ArgumentHelper> Arguments;
        private readonly DeploymentWorker DeploymentWorker;

        public CommandLineParser(DeploymentWorker worker)
        {
            DeploymentWorker = worker;
            Arguments = new List<ArgumentHelper>();
        }

        public void AddRequiredArgumentWithInput(string argName, string helpMsg, Action<DeploymentWorker, string> handler)
        {
            AddArgument(new string[] { argName }, helpMsg, true, true, false, handler);
        }
        public void AddOptionalArgumentWithInput(string argName, string helpMsg, Action<DeploymentWorker, string> handler)
        {
            AddArgument(new string[] { argName }, helpMsg, true, false, false, handler);
        }
        public void AddOptionalArgumentWithoutInput(string argName, string helpMsg, Action<DeploymentWorker, string> handler)
        {
            AddArgument(new string[] { argName }, helpMsg, false, false, false, handler);
        }
        public void AddHelpArgument(string [] argNames, string helpMsg)
        {
            AddArgument(argNames, helpMsg, false, false, true, null);
        }
        private void AddArgument(string[] argNames, string helpMsg, bool consumesNextArg, bool required, bool isHelp, Action<DeploymentWorker, string> handler)
        {
            Arguments.Add(new ArgumentHelper(argNames, helpMsg, consumesNextArg, required)
            {
                Handler = handler,
                IsHelp = isHelp,
            });
        }

        private void OutputHelpMessage(ArgumentHelper option, bool required, bool input)
        {
            if (option.Required == required && option.ConsumesNextArg == input)
            {
                DeploymentWorker.OutputMessage(string.Format(CultureInfo.InvariantCulture, "     {0, -20}{1}", option.MatchString, option.HelpString));
            }
        }

        private void OutputHelpMessage()
        {
            var sortedOptions = new ArgumentHelper[Arguments.Count];
            Arguments.CopyTo(sortedOptions);
            Array.Sort(sortedOptions, (a, b) => string.CompareOrdinal(a.MatchString, b.MatchString));

            DeploymentWorker.OutputMessage("");
            DeploymentWorker.OutputMessage(string.Format(CultureInfo.InvariantCulture, "  {0} -s (source) -n (target):", "IotCoreAppDeployment.exe"));
            DeploymentWorker.OutputMessage("");
            DeploymentWorker.OutputMessage(Resource.CommandLineParser_RequiredArgWithInput);
            foreach (var option in sortedOptions)
            {
                OutputHelpMessage(option, true, true);
            }
            DeploymentWorker.OutputMessage("");
            DeploymentWorker.OutputMessage(Resource.CommandLineParser_OptionalArgWithInput);
            foreach (var option in sortedOptions)
            {
                OutputHelpMessage(option, false, true);
            }
            DeploymentWorker.OutputMessage("");
            DeploymentWorker.OutputMessage(Resource.CommandLineParser_OptionalArgNoInput);
            foreach (var option in sortedOptions)
            {
                OutputHelpMessage(option, false, false);
            }
            DeploymentWorker.OutputMessage("");
        }

        public bool HandleCommandLineArgs(string[] args)
        {
            var doUsage = true;

            if (args.Length > 1)
            {
                doUsage = false;

                for (var current = 0; current < args.Length; current++)
                {
                    var handled = false;
                    var arg = args[current];
                    foreach (var helper in Arguments)
                    {
                        if (!helper.ArgumentMatcher.IsMatch(arg))
                        {
                            continue;
                        }

                        handled = true;
                        if (helper.IsHelp)
                        {
                            doUsage = true;
                            break;
                        }
                        string handlerValue = null;
                        if (helper.ConsumesNextArg)
                        {
                            if (current + 1 < args.Length)
                            {
                                handlerValue = args[current + 1];
                            }

                            if (handlerValue == null || handlerValue.StartsWith("-", StringComparison.OrdinalIgnoreCase) || handlerValue.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                            {
                                // Value must be specified for this argument
                                DeploymentWorker.OutputMessage(string.Format(CultureInfo.InvariantCulture, Resource.CommandLineParser_ArgRequiresInput, helper.MatchString));
                                doUsage = true;
                                break;
                            }

                            current++;
                        }

                        try
                        {
                            helper.Handler(DeploymentWorker, handlerValue);
                        }
                        catch (ArgumentException e)
                        {
                            DeploymentWorker.OutputMessage(e.Message);
                            doUsage = true;
                        }
                        break;
                    }

                    if (!handled)
                    {
                        // Value must be specified for this argument
                        DeploymentWorker.OutputMessage(string.Format(CultureInfo.InvariantCulture, Resource.CommandLineParser_UnrecognizedArgument, arg));
                        doUsage = true;
                        break;
                    }
                }
            }

            if (doUsage)
            {
                OutputHelpMessage();
                return false;
            }

            return true;
        }
    }
}
