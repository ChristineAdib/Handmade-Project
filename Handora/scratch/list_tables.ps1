try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
    $r = $cmd.ExecuteReader()
    Write-Output "=== Tables ==="
    while ($r.Read()) {
        Write-Output "Table: $($r['TABLE_NAME'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
