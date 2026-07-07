try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders'"
    $r = $cmd.ExecuteReader()
    Write-Output "=== Orders Columns ==="
    while ($r.Read()) {
        Write-Output "Column: Name=$($r['COLUMN_NAME']), Type=$($r['DATA_TYPE']), Nullable=$($r['IS_NULLABLE'])"
    }
    $r.Close()
    
    $cmd.CommandText = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'OrderItems'"
    $r = $cmd.ExecuteReader()
    Write-Output "`n=== OrderItems Columns ==="
    while ($r.Read()) {
        Write-Output "Column: Name=$($r['COLUMN_NAME']), Type=$($r['DATA_TYPE']), Nullable=$($r['IS_NULLABLE'])"
    }
    $r.Close()
    
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
