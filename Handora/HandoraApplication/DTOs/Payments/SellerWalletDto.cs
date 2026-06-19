using System;
using System.Collections.Generic;

namespace HandoraApplication.DTOs.Payments;

public class SellerWalletDto
{
    public decimal AvailableBalance { get; set; }
    public decimal PendingBalance { get; set; }
    public decimal TotalEarnings { get; set; }
    public List<WalletTransactionDto> Transactions { get; set; } = new();
}

public class WalletTransactionDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // "Sale" or "Withdrawal"
    public decimal Amount { get; set; } // Positive for Sales, Negative for Withdrawals
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
}
