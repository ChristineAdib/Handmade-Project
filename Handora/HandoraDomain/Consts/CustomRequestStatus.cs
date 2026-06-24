namespace HandoraDomain.Consts
{
    public enum CustomRequestStatus
    {
        Draft = 1,
        Configuring = 2,
        ReadyForGeneration = 3,
        Generating = 4,
        Generated = 5,
        DesignSelected = 6,
        SellerMatched = 7,
        Negotiation = 8,
        OfferSent = 9,
        OfferAccepted = 10,
        PaymentPending = 11,
        Paid = 12,
        InProgress = 13,
        Completed = 14,
        Cancelled = 15,
        Rejected = 16
    }
}
