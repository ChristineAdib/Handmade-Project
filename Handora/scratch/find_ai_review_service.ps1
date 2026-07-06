Get-ChildItem -Path . -Recurse -Include *.cs | ForEach-Object {
    $content = Get-Content $_.FullName
    if ($content -match "IAiReviewService") {
        Write-Output "Found in: $($_.FullName)"
    }
}
