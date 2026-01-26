namespace Domain
{
    public enum PackageType
    {
        Unknown = 0,
        AuthRequest,
        AuthResponse,
        BalanceInquiryRequest,
        BalanceInquiryResponse,
        TransactionRequest,
        TransactionResponse,
        MessageNotification
    }
}
