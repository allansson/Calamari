﻿using System.IO;
using Calamari.Commands.Support;
using Calamari.Deployment;
using Calamari.Integration.FileSystem;
using Calamari.Integration.Processes;
using Calamari.Integration.Scripting;
using Calamari.Integration.ServiceMessages;
using Octostache;

namespace Calamari.Commands
{
    [Command("run-script", Description = "Invokes a PowerShell or ScriptCS script")]
    public class RunScriptCommand : Command
    {
        private string variablesFile;
        private string outputVariablesFile;
        private string scriptFile;
        private string sensitiveVariablesFile;
        private string sensitiveVariablesPassword;

        public RunScriptCommand()
        {
            Options.Add("variables=", "Path to a JSON file containing variables.", v => variablesFile = Path.GetFullPath(v));
            Options.Add("outputVariables=", "Path to JSON file where output variables are stored.", v => outputVariablesFile = Path.GetFullPath(v));
            Options.Add("script=", "Path to the script (PowerShell or ScriptCS) script to execute.", v => scriptFile = Path.GetFullPath(v));
            Options.Add("sensitiveVariables=", "Password protected JSON file containing sensitive-variables.", v => sensitiveVariablesFile = v);
            Options.Add("sensitiveVariablesPassword=", "Password used to decrypt sensitive-variables.", v => sensitiveVariablesPassword = v);
        }

        public override int Execute(string[] commandLineArguments)
        {
            Options.Parse(commandLineArguments);

            var variables = new CalamariVariableDictionary(variablesFile, sensitiveVariablesFile, sensitiveVariablesPassword);
            var outputVariables = new VariableDictionary(outputVariablesFile);

            variables.MergeWith(outputVariables);
            variables.EnrichWithEnvironmentVariables();
            variables.LogVariables();

            return InvokeScript(variables, outputVariables);
        }
        
        private int InvokeScript(CalamariVariableDictionary variables, VariableDictionary outputVariables)
        {
            if (!File.Exists(scriptFile))
                throw new CommandException("Could not find script file: " + scriptFile);

            var scriptEngine = new CombinedScriptEngine();
            var runner = new CommandLineRunner(
                new SplitCommandOutput(new ConsoleCommandOutput(), new ServiceMessageCommandOutput(variables), new ServiceMessageCommandOutput(outputVariables)));
            var result = scriptEngine.Execute(scriptFile, variables, runner);

            outputVariables.Save();

            return result.ExitCode;
        }
    }
}
