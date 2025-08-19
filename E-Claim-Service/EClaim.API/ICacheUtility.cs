namespace E_Claim_Service
{
    public interface ICacheUtility
    {
        Task<string> GetDataFromCachAsync(string key);
        Task<string> SetDataInCachAsync(string key, object obj);
    }
}
