// Copyright 2023 Gamergenic.  See full copyright notice in AutoVersionNumbersModule.h.
// Author: chucknoble@gamergenic.com | https://www.gamergenic.com
// 
// Project page:   https://gamedevtricks.com/post/automagically-updating-ue-app-version/
// GitHub:         https://github.com/Gamergenic/AutoVersionNumbers/


using UnrealBuildTool;
using System.Collections.Generic;

public class AutoVersionNumbersTarget : TargetRules
{
	public AutoVersionNumbersTarget( TargetInfo Target) : base(Target)
	{
		Type = TargetType.Game;
		DefaultBuildSettings = BuildSettingsVersion.V2;
		IncludeOrderVersion = EngineIncludeOrderVersion.Unreal5_1;
		ExtraModuleNames.Add("AutoVersionNumbers");
    }
}
