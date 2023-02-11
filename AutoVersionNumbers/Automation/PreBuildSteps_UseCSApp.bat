REM First parameter is the path to the project directory.
set ProjectDirectory=%1

REM This batch file runs every time our unreal engine project executes
REM Parameter %1 is our UE project directory
echo Running custom PreBuildSteps (Using C# App)
echo PreBuildSteps [%DATE% %TIME%] >> %ProjectDirectory%\\Automation\\AutomationLog.txt

REM save the working directory
pushd .

REM change the working directory to the project directory
cd %ProjectDirectory%\\Automation\\Bin

REM invoke the UE commandlet
%ProjectDirectory%\\Automation\\Bin\\UpdateProjectVersionCS.exe %ProjectDirectory%

REM restore the previous working directory
popd
