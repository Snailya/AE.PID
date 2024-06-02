using System.CommandLine;
using System.IO.Packaging;

namespace PID.DocumentStencilUpdateTool;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(
                "--file",
                description: "The file to update document stencil",
                parseArgument: result =>
                {
                    var filePath = result.Tokens.Single().Value;

                    if (!File.Exists(filePath))
                    {
                        result.ErrorMessage = "File does not exist";
                        return null;
                    }

                    if (Path.GetExtension(filePath) != ".vsdx")
                    {
                        result.ErrorMessage =
                            "Only a valid Visio document with extension vsdx could be updated using this tool.";
                        return null;
                    }

                    return new FileInfo(filePath);
                })
            { IsRequired = true, AllowMultipleArgumentsPerToken = false };

        var referenceOption = new Option<FileInfo>(
            "--reference",
            description: "The reference file that used as the update target.",
            parseArgument: result =>
            {
                var filePath = result.Tokens.Single().Value;

                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "File does not exist";
                    return null;
                }

                var extension = Path.GetExtension(filePath);

                if (extension is ".vssx" or ".cheatsheet") return new FileInfo(filePath);

                result.ErrorMessage =
                    "Only a valid Visio stencil document with extension vssx or a cheatsheet file could be used as a reference by this tool.";
                return null;
            });


        var rootCommand = new RootCommand("Sample app for System.CommandLine");
        rootCommand.AddOption(fileOption);
        rootCommand.AddOption(referenceOption);

        rootCommand.SetHandler(file => { Update(file!); }, fileOption);
        rootCommand.SetHandler((file, reference) => { Update(file!, reference!); },
            fileOption, referenceOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static void Update(FileInfo file, FileInfo? reference = null)
    {
        // create a backup file
        UpdateHelper.CreateBackup(file);

        using var package = Package.Open(file.FullName, FileMode.Open, FileAccess.ReadWrite);
        // when user using a context menu to set up the subclass property, the subclass property value is a string,
        // which will lost if the subclass format changed,
        // therefore, replace this string value with a formula basing the index
        UpdateHelper.SupplementSubClassFormula(package);

        // though the masters are set to match name on dropping,
        // it still could not restrict user to use the unique master.
        // by checking the BaseID in the masters, replace the shapes to point to one single master
        UpdateHelper.ReplaceDuplicateMasters(package);

        // replace the master and contents
        var refMasters = reference == null ? UpdateHelper.LoadReferenceFromServer().GetAwaiter().GetResult() :
            reference.Extension == "vssx" ? UpdateHelper.LoadReferenceFromDocument(reference) :
            UpdateHelper.LoadReferenceFromPath(reference);
        UpdateHelper.ReplaceMasterElementAndMasterContent(package, refMasters);

        Console.WriteLine("Update done without error.");
    }
}