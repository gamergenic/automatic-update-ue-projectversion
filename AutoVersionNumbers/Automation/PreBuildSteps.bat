REM First parameter is expected to be path to UnrealEditor.exe, and the second the path to the project directory.
set UEEditor=%1
set ProjectDirectory=%2

REM save the working directory
pushd .

REM change the working directory to the project directory
cd %ProjectDirectory%\\Automation

REM to use the custom c# app to update the version number & submit to perforce, un-REM this line
REM call %ProjectDirectory%\\Automation\\PreBuildSteps_UseCSApp.bat %2

REM to use the unreal engine commandlet instead, un-REM this line
call %ProjectDirectory%\\Automation\\PreBuildSteps_UseUECommandlet.bat %1 %2

REM restore the previous working directory
popd
