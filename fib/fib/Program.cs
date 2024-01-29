using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;

//commands

var rootCommand = new RootCommand("RootCommand for file bundle cli");

var bundleCommand = new Command("bundle", "Bundle code files to a single file");

var createRspCommand = new Command("create-rsp", "Create a response file for the bundle command");


//options

var outputOption = new Option<FileInfo>(new[] { "--output", "-o" }, "File path and name") { IsRequired = true };

var languageOption = new Option<string>(new[] { "--language", "-l" }, "Programming language to include in the bundle") { IsRequired = true };

var noteOption = new Option<bool>(new[] { "--note", "-n" }, "Include source code note in the bundle");

var sortOption = new Option<string>(new[] { "--sort", "-s" }, "Sorting order (name or type)");

var removeEmptyLinesOption = new Option<bool>(new[] { "--remove-empty-lines", "-r" }, "Remove empty lines from code files");

var authorOption = new Option<string>(new[] { "--author", "-a" }, "Name of the file creator");


bundleCommand.AddOption(outputOption);

bundleCommand.AddOption(languageOption);

bundleCommand.AddOption(noteOption);

bundleCommand.AddOption(sortOption);

bundleCommand.AddOption(removeEmptyLinesOption);

bundleCommand.AddOption(authorOption);


static string RemoveEmptyLinesFunc(string code)
{
    string[] lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    List<string> nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
    return string.Join(Environment.NewLine, nonEmptyLines);
}


//הפונקציה שאני אריץ ואני אראה את הפקודה 
bundleCommand.SetHandler((output, language, note, sort, author) =>
{

    if (string.IsNullOrEmpty(sort))
    {
        sort = "name";
    }
    try
    {
        DirectoryInfo directory = new DirectoryInfo(".");
        FileInfo[] files = directory.GetFiles("*." + language); // Get files with the specified language extension

        var excludedDirectories = new[] { "bin", "debug" }; 
        files = files.Where(file => !excludedDirectories.Any(dir => file.FullName.StartsWith(Path.Combine(directory.FullName, dir)))).ToArray();


        //language
        if (language.ToLowerInvariant() == "all")
        {
            files = directory.GetFiles();
        }
        else
        {
            files = directory.GetFiles("*." + language);
        }

        //sort
        if (sort.ToLowerInvariant() == "type")
        {
            Array.Sort(files, (f1, f2) => string.Compare(Path.GetExtension(f1.Name), Path.GetExtension(f2.Name), StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            Array.Sort(files, (f1, f2) => string.Compare(f1.Name, f2.Name, StringComparison.OrdinalIgnoreCase));
        }

        using (StreamWriter writer = File.CreateText(output.FullName)) // Create a new file instead of appending
        {
            if (!string.IsNullOrEmpty(author))
            {
                writer.WriteLine($"# Author: {author}");
            }
            foreach (var file in files)
            {
                string code = File.ReadAllText(file.FullName);
                if (bundleCommand.Parse(args).HasOption(removeEmptyLinesOption))
                {
                    code = RemoveEmptyLinesFunc(code);
                }

                if (note) // Check for note option and value
                {
                    writer.WriteLine(output.FullName);
                }
                writer.WriteLine(code);
            }
        }

        Console.WriteLine($"File '{output.Name}' was created");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error: File path is invalid");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    }
}, outputOption, languageOption, noteOption, sortOption, authorOption);
createRspCommand.SetHandler(() =>
{
    var responseFile = new FileInfo("responseFile.rsp");

    try
    {
        using (StreamWriter rspWriter = new StreamWriter(responseFile.FullName))
        {
            rspWriter.Write(GetUserInput("--output", "Enter File output name"));

            rspWriter.Write(GetUserInput("--language", "Enter programming language or to include every language enter all"));

            rspWriter.Write(GetUserInput("--note", "Include source code origin as a comment? (yes/no)").ToLower() == "yes" ? "--note " : "");

            rspWriter.Write(GetUserInput("--sort", "Enter the sort order for code files ('name' or 'type'):"));

            rspWriter.Write(GetUserInput("--remove-empty-lines", "Remove empty lines from code files? (yes/no)").ToLower() == "yes" ? "--remove-empty-lines " : "");

            rspWriter.Write(GetUserInput("--author", "Enter the author`s name:"));
        }

        Console.WriteLine($"Response file created successfully: {responseFile.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating response file: {ex.Message}");
    }
});

string GetUserInput(string option, string prompt)
{
    Console.WriteLine(prompt);
    string input;
    do
    {
        input = Console.ReadLine();
    } while (string.IsNullOrEmpty(input));

    return $"{option} {input} ";
}

rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
// בשביל שזה עוטף את הmain
rootCommand.InvokeAsync(args);
