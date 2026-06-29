try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 10 Id, RequestId, Action, Details, CreatedAt FROM CustomStudioAuditLogs ORDER BY CreatedAt DESC"
    $r = $cmd.ExecuteReader()
    Write-Output "=== CustomStudioAuditLogs ==="
    while ($r.Read()) {
        Write-Output "Log: Id=$($r['Id']), RequestId=$($r['RequestId']), Action=$($r['Action']), Details=$($r['Details']), CreatedAt=$($r['CreatedAt'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
