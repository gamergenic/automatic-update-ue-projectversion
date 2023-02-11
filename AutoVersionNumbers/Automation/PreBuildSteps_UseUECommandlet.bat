REM First parameter is expected to be path to UnrealEditor.exe, and the second the path to the project directory.
set UEEditor=%1
set ProjectDirectory=%2

REM This batch file runs every time our unreal engine project executes
REM Parameter %1 is our UE project directory
echo Running custom PreBuildSteps (Using UE Commandlet)
echo PreBuildSteps [%DATE% %TIME%] >> %ProjectDirectory%\\Automation\\AutomationLog.txt

REM save the working directory
pushd .

REM change the working directory to the project directory
cd %ProjectDirectory%

REM invoke the UE commandlet
%UEEditor% %ProjectDirectory%\\AutoVersionNumbers.uproject -skipcompile -run=AutoVersionCommandlet


REM restore the previous working directory
popd
