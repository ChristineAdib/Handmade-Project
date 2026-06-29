$connString = "Server=db55814.public.databaseasp.net; Database=db55814; User Id=db55814; Password=7z_FG#5ji2X?; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=60;"
$convId = "450cda5c-4958-4b11-9bf6-68ea9fa92370"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connString)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Id, BuyerId, SellerId FROM Conversations WHERE Id = '$convId'"
    $r = $cmd.ExecuteReader()
    $buyerId = $null
    $sellerId = $null
    while ($r.Read()) {
        $buyerId = $r['BuyerId']
        $sellerId = $r['SellerId']
        Write-Output "Conversation: Id=$($r['Id']), BuyerId=$buyerId, SellerId=$sellerId"
    }
    $r.Close()
    
    if ($buyerId -and $sellerId) {
        Write-Output "Searching CustomRequests for Buyer=$buyerId, Seller=$sellerId..."
        $cmd.CommandText = "SELECT Id, Status, SelectedSellerId, ProductType FROM CustomRequests WHERE BuyerId = '$buyerId'"
        $r = $cmd.ExecuteReader()
        $requestIds = New-Object System.Collections.Generic.List[Guid]
        while ($r.Read()) {
            $requestId = [Guid]$r['Id']
            $requestIds.Add($requestId)
            Write-Output "CustomRequest: Id=$requestId, Status=$($r['Status']), Seller=$($r['SelectedSellerId']), Product=$($r['ProductType'])"
        }
        $r.Close()
        
        foreach ($reqId in $requestIds) {
            $cmd.CommandText = "SELECT Id, CustomRequestId, Status, Price, OrderId FROM CustomServices WHERE CustomRequestId = '$reqId'"
            $r = $cmd.ExecuteReader()
            while ($r.Read()) {
                Write-Output "CustomService: Id=$($r['Id']), CustomRequestId=$($r['CustomRequestId']), Status=$($r['Status']), Price=$($r['Price']), OrderId=$($r['OrderId'])"
            }
            $r.Close()
            
            $cmd.CommandText = "SELECT Id, CustomRequestId, CustomServiceId, SelectedOfferId, OrderId, Status FROM ProjectWorkspaces WHERE CustomRequestId = '$reqId'"
            $r = $cmd.ExecuteReader()
            while ($r.Read()) {
                Write-Output "ProjectWorkspace: Id=$($r['Id']), CustomRequestId=$($r['CustomRequestId']), CustomServiceId=$($r['CustomServiceId']), SelectedOfferId=$($r['SelectedOfferId']), OrderId=$($r['OrderId']), Status=$($r['Status'])"
            }
            $r.Close()
        }
    } else {
        Write-Output "Conversation $convId not found."
    }
    
    $conn.Close()
} catch {
    Write-Output "Error: $_"
}
