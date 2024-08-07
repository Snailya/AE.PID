﻿using System;
using System.IO;

namespace AE.PID.Tools;

internal abstract class Constants
{
    public const string FrameBaseId = "{7811D65E-9633-4E98-9FCD-B496A8B823A7}";
    public const string FunctionalElementBaseId = "{B28A5C75-E7CB-4700-A060-1A6D0A777A94}";

    public static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AE\\PID");

    public static readonly string LibraryFolder = Path.Combine(AppDataFolder, "Libraries");

    public static readonly string LibraryCheatSheetPath = Path.Combine(LibraryFolder, ".cheatsheet");

    public static readonly string TmpFolder = Path.Combine(AppDataFolder, "Tmp");
    public static readonly string ValidationLayerName = "Validation";
}