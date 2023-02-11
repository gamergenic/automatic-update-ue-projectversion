// Copyright 2023 Gamergenic.  See full copyright notice in AutoVersionNumbersModule.h.
// Author: chucknoble@gamergenic.com | https://www.gamergenic.com
// 
// Project page:   https://gamedevtricks.com/post/automagically-updating-ue-app-version/
// GitHub:         https://github.com/Gamergenic/AutoVersionNumbers/


using UnrealBuildTool;

public class AutoVersionNumbers : ModuleRules
{
	public AutoVersionNumbers(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
	
		PublicDependencyModuleNames.AddRange(new string[] { "Core", "CoreUObject", "Engine", "InputCore" });

        // We need EngineSettings to "properly" get the Project Version #.
		PrivateDependencyModuleNames.AddRange(new string[] { "EngineSettings" });

        if(Target.bBuildEditor)
        {
            // Needed for Source Control (Perforce, etc)
            PrivateDependencyModuleNames.AddRange(new string[] { "SourceControl" });
        }

        PublicIncludePaths.Add("AutoVersionNumbers/Commandlets/Public");
    }
}
