using System;
using System.IO;

namespace AE.PID;

public abstract class Constants
{
    public static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AE\\PID");

    public static readonly string LibraryFolder = Path.Combine(AppDataFolder, "Libraries");

    public static readonly string LibraryCheatSheetPath = Path.Combine(LibraryFolder, ".cheatsheet");

    public static readonly string TmpFolder = Path.Combine(AppDataFolder, "Tmp");
}