namespace Domain
{
    public enum PackageType
    {
        Unknown = 0,
        AuthRequest,
        TransactionRequest,
        ClientDataRequest,
        AuthResponse,
        TransactionResponse,
        ClientDataResponse
    }
}
