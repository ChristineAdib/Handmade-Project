$loginUrl = "https://handauraa.runasp.net/api/auth/login"
$reviewUrl = "https://handauraa.runasp.net/api/reviews"
$productId = "7fb98e19-7380-4d34-a9c8-1c868855fa6e"

# 1. Login
$loginBody = @{
    email = "sara@gmail.com"
    password = "Buyer@123"
} | ConvertTo-Json

try {
    Write-Output "Logging in as sara@gmail.com..."
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginBody -ContentType "application/json"
    
    $token = $loginResponse.data.token
    if (-not $token) {
        Write-Output "Failed to get token: $($loginResponse | ConvertTo-Json -Depth 5)"
        exit
    }
    Write-Output "Login successful. Token acquired."

    # 2. Check eligibility via API first to verify validation works perfectly
    $eligibilityUrl = "https://handauraa.runasp.net/api/reviews/eligible/$productId"
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    Write-Output "Checking review eligibility via API..."
    $eligibilityResponse = Invoke-RestMethod -Uri $eligibilityUrl -Method Get -Headers $headers
    Write-Output "Eligibility Response: $($eligibilityResponse | ConvertTo-Json)"

    if ($eligibilityResponse.isEligible -eq $false -or $eligibilityResponse.data.isEligible -eq $false) {
        Write-Output "User is not eligible according to the API. Exiting."
        exit
    }

    # 3. Post review
    $reviewBody = @{
        productId = $productId
        rating = 5
        comment = "The craftsmanship is top-notch! The mug looks absolutely beautiful in my kitchen, is comfortable to hold, and has a very charming playful design. Highly recommended!"
    } | ConvertTo-Json

    Write-Output "Submitting review..."
    $reviewResponse = Invoke-RestMethod -Uri $reviewUrl -Method Post -Headers $headers -Body $reviewBody -ContentType "application/json"
    Write-Output "Review submitted successfully!"
    Write-Output "Response: $($reviewResponse | ConvertTo-Json -Depth 5)"

} catch {
    Write-Output "Error occurred: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        Write-Output "Error Response Body: $body"
    }
}
