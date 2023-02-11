// Copyright 2023 Gamergenic.  See full copyright notice in AutoVersionNumbersModule.h.
// Author: chucknoble@gamergenic.com | https://www.gamergenic.com
// 
// Project page:   https://gamedevtricks.com/post/automagically-updating-ue-app-version/
// GitHub:         https://github.com/Gamergenic/AutoVersionNumbers/


using UnrealBuildTool;
using System.Collections.Generic;

public class AutoVersionNumbersEditorTarget : TargetRules
{
	public AutoVersionNumbersEditorTarget( TargetInfo Target) : base(Target)
	{
		Type = TargetType.Editor;
		DefaultBuildSettings = BuildSettingsVersion.V2;
		IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_1;
		ExtraModuleNames.Add("AutoVersionNumbers");

        // Prebuild step to increment version #.
        if (Target.Platform == UnrealTargetPlatform.Win64)
        {
            PreBuildSteps.Add("$(ProjectDir)\\Automation\\PreBuildSteps.bat \"$(EngineDir)\\Binaries\\Win64\\UnrealEditor.exe\" \"$(ProjectDir)\"");
        }
    }
}
