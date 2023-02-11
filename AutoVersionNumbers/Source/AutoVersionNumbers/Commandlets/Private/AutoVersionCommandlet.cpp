// Copyright 2023 Gamergenic.  See full copyright notice in AutoVersionNumbersModule.h.
// Author: chucknoble@gamergenic.com | https://www.gamergenic.com
// 
// Project page:   https://gamedevtricks.com/post/automagically-updating-ue-app-version/
// GitHub:         https://github.com/Gamergenic/AutoVersionNumbers/


#include "AutoVersionCommandlet.h"
#include "HAL/PlatformFileManager.h"
#include "GenericPlatform/GenericPlatformFile.h"
#include "Misc/Paths.h"
#include "HAL/FileManager.h"
#include "GeneralProjectSettings.h"
#include "Misc/DefaultValueHelper.h"
#include "ISourceControlModule.h"
#include "ISourceControlState.h"
#include "ISourceControlProvider.h"
#include "SourceControlOperations.h"

#define LOCTEXT_NAMESPACE "AutoVersionCommandlet"

int32 UAutoVersionCommandlet::Main(const FString& Params)
{
#if WITH_EDITOR

    FString DefaultGameINIPath = FPaths::Combine(FPaths::ProjectDir(), TEXT("Config/DefaultGame.ini"));

    //
    // First, sync the config file
    // Note:  You really should also make sure it ends up resolved, as there may be local edits!
    //
    SyncINIFile(DefaultGameINIPath);


    //
    // First, ensure the file is checked out from source control, or otherwise writable
    // 
    
    bool bFileWasCheckedOut = CheckoutINIFile(DefaultGameINIPath);

    //
    // Update project settings
    // 

    // Get the ProjectSettings "Class Default Object"
    // The CDO's ProjectVersion member is what we're trying to increment.
    // Since we're going to modify the value, we'll need a "Mutable" copy of it.
    UGeneralProjectSettings* ProjectSettings = GetMutableDefault<UGeneralProjectSettings>();
    FString VersionString = ProjectSettings->ProjectVersion;

    // Break the version number into strings separated by "."
    TArray<FString> VersionNumberStrings;
    VersionString.ParseIntoArray(VersionNumberStrings, TEXT("."), 1);
    
    if(VersionNumberStrings.Num() > 0)
    {
        // Find the very last string in the version number
        FString BuildNumberString = VersionNumberStrings[VersionNumberStrings.Num() - 1];

        // We're going to assume the last string is an int.  It's this we want to increment
        int64 BuildNumber = 0;

        if (FDefaultValueHelper::ParseInt64(BuildNumberString, BuildNumber))
        {
            // Success!  We found the build number, so increment it
            BuildNumber++;

            // Find the location the old build number started at.
            int32 LastDelimiterIndex;
            if (VersionString.FindLastChar('.', LastDelimiterIndex))
            {
                // Trim the last build number
                VersionString.RemoveAt(LastDelimiterIndex + 1);
                
                // Append the incremented build number to the string
                VersionString += FString::Printf(TEXT("%lld"), BuildNumber);

                // Set the value of the CDO
                ProjectSettings->ProjectVersion = VersionString;

                // Force the updated value to write back out to DefaultGame.ini
                ProjectSettings->UpdateSinglePropertyInConfigFile(ProjectSettings->GetClass()->FindPropertyByName(GET_MEMBER_NAME_CHECKED(UGeneralProjectSettings, ProjectVersion)), ProjectSettings->GetDefaultConfigFilename());
            }
        }
    }

    //
    // Lastly, submit the updated version # if we need to
    // 

    if (bFileWasCheckedOut)
    {
        SubmitINIFile(DefaultGameINIPath);
    }

#endif

    return 0;
}

#if WITH_EDITOR
void UAutoVersionCommandlet::SyncINIFile(const FString& FilePath)
{
    ISourceControlProvider* SourceControlProvider = &ISourceControlModule::Get().GetProvider();
    FSourceControlStatePtr ToFileSCCState;
    if (SourceControlProvider && SourceControlProvider->IsEnabled())
    {
        ToFileSCCState = SourceControlProvider->GetState(FilePath, EStateCacheUsage::ForceUpdate);

        if (ToFileSCCState->IsSourceControlled())
        {
            TArray<FString> FilesToBeSynced;
            FilesToBeSynced.Add(FilePath);

            SourceControlProvider->Execute(ISourceControlOperation::Create<FSync>(), FilesToBeSynced);
        }
    }
}


bool UAutoVersionCommandlet::CheckoutINIFile(const FString& FilePath)
{
    // Get the source control state of the INI file
    ISourceControlProvider* SourceControlProvider = &ISourceControlModule::Get().GetProvider();
    FSourceControlStatePtr ToFileSCCState;
    if (SourceControlProvider && SourceControlProvider->IsEnabled())
    {
        ToFileSCCState = SourceControlProvider->GetState(FilePath, EStateCacheUsage::ForceUpdate);

        // We don't need to do anything with source control if the file is already checked-out or added
        bool bAlreadyCheckedOut = ToFileSCCState && (ToFileSCCState->IsCheckedOut() || ToFileSCCState->IsAdded());
        const bool bRequiresSCCAction = ToFileSCCState && !ToFileSCCState->IsCheckedOut() && !ToFileSCCState->IsAdded();

        if (bRequiresSCCAction && ToFileSCCState->IsSourceControlled())
        {
            // the static analysis tool is not able to see that `SourceControlProvider` won't be null if `bRequiresSCCAction` is true
            CA_ASSUME(SourceControlProvider != nullptr);
            if (ToFileSCCState->CanCheckout() && SourceControlProvider->UsesCheckout())
            {
                TArray<FString> FilesToBeCheckedOut;
                FilesToBeCheckedOut.Add(FilePath);

                if (SourceControlProvider->Execute(ISourceControlOperation::Create<FCheckOut>(), FilesToBeCheckedOut) != ECommandResult::Succeeded)
                {
                    FText Failure = FText::Format(
                        NSLOCTEXT("AutoVersionCommandlet", "CheckoutFileFailure", "Failed to check-out file '{0}' from source control when updating INI file!"),
                        FText::FromString(FilePath));
                    UE_LOG(LogTemp, Warning, TEXT("%s"), *Failure.ToString());
                    return false;
                }

                return true;
            }
            else
            {
                FText Failure = FText::Format(
                    NSLOCTEXT("AutoVersionCommandlet", "CanCheckoutFileFailure", "Can't check-out file '{0}' from source control when updating INI file!"),
                    FText::FromString(FilePath));
                UE_LOG(LogTemp, Warning, TEXT("%s"), *Failure.ToString());
                return false;
            }
        }

        return bAlreadyCheckedOut;
    }
    else
    {
        IFileManager* FileManager = &IFileManager::Get();

        // No source control - make sure the INI file is writable
        if (FileManager && FileManager->FileExists(*FilePath) && FileManager->IsReadOnly(*FilePath))
        {
            FText Failure = FText::Format(
                NSLOCTEXT("AutoVersionCommandlet", "CanMakeWritable", "Can't make file '{0}' writable when updating DefaultGame.ini!"),
                FText::FromString(FilePath));
            UE_LOG(LogTemp, Warning, TEXT("%s"), *Failure.ToString());
        }
    }

    return false;
}

void UAutoVersionCommandlet::SubmitINIFile(const FString& FilePath)
{
    ISourceControlProvider* SourceControlProvider = &ISourceControlModule::Get().GetProvider();
    FSourceControlStatePtr ToFileSCCState;
    if (SourceControlProvider && SourceControlProvider->IsEnabled())
    {
        TArray<FString> FilesToBeCheckeIn;
        FilesToBeCheckeIn.Add(FilePath);

        TSharedRef<FCheckIn> CheckinOperation = ISourceControlOperation::Create<FCheckIn>();
        CheckinOperation->SetDescription(NSLOCTEXT("AutoVersionCommandlet", "UpdateVersion", "AUTOMATION: Update Project Version"));

        if (SourceControlProvider->Execute(CheckinOperation, FilesToBeCheckeIn) != ECommandResult::Succeeded)
        {
            FText Failure = FText::Format(
                NSLOCTEXT("AutoVersionCommandlet", "CheckinFileFailure", "Failed to check-in file '{0}' from source control when updating INI file!"),
                FText::FromString(FilePath));
            UE_LOG(LogTemp, Warning, TEXT("%s"), *Failure.ToString());
        }
    }
}
#endif

#undef LOCTEXT_NAMESPACE
