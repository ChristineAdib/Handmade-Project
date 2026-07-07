try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 15 Id, UserName, Email FROM AspNetUsers"
    $r = $cmd.ExecuteReader()
    Write-Output "=== Users ==="
    while ($r.Read()) {
        Write-Output "User: Id=$($r['Id']), UserName=$($r['UserName']), Email=$($r['Email'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
