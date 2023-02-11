// Copyright 2023 Gamergenic.  See full copyright notice in AutoVersionNumbersModule.h.
// Author: chucknoble@gamergenic.com | https://www.gamergenic.com
// 
// Project page:   https://gamedevtricks.com/post/automagically-updating-ue-app-version/
// GitHub:         https://github.com/Gamergenic/AutoVersionNumbers/

#pragma once

#include "Commandlets/Commandlet.h"
#include "AutoVersionCommandlet.generated.h"

UCLASS()
class UAutoVersionCommandlet : public UCommandlet
{
    GENERATED_BODY()

    virtual int32 Main(const FString& Params) override;

private:
#if WITH_EDITOR
    // If source control is connected, sync DefaultGame.ini
    void SyncINIFile(const FString& FilePath);

    // If source control is connected, check out DefaultGame.ini
    bool CheckoutINIFile(const FString& FilePath);

    // Submit the updated DefaultGame.ini if it was checked out via source control
    void SubmitINIFile(const FString& FilePath);
#endif
};