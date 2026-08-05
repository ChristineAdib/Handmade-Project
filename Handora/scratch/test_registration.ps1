$registerUrl = "https://handauraa.runasp.net/api/auth/register"
$email = "testreview_t1@example.com"
$name = "Test Review T1"
$password = "Buyer@123"

try {
    # Prepare multipart/form-data
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    $bodyLines = (
        "--$boundary",
        'Content-Disposition: form-data; name="Name"',
        "",
        $name,
        "--$boundary",
        'Content-Disposition: form-data; name="Email"',
        "",
        $email,
        "--$boundary",
        'Content-Disposition: form-data; name="Password"',
        "",
        $password,
        "--$boundary",
        'Content-Disposition: form-data; name="ConfirmPassword"',
        "",
        $password,
        "--$boundary",
        'Content-Disposition: form-data; name="Role"',
        "",
        "Buyer",
        "--$boundary--"
    ) -join $LF

    $headers = @{
        "Content-Type" = "multipart/form-data; boundary=$boundary"
    }

    Write-Output "Registering user..."
    $regResponse = Invoke-RestMethod -Uri $registerUrl -Method Post -Headers $headers -Body $bodyLines
    Write-Output "Registration response: $($regResponse | ConvertTo-Json -Depth 5)"

    # Query DB for OTP
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 OtpCode, UserId FROM OtpVerifications WHERE Email = @Email ORDER BY ExpiresAt DESC"
    $cmd.Parameters.AddWithValue("@Email", $email) | Out-Null
    $r = $cmd.ExecuteReader()
    $otp = ""
    $userId = ""
    while ($r.Read()) {
        $otp = $r['OtpCode']
        $userId = $r['UserId']
    }
    $r.Close()
    $conn.Close()

    Write-Output "Retrieved OTP: $otp for UserId: $userId"

    # Verify OTP
    $verifyUrl = "https://handauraa.runasp.net/api/auth/verify-otp"
    $verifyBody = @{
        email = $email
        otpCode = $otp
    } | ConvertTo-Json
    Write-Output "Verifying OTP..."
    $verifyResponse = Invoke-RestMethod -Uri $verifyUrl -Method Post -Body $verifyBody -ContentType "application/json"
    Write-Output "Verification response: $($verifyResponse | ConvertTo-Json -Depth 5)"

} catch {
    Write-Output "Error occurred: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $body = $reader.ReadToEnd()
        Write-Output "Error Response Body: $body"
    }
}
