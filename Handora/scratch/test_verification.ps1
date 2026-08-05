$verifyUrl = "https://handauraa.runasp.net/api/auth/verify-otp"
$loginUrl = "https://handauraa.runasp.net/api/auth/login"
$email = "testreview_t1@example.com"
$otp = "279328" # from database query earlier

try {
    $verifyBody = @{
        email = $email
        otpCode = $otp
    } | ConvertTo-Json
    Write-Output "Verifying OTP..."
    $verifyResponse = Invoke-RestMethod -Uri $verifyUrl -Method Post -Body $verifyBody -ContentType "application/json"
    Write-Output "Verification response: $($verifyResponse | ConvertTo-Json -Depth 5)"

    # Login
    $loginBody = @{
        email = $email
        password = "Buyer@123"
    } | ConvertTo-Json
    Write-Output "Logging in..."
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginBody -ContentType "application/json"
    Write-Output "Login response token: $($loginResponse.data.token)"
} catch {
    Write-Output "Error occurred: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        Write-Output "Error Response Body: $body"
    }
}
