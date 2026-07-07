try {
    $conn = New-Object System.Data.SqlClient.SqlConnection("Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;")
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, OverallSummary, Pros, Cons, LastUpdated FROM ProductReviewSummaries WHERE ProductId = 'a0e06fc4-e44e-4f31-8367-5ee47ee09008'"
    $r = $cmd.ExecuteReader()
    Write-Output "=== ProductReviewSummaries ==="
    while ($r.Read()) {
        Write-Output "Summary: Id=$($r['Id']), Summary=$($r['OverallSummary']), Pros=$($r['Pros']), Cons=$($r['Cons']), LastUpdated=$($r['LastUpdated'])"
    }
    $r.Close()
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
