using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IoTAPIPortingTool
{
    class Program
    {
        private enum DllType
        {
            Windows8 = 0,
            WindowsCE = 1,
            WindowsIoT = 2,
            WindowsIoTUAP = 3,
            WindowsIoTNonUAP = 4,
        }

        private const string functionSelect = "SELECT * FROM FUNCTION WHERE F_NAME = '{0}';";
        private const string functionSelectWithDll = functionSelect + "AND F_DLL_NAME = '{1}';";
        private const string selectDll = "SELECT * FROM DLL WHERE D_NAME = '{0}'";

        private static void GenerateTables()
        {
            var insertDll = "INSERT INTO DLL VALUES('{0}', {1});";
            var insertFunction = "INSERT INTO FUNCTION (F_NAME, F_DLL_NAME) VALUES('{0}', '{1}');";

            var dbPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\apiCheck.db3";

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
                        using (var reader = new StreamReader(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\IoTDLLs.txt"))
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
                                    command.CommandText = string.Format(insertDll, dll, Convert.ToInt32(DllType.WindowsIoT));
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

        private static void ProcessLines(string[] lines, bool isUAP, string filename, StringBuilder outputBuilder)
        {
            // Queries
            var functionDLLJoin = "SELECT * FROM FUNCTION, DLL WHERE F_NAME = '{0}' AND D_NAME = F_DLL_NAME";
            var ordinal = "select * from COREDLL where API_ORDINAL = {0}";
            string apiQuery;
            if (isUAP)   // look for UAP Win32 APIs only.
            {
                apiQuery = "select * from ModernAPIs where APIName = '{0}' COLLATE NOCASE";
            }
            else
            {                   // look for O/S - ONECOREUAP APIs
                apiQuery = "select * from ONECOREUAP where API_Name = '{0}' COLLATE NOCASE";
            }

            // Output formatting
            var csvOutputFunctionSameDllFormat = filename + ",{0},{1}\n";
            var csvOutputFunctionAltDllFormat = filename + ",{0},{1},{2}\n";

            // Counts for errors/warnings
            var invalidDllCount = 0;
            var invalidFunctionCount = 0;
            var differentDllFunctionCount = 0;
            var notRecognizedDllCount = 0;

            var isCoreDLL = false;
            var isCrtDLL = false;

            var dbPath = "data source=" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\apiCheck.db3";

            using (var connection = new SQLiteConnection(dbPath))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    var index = 0;

                    // increment until we find a dll or hit the end
                    for (index = 5; index < lines.Length; index++)
                    {
                        if (lines[index].ToLower().Contains("fatal error"))
                        {
                            Console.Out.WriteLine("Unknown Error. Skipping " + filename);
                            return;
                        }

                        if (lines[index].ToLower().Trim().EndsWith(".dll"))
                        {
                            break;
                        }
                    }

                    var apiDBPath = "data source=" + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\ModernAPIs.sqlite";
                    var apiDBConnection = new SQLiteConnection(apiDBPath);
                    apiDBConnection.Open();

                    var apiDBCommand = new SQLiteCommand(apiDBConnection);

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

                        isCoreDLL = dllName.Equals("coredll.dll");
                        isCrtDLL = (dllName.StartsWith("msvc") || dllName.StartsWith("ucrt"));

                        // Try to get the dll from the db
                        command.CommandText = string.Format(selectDll, dllName);
                        var dataReader = command.ExecuteReader();
                        var isValidDll = true;
                        var isRecognized = dataReader.Read();

                        if (!isCoreDLL && !isCrtDLL)
                        {
                            if (isRecognized)
                            {
                                var dllType = (DllType)Convert.ToInt32(dataReader["D_VERSION"]);

                                // Check if the dll is in IoT
                                if (!IsValidIoTDll(dllType, isUAP))
                                {
                                    isValidDll = false;
                                    invalidDllCount++;
                                }
                            }
                            else
                            {
                                notRecognizedDllCount++;
                            }
                        }
                        else if (!isCrtDLL)
                        {
                            isValidDll = false;
                            invalidDllCount++;
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

                            var tableComponents = currentLine.Split(' ');

                            if ((!isCoreDLL && tableComponents[0] == "Ordinal") || isCrtDLL)
                            {
                                continue;
                            }

                            var functionName = tableComponents.Last();

                            if (isCoreDLL && !functionName.Equals("-1"))
                            {
                                apiDBCommand.CommandText = string.Format(ordinal, functionName);
                                dataReader = apiDBCommand.ExecuteReader();

                                if (dataReader.Read())
                                {
                                    functionName = dataReader.GetString(0);
                                }

                                dataReader.Close();
                            }

                            // If the dll exists double check that the function exists, otherwise see if there is another dll that has it
                            if (isValidDll)
                            {
                                command.CommandText = string.Format(functionSelectWithDll, functionName, dllName);
                                dataReader = command.ExecuteReader();
                                var functionExists = dataReader.Read();
                                dataReader.Close();

                                apiDBCommand.CommandText = string.Format(apiQuery, functionName);
                                dataReader = apiDBCommand.ExecuteReader();

                                functionExists &= dataReader.Read();

                                dataReader.Close();

                                if (!functionExists)
                                {
                                    invalidFunctionCount++;
                                    outputBuilder.AppendFormat(csvOutputFunctionSameDllFormat, dllName, functionName, functionExists);
                                }
                            }
                            else
                            {
                                command.CommandText = string.Format(functionDLLJoin, functionName);
                                dataReader = command.ExecuteReader();
                                var functionExistsInAltDll = dataReader.Read();

                                var altDll = functionExistsInAltDll ? dataReader["D_NAME"] : null;

                                dataReader.Close();

                                apiDBCommand.CommandText = string.Format(apiQuery, functionName);
                                dataReader = apiDBCommand.ExecuteReader();
                                bool functionExists = dataReader.Read();

                                dataReader.Close();

                                if (functionExists && functionExistsInAltDll)
                                {
                                    outputBuilder.AppendFormat(csvOutputFunctionAltDllFormat, dllName, functionName, altDll);
                                    differentDllFunctionCount++;
                                }
                                else if (functionExists)
                                {
                                    continue;
                                }
                                else
                                {
                                    invalidFunctionCount++;
                                    outputBuilder.AppendFormat(csvOutputFunctionSameDllFormat, dllName, functionName, functionExists);
                                }
                            }
                        }
                    }

                    apiDBConnection.Close();
                }
            }

            // Summary output
            Console.Out.WriteLine(string.Format("{0}{1}Summary for {2}", Environment.NewLine, Environment.NewLine, filename));
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
                Console.Out.WriteLine(string.Format("{0}{1}Compatible with Windows IoT Core!", Environment.NewLine, Environment.NewLine));
            }

            Console.Out.WriteLine();
        }

        private static bool IsValidIoTDll(DllType dllType, bool isUAP)
        {
            return (dllType == DllType.WindowsIoT || (isUAP && dllType == DllType.WindowsIoTUAP) || (!isUAP && dllType == DllType.WindowsIoTNonUAP));
        }

        private static void InvalidUsage()
        {
            Console.Out.WriteLine("Usage: IoTAPIPortingTool.exe <win32 Binary> [-os]");
            Console.Out.WriteLine("Note: adding [-os] as a option will scan the underlying o/s APIs");
            Console.Out.WriteLine("The default option is to scan for Win32 UAP supported APIs");

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
                if (!args[1].ToLower().Equals("-os"))
                {
                    InvalidUsage();
                } 
                else 
                {
                    isUAP = false;
                }
            }

            CheckDeveloperPrompt();

            var path = Path.GetDirectoryName(args[0]);

            if (path.Length == 0)
            {
                path = @".\";
            }

            var filePattern = Path.GetFileName(args[0]);
            var files = Directory.GetFiles(path, filePattern);

            if (files.Count() == 0)
            {
                Console.Out.WriteLine(string.Format("No files found that match '{0}'", args[0]));
                InvalidUsage();
            }

            var outputFile = isUAP ? "IoTAPIPortingTool.csv" : "IoTAPIPortingToolOS.csv";
            var outputBuilder = new StringBuilder("INPUT FILE,DLL NAME,FUNCTION NAME,ALTERNATE DLL\n\n");

            foreach (var file in files)
            {
                if (!file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var trimmedFilename = file.Substring((file.IndexOf('\\') + 1));

                Console.Out.WriteLine();
                Console.Out.WriteLine("Parsing " + trimmedFilename);
                var lines = GetDumpbinOutput(file);
                ProcessLines(lines, isUAP, trimmedFilename, outputBuilder);
            }

            try
            {
                File.WriteAllText(outputFile, outputBuilder.ToString());
            }
            catch (Exception)
            {
                Console.Out.WriteLine(string.Format("***Please close the {0} file to obtain your detailed results***", outputFile));
            }
        }
    }
}
