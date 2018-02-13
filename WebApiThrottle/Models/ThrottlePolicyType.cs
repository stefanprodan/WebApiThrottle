namespace WebApiThrottle.Models
{
    public enum ThrottlePolicyType
    {
        IpThrottling = 1,
        ClientThrottling,
        EndpointThrottling
    }
}