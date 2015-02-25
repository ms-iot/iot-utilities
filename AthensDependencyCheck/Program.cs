using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AthensDependencyCheck
{
    class Program
    {
        private enum DllType
        {
            Windows8 = 0,
            WindowsCE = 1,
            WindowsAthens = 2,
            WindowsAthensUAP = 3,
            WindowsAthensNonUAP = 4,
        }

        private static DllType StringToDllType(string dllType)
        {
            switch (dllType)
            {
                case "8":
                    return DllType.Windows8;
                case "CE":
                    return DllType.WindowsCE;
                case "Athens":
                    return DllType.WindowsAthens;
                case "UAP":
                    return DllType.WindowsAthensUAP;
                case "NonUAP":
                    return DllType.WindowsAthensNonUAP;
                default:
                    throw new ArgumentException("Invalid DLL Type");
            }
        }

        private const string functionSelect = "SELECT * FROM FUNCTION WHERE F_NAME = '{0}';";
        private const string functionSelectWithDll = functionSelect + "AND F_DLL_NAME = '{1}';";
        private const string selectDll = "SELECT * FROM DLL WHERE D_NAME = '{0}'";

        private static void GenerateTables()
        {
            var insertDll = "INSERT INTO DLL VALUES('{0}', {1});";
            var insertFunction = "INSERT INTO FUNCTION (F_NAME, F_DLL_NAME) VALUES('{0}', '{1}');";

            var dbPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\athensCheck.db3";

            SQLiteConnection.CreateFile(dbPath);
            using (var connection = new SQLiteConnection("data source=" + dbPath))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    // Create the DLL Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS DLL 
                                            (
                                                D_NAME VARCHAR(255) NOT NULL PRIMARY KEY,
                                                D_VERSION INTEGER NOT NULL
                                            );";
                    command.ExecuteNonQuery();

                    // Create the Function Table
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS FUNCTION 
                                            (
                                                F_ID INTEGER PRIMARY KEY,
                                                F_NAME VARCHAR(255) NOT NULL,
                                                F_DLL_NAME INTEGER NOT NULL,
                                                FOREIGN KEY(F_DLL_NAME) REFERENCES DLL(D_NAME)
                                            );";
                    command.ExecuteNonQuery();

                    // Ensure we wiped the old tables
                    command.CommandText = "DELETE FROM DLL;";
                    command.ExecuteNonQuery();
                    command.CommandText = "DELETE FROM FUNCTION;";
                    command.ExecuteNonQuery();

                    try
                    {
                        using (var reader = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\AthensDLLs.txt"))
                        {
                            var lastDll = string.Empty;

                            while (!reader.EndOfStream)
                            {
                                var parts = reader.ReadLine().Trim().Split(',');
                                var dll = parts[0].ToLower();
                                var function = parts[2];

                                command.CommandText = string.Format(functionSelectWithDll, function, dll);
                                using (var dataReader = command.ExecuteReader())
                                {
                                    if (dataReader.Read())
                                    {
                                        continue;
                                    }
                                }
 
                                command.CommandText = string.Format(selectDll, dll);
                                var exists = false;

                                using (var dataReader = command.ExecuteReader())
                                {
                                    exists = dataReader.Read();
                                }

                                if (!exists)
                                {
                                    command.CommandText = string.Format(insertDll, dll, Convert.ToInt32(DllType.WindowsAthens));
                                    command.ExecuteNonQuery();
                                }

                                command.CommandText = string.Format(insertFunction, function, dll);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Read in the known Windows 8 dlls
                        using (var reader = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Win8DLLs.txt"))
                        {
                            while (!reader.EndOfStream)
                            {
                                var dllName = reader.ReadLine().ToLower().Trim();

                                command.CommandText = string.Format(selectDll, dllName);
                                using (var dataReader = command.ExecuteReader())
                                {
                                    if (dataReader.Read())
                                    {
                                        continue;
                                    }
                                }

                                command.CommandText = string.Format(insertDll, dllName, Convert.ToInt32(DllType.Windows8));
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (IOException)
                    {
                        Console.Error.WriteLine("Error Reading Input File");
                        Environment.Exit(1);
                    }
                }
            }
        }

        private static void CheckDeveloperPrompt()
        {
            var haveDeveloperPrompt = false;
            var path = Environment.GetEnvironmentVariable("PATH");
            var paths = path.Split(';');

            // Parse the PATH for dumpbin
            if (paths.Count() > 0)
            {
                foreach (var s in paths)
                {
                    var pth = Path.Combine(s, "dumpbin.exe");
                    if (File.Exists(pth))
                    {
                        haveDeveloperPrompt = true;
                        break;
                    }
                }
            }

            if (!haveDeveloperPrompt)
            {
                Console.WriteLine("\nPlease launch from a developer command prompt\n");
                Console.WriteLine("I can't find Dumpbin.exe on the current path\n");
                Environment.Exit(1);
            }
        }

        private static string[] GetDumpbinOutput(string target)
        {
            var dumpbin = new Process();
            dumpbin.StartInfo.FileName = "dumpbin.exe";
            dumpbin.StartInfo.Arguments = "/imports \"" + target + '"';
            dumpbin.StartInfo.UseShellExecute = false;
            dumpbin.StartInfo.RedirectStandardOutput = true;
            dumpbin.Start();

            var console = dumpbin.StandardOutput.ReadToEnd();
            var lines = console.Split('\n');

            dumpbin.WaitForExit();
            return lines;
        }

        private static void ProcessLines(string[] lines, bool isUAP)
        {
            // Queries
            var functionDLLJoin = "SELECT * FROM FUNCTION, DLL WHERE F_NAME = '{0}' AND D_NAME = F_DLL_NAME";

            // Output formatting
            var csvOutputFunctionSameDllFormat = "{0},{1}\n";
            var csvOutputFunctionAltDllFormat = "{0},{1},{2}\n";

            // Counts for errors/warnings
            var invalidDllCount = 0;
            var invalidFunctionCount = 0;
            var differentDllFunctionCount = 0;
            var notRecognizedDllCount = 0;

            var dbPath = "data source=" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\athensCheck.db3";

            using (var connection = new SQLiteConnection(dbPath))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    var index = 0;

                    // increment until we find a dll or hit the end
                    for (index = 5; index < lines.Length; index++)
                    {
                        if (lines[index].ToLower().Trim().EndsWith(".dll"))
                        {
                            break;
                        }
                    }

                    var csvOutput = new StringBuilder("DLL NAME, FUNCTION NAME, ALTERNATE DLL\n\n");

                    // Parse the dlls
                    while (true)
                    {
                        var currentLine = lines[index].ToLower().Trim();

                        // We hit a different section of imports, skip to the dlls
                        if (currentLine.StartsWith("section"))
                        {
                            index += 2;
                            currentLine = lines[index].ToLower().Trim();
                        }

                        // If we run out of dlls we are done
                        if (!currentLine.EndsWith(".dll"))
                        {
                            break;
                        }

                        var dllName = currentLine;

                        // Try to get the dll from the db
                        command.CommandText = string.Format(selectDll, dllName);
                        var dataReader = command.ExecuteReader();
                        var isValidDll = true;
                        var isRecognized = dataReader.Read();

                        if (isRecognized)
                        {
                            var dllType = (DllType)Convert.ToInt32(dataReader["D_VERSION"]);

                            // Check if the dll is in Athens
                            if (!IsValidAthensDll(dllType, isUAP))
                            {
                                isValidDll = false;
                                invalidDllCount++;
                            }
                        }
                        else
                        {
                            csvOutput.AppendFormat(csvOutputFunctionSameDllFormat, dllName, "Unknown DLL");
                            notRecognizedDllCount++;
                        }

                        dataReader.Close();

                        // Loop through the preamble (of variable length)
                        while (!string.IsNullOrWhiteSpace(currentLine))
                        {
                            index++;
                            currentLine = lines[index].ToLower().Trim();
                        }

                        // Parse each function inside the dll
                        while (true)
                        {
                            index++;
                            currentLine = lines[index].Trim();

                            // If we hit a line of whitespace move onto the next dll
                            if (string.IsNullOrWhiteSpace(currentLine))
                            {
                                index++;
                                break;
                            }

                            // Skip the function is the DLL is not known to us
                            if (!isRecognized)
                            {
                                continue;
                            }

                            var tableComponents = currentLine.Split(' ');

                            if (tableComponents[0] == "Ordinal")
                            {
                                continue;
                            }

                            var functionName = tableComponents.Last();

                            // If the dll exists double check that the function exists, otherwise see if there is another dll that has it
                            if (isValidDll)
                            {
                                command.CommandText = string.Format(functionSelectWithDll, functionName, dllName);
                                dataReader = command.ExecuteReader();
                                var functionExists = dataReader.Read();

                                if (!functionExists)
                                {
                                    invalidFunctionCount++;
                                    csvOutput.AppendFormat(csvOutputFunctionSameDllFormat, dllName, functionName, functionExists);
                                }

                                dataReader.Close();
                            }
                            else
                            {
                                command.CommandText = string.Format(functionDLLJoin, functionName);
                                dataReader = command.ExecuteReader();
                                var functionExists = dataReader.Read();

                                // Ensure that the dll is UAP/Non-UAP compatible and that the function exists

                                if (functionExists && IsValidAthensDll((DllType)Convert.ToInt32(dataReader["D_VERSION"]), isUAP))
                                {
                                    csvOutput.AppendFormat(csvOutputFunctionAltDllFormat, dllName, functionName, dataReader["D_NAME"]);
                                    differentDllFunctionCount++;
                                }
                                else
                                {
                                    invalidFunctionCount++;
                                    csvOutput.AppendFormat(csvOutputFunctionSameDllFormat, dllName, functionName, functionExists);
                                }

                                dataReader.Close();
                            }
                        }
                    }

                    File.WriteAllText("result.csv", csvOutput.ToString());
                }
            }

            // Summary output
            Console.Out.WriteLine(string.Format("{0}{1}Summary", Environment.NewLine, Environment.NewLine));
            Console.Out.WriteLine(invalidDllCount == 0 ? "All DLLs are compatible" : "Number of DLLs incompatible: " + invalidDllCount);
            Console.Out.WriteLine(invalidFunctionCount == 0 ? "All functions are comptible" : "Number of functions incompatible: " + invalidFunctionCount);

            if (differentDllFunctionCount > 0) {
                Console.Out.WriteLine("Number of functions found in different DLLs: " + differentDllFunctionCount);
            }

            if (notRecognizedDllCount > 0)
            {
                Console.Out.WriteLine("Number of unrecognized DLLs: " + notRecognizedDllCount);
            }

            if (invalidDllCount + invalidFunctionCount + differentDllFunctionCount == 0)
            {
                Console.Out.WriteLine(string.Format("{0}{1}Your DLL is compatible with Windows Athens!", Environment.NewLine, Environment.NewLine));
            }
        }

        private static bool IsValidAthensDll(DllType dllType, bool isUAP)
        {
            return (dllType == DllType.WindowsAthens || (isUAP && dllType == DllType.WindowsAthensUAP) || (!isUAP && dllType == DllType.WindowsAthensNonUAP));
        }

        private static void InvalidUsage()
        {
            Console.Error.WriteLine("Usage: AthensDependencyCheck.exe (generate | [dllName] [-u])");
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                InvalidUsage();
            }

            // Assumption no input file name is just "generate" (Could be a bad one)
            if (args.Length == 1 && args[0].Equals("generate"))
            {
                GenerateTables();
                return;
            }

            var isUAP = true;

            // Check for a Non-UAP flag
            if (args.Length == 2) {
                if (!args[1].Equals("-u"))
                {
                    InvalidUsage();
                } 
                else 
                {
                    isUAP = false;
                }
            }

            CheckDeveloperPrompt();
            var lines = GetDumpbinOutput(args[0]);
            ProcessLines(lines, isUAP);
        }
    }
}
