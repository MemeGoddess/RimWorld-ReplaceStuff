function workshop {
    if (Get-Command "steamcmd" -ErrorAction SilentlyContinue) {
        Write-Host "steamcmd is installed."
    } else {
        Write-Host "steamcmd is not installed."
        exit 1
    }

    $workshopItemsFile = "RequiredWorkshopItems.txt"
    $workshopItems = ""

    if (Test-Path $workshopItemsFile) {
        $workshopItems = Get-Content $workshopItemsFile | Where-Object { $_ -ne "" } | ForEach-Object { "+workshop_download_item 294100 $_" }
        Write-Host "Workshop items: '$workshopItems'"
    } else {
        Write-Host "RequiredWorkshopItems.txt file not found. Nothing to download."
        return
    }

    # Set the download location to Workshop
    $scriptDirectory = $PSScriptRoot
    $resolvedPath = Resolve-Path (Join-Path $scriptDirectory "..\..\..\")
    $downloadPath = Join-Path $resolvedPath "Workshop"
    if (-not (Test-Path $downloadPath)) {
        New-Item -ItemType Directory -Path $downloadPath | Out-Null
    }

    # Download workshop items for appid 294100
    &steamcmd +force_install_dir $downloadPath +login anonymous $workshopItems +quit

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to download workshop item ID: $itemId"
        exit 1
    }

    Write-Host "Workshop items downloaded successfully."

}

# Call the function
workshop
Write-Host "Setup complete. Solution should be buildable now."